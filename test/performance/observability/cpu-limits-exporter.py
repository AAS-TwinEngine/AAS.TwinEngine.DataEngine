#!/usr/bin/env python3
"""
Simple Prometheus exporter for container CPU limits.
Reads CPU limits from docker inspect and environment variables.
Exposes them as Prometheus metrics for dynamic dashboard calculations.
"""

import os
import docker
import logging
from prometheus_client import start_http_server, Gauge, CollectorRegistry
import time

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Create registry
registry = CollectorRegistry()

# Define gauge for CPU limits
cpu_limit_gauge = Gauge(
    'container_cpu_limit_cores',
    'CPU limit in cores for each container',
    ['container_name'],
    registry=registry
)

def get_cpu_limits():
    """
    Retrieve CPU limits for containers from docker and environment.
    """
    try:
        client = docker.DockerClient(base_url='unix:///var/run/docker.sock')
    except Exception as e:
        logger.warning(f"Could not connect to docker: {e}, using env defaults")
        client = None
    
    # Define container CPU limits mapping (from environment or defaults)
    container_limits = {
        'twinengine-dataengine': float(os.getenv('TWINENGINE_CPU', '1')),
        'dpp-plugin': float(os.getenv('DPP_PLUGIN_CPU', '1')),
        'postgres': float(os.getenv('POSTGRES_CPU', '1')),
    }
    
    # Try to get actual limits from docker as well
    if client:
        try:
            containers = client.containers.list()
            for container in containers:
                name = container.name
                if name in container_limits:
                    # Verify the limit by checking docker config
                    nano_cpus = container.attrs.get('HostConfig', {}).get('NanoCpus', 0)
                    if nano_cpus > 0:
                        # Convert nanocpus to cores
                        actual_limit = nano_cpus / 1e9
                        container_limits[name] = actual_limit
                        logger.info(f"Found {name} CPU limit: {actual_limit} cores (from docker)")
                    else:
                        logger.info(f"No docker CPU limit for {name}, using env: {container_limits[name]} cores")
        except Exception as e:
            logger.warning(f"Could not get limits from docker: {e}")
    
    return container_limits

def update_metrics():
    """
    Update Prometheus metrics with current CPU limits.
    """
    limits = get_cpu_limits()
    for container_name, cpu_limit in limits.items():
        cpu_limit_gauge.labels(container_name=container_name).set(cpu_limit)
        logger.info(f"Metric: {container_name} -> {cpu_limit} cores")

if __name__ == '__main__':
    logger.info("Starting CPU limits exporter...")
    
    # Start HTTP server on port 8000
    start_http_server(8000, registry=registry)
    logger.info("Listening on port 8000")
    
    # Update metrics periodically
    while True:
        try:
            update_metrics()
        except Exception as e:
            logger.exception("Error updating metrics")
        
        # Update every 30 seconds
        time.sleep(30)
