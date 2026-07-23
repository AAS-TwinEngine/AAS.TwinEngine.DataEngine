#!/usr/bin/env python3

"""
Docker Resource Exporter

Exports configured Docker CPU and memory limits as Prometheus metrics.

Runtime CPU, memory, network and I/O metrics are collected by the
OpenTelemetry docker_stats receiver. This exporter only exposes the
configured resource limits obtained from Docker inspect.
"""

import os
import docker
import logging
from prometheus_client import start_http_server, Gauge, CollectorRegistry
import time

import docker
from docker.errors import DockerException
from prometheus_client import CollectorRegistry, Gauge, start_http_server

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s %(levelname)s %(message)s",
)

logger = logging.getLogger(__name__)

SCRAPE_INTERVAL = 30
EXPORTER_PORT = 8000
DOCKER_SOCKET_URL = "unix://var/run/docker.sock"

registry = CollectorRegistry()

# Define gauge for CPU limits
cpu_limit_gauge = Gauge(
    "container_cpu_limit_cores",
    "Configured Docker CPU limit in cores",
    ["container", "compose_service", "image"],
    registry=registry,
)

memory_limit_gauge = Gauge(
    "container_memory_limit_bytes",
    "Configured Docker memory limit in bytes",
    ["container", "compose_service", "image"],
    registry=registry,
)

client = None
last_values = {}
seen_series = set()

    # Define container CPU limits mapping (from environment or defaults)
    container_limits = {
        'twinengine-dataengine': float(os.getenv('TWINENGINE_CPU', '1')),
        'dpp-plugin': float(os.getenv('DPP_PLUGIN_CPU', '1')),
        'postgres': float(os.getenv('POSTGRES_CPU', '1')),
    }

def connect():
    """Create Docker client using the Unix socket only."""
    global client

    if client:
        return client

    try:
        # Intentionally avoid docker.from_env() so invalid DOCKER_HOST values
        # (e.g. http+docker://...) cannot break connectivity.
        client = docker.DockerClient(base_url=DOCKER_SOCKET_URL, version="auto")
        client.ping()
        logger.info("Connected to Docker daemon")
        return client

    except DockerException as ex:
        logger.info("Unable to connect to Docker daemon: %s", ex)
        client = None
        return None


def update_metrics():
    global client

    docker_client = connect()

    if docker_client is None:
        return

    try:
        active_series = set()

        for container in docker_client.containers.list():

            attrs = container.attrs
            host = attrs.get("HostConfig", {})
            labels = attrs.get("Config", {}).get("Labels", {})

            container_name = container.name
            compose_service = labels.get(
                "com.docker.compose.service",
                container_name,
            )
            image = container.image.tags[0] if container.image.tags else container.image.short_id

            nano_cpus = host.get("NanoCpus", 0)
            memory = host.get("Memory", 0)

            cpu_limit = (
                nano_cpus / 1_000_000_000
                if nano_cpus > 0
                else 0
            )

            label_values = {
                "container": container_name,
                "compose_service": compose_service,
                "image": image,
            }

            cpu_limit_gauge.labels(**label_values).set(cpu_limit)
            memory_limit_gauge.labels(**label_values).set(memory)

            current = (cpu_limit, memory)
            series_key = (container_name, compose_service, image)
            active_series.add(series_key)

            if last_values.get(container_name) != current:
                logger.debug(
                    "Container '%s' limits changed: CPU=%s cores Memory=%s bytes",
                    container_name,
                    cpu_limit,
                    memory,
                )
                last_values[container_name] = current

        removed_series = seen_series - active_series
        for series in removed_series:
            cpu_limit_gauge.remove(*series)
            memory_limit_gauge.remove(*series)

        seen_series.clear()
        seen_series.update(active_series)

    except DockerException as ex:
        logger.info("Failed to read Docker container metadata: %s", ex)
        try:
            docker_client.close()
        except DockerException:
            pass
        client = None


def main():
    logger.info("Starting Docker Resource Exporter")
    logger.info("Metrics available on :%d/metrics", EXPORTER_PORT)

    start_http_server(EXPORTER_PORT, registry=registry)

    # Update metrics periodically
    while True:
        try:
        update_metrics()
        time.sleep(SCRAPE_INTERVAL)


if __name__ == "__main__":
    main()