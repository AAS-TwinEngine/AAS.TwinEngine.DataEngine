import { config } from './helpers/config.js';
import { discoverIds } from './helpers/setup.js';
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.4/index.js';
import {
    endpointMetricNames,
    getEnabledDiscoveryRequirements,
    getEnabledIdDependentEndpointKeys,
    resolveScenarioByKey
} from './helpers/scenarios.js';

import {
    executePagedRequest,
    executeRequest
} from './helpers/requests.js';

function buildScenarioOptions() {

    const scenarios = {};

    Object.entries(config.endpointRequests)
        .forEach(([endpointKey, requestCount]) => {

            if (!config.endpoints[endpointKey]) {
                return;
            }

            const scenarioMaxDuration =
                endpointKey === 'loadAllShellDescriptors'
                    ? config.load.loadAllShellDescriptorsMaxDuration
                    : config.load.maxDuration;

            scenarios[`endpoint_${endpointKey}`] = {
                executor: 'shared-iterations',
                exec: 'executeEndpointScenario',
                vus: config.load.vus,
                iterations: requestCount,
                maxDuration: scenarioMaxDuration,
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
    cloud: {
        projectID: 8108924
    },
    scenarios: configuredScenarios,
    summaryTimeUnit: 'ms',
    setupTimeout: config.load.setupTimeout
};

export function setup() {

    const idDependentEndpoints =
        getEnabledIdDependentEndpointKeys(config);

    const shouldDiscoverIds =
        idDependentEndpoints.length > 0;

    if (!shouldDiscoverIds) {

        console.log(
            '=== Skipping ID discovery ==='
        );

        return {
            shellIds: [],
            submodelIds: []
        };
    }

    const discoveredData =
        discoverIds();

    const discoveryRequirements =
        getEnabledDiscoveryRequirements(config);

    const missingRequirements =
        Object.entries(discoveryRequirements)
            .filter(([dataKey]) =>
                !Array.isArray(discoveredData[dataKey]) ||
                discoveredData[dataKey].length === 0
            );

    if (missingRequirements.length > 0) {

        const details =
            missingRequirements
                .map(([dataKey, endpoints]) =>
                    `${dataKey} required by: ${endpoints.join(', ')}`
                )
                .join(' | ');

        throw new Error(
            `Discovery completed but required IDs are missing. ${details}`
        );
    }

    return discoveredData;
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

    if (scenario.requestMode === 'paged') {
        executePagedRequest(
            scenario.name,
            scenario.url,
            scenario.metricName
        );

        return;
    }

    executeRequest(
        scenario.name,
        scenario.url,
        scenario.metricName
    );
}

export function handleSummary(data) {

    const output = {
        stdout: textSummary(data)
    };

    const csv = [
        [
            'Metric',
            'Avg(ms)',
            'Min(ms)',
            'Max(ms)',
            'P90(ms)',
            'P95(ms)'
        ].join(',')
    ];

    endpointMetricNames.forEach(metricName => {

        const metric =
            data.metrics[metricName];

        if (!metric?.values) {
            return;
        }

        csv.push([
            metricName,
            metric.values.avg?.toFixed(2),
            metric.values.min?.toFixed(2),
            metric.values.max?.toFixed(2),
            metric.values['p(90)']?.toFixed(2),
            metric.values['p(95)']?.toFixed(2)
        ].join(','));
    });

    const reportPath =
        config.reports.outputPath;

    if (config.reports.exportJson) {

        output[
            `${reportPath}/summary.json`
        ] = JSON.stringify(
            data,
            null,
            2
        );
    }

    if (config.reports.exportCsv) {

        output[
            `${reportPath}/results.csv`
        ] = csv.join('\n');
    }

    return output;
}
