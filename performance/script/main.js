import { sleep } from 'k6';

import { config } from './config.js';
import { discoverIds } from './setup.js';
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.4/index.js';
import {
    getEndpointMetricNames,
    resolveScenarioByKey
} from './scenarios.js';

import {
    executeRequest
} from './requests.js';

function buildScenarioOptions() {

    const scenarios = {};

    Object.entries(config.endpointRequests)
        .forEach(([endpointKey, requestCount]) => {

            if (!config.endpoints[endpointKey]) {
                return;
            }

            scenarios[`endpoint_${endpointKey}`] = {
                executor: 'shared-iterations',
                exec: 'executeEndpointScenario',
                vus: config.load.vus,
                iterations: requestCount,
                maxDuration: config.load.maxDuration,
                gracefulStop: config.load.gracefulStop,
                env: {
                    ENDPOINT_KEY: endpointKey
                }
            };
        });

    return scenarios;
}

const configuredScenarios =
    buildScenarioOptions();

if (Object.keys(configuredScenarios).length === 0) {

    throw new Error(
        'No endpoint requests configured. Add entries in config.endpointRequests.'
    );
}

export const options = {
    scenarios: configuredScenarios,
    summaryTimeUnit: 'ms'
};

export function setup() {

    return discoverIds();
}

export function executeEndpointScenario(data) {

    const endpointKey = __ENV.ENDPOINT_KEY;

    const scenario = resolveScenarioByKey(
        config,
        data,
        endpointKey
    );

    if (!scenario) {
        return;
    }

    executeRequest(
        scenario.name,
        scenario.url,
        scenario.metricName
    );
}


export function handleSummary(data) {

    const metrics = data.metrics;

    const csv = [
        [
            "Metric",
            "Avg(ms)",
            "Min(ms)",
            "Max(ms)",
            "P90(ms)",
            "P95(ms)"
        ].join(",")
    ];

    const endpointMetrics =
        [...new Set(getEndpointMetricNames())];

    endpointMetrics.forEach(metricName => {

        const metric = metrics[metricName];

        if (!metric || !metric.values) {
            return;
        }

        csv.push([
            metricName,
            metric.values.avg?.toFixed(2),
            metric.values.min?.toFixed(2),
            metric.values.max?.toFixed(2),
            metric.values["p(90)"]?.toFixed(2),
            metric.values["p(95)"]?.toFixed(2)
        ].join(","));
    });

    return {
        stdout: textSummary(data),
        "summary.json":
            JSON.stringify(data, null, 2),

        "results.csv":
            csv.join("\n")
    };
}
