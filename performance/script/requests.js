import http from 'k6/http';
import { check } from 'k6';
import { Trend, Counter } from 'k6/metrics';
import { config } from './config.js';
import { endpointScenarios } from './scenarios.js';

export const requestFailures =
    new Counter('request_failures');

const durationMetricsByName = {};

endpointScenarios.forEach(endpoint => {

    durationMetricsByName[endpoint.metricName] =
    new Trend(endpoint.metricName, true);
});

export function executeRequest(
    name,
    url,
    metricName
) {

    const metric =
        durationMetricsByName[metricName];

    const response = http.get(
        url,
        {
            tags: {
                endpoint: name
            }
        }
    );

    if (metric) {

        metric.add(
            response.timings.duration
        );
    }

    const success = check(response, {
        [`${name} status 200`]:
            r => r.status === 200
    });

    if (config.logRequests && success) {

        console.log(
            `[SUCCESS] ${name} | ${response.status} | ${response.timings.duration} ms`
        );
    }
    else if(!success) {

        requestFailures.add(1);

        console.error(
            `[FAILED] ${name} | ${response.status}`
        );

        console.error(
            response.body.substring(0, 500)
        );
    }

    return response;
}
