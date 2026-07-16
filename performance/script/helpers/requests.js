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

function extractCursor(payload) {

    return payload?.paging_metadata?.cursor ||
        payload?.pagingMetadata?.cursor ||
        payload?.cursor ||
        null;
}

function addDurationMetric(metricName, duration) {

    const metric =
        durationMetricsByName[metricName];

    if (metric) {
        metric.add(duration);
    }
}

function createRequestParams(name) {

    return {
        tags: {
            endpoint: name
        }
    };
}

function logFailure(name, response) {

    requestFailures.add(1);

    console.error(
        `[FAILED] ${name} | ${response.status}`
    );

    console.error(
        response.body.substring(0, 500)
    );
}

export function executeRequest(
    name,
    url,
    metricName
) {

    const response = http.get(
        url,
        createRequestParams(name)
    );

    addDurationMetric(
        metricName,
        response.timings.duration
    );

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
        logFailure(name, response);
    }

    return response;
}

export function executePagedRequest(
    name,
    url,
    metricName
) {

    let nextUrl = url;
    let totalDuration = 0;
    let pageCount = 0;

    while (nextUrl) {

        const response = http.get(
            nextUrl,
            createRequestParams(name)
        );

        totalDuration += response.timings.duration;
        pageCount += 1;

        const success = check(response, {
            [`${name} status 200`]:
                r => r.status === 200
        });

        if (!success) {
            logFailure(name, response);
            return response;
        }

        const payload = response.json();
        const cursor = extractCursor(payload);

        if (config.logRequests) {
            console.log(
                `[SUCCESS] ${name} | page ${pageCount} | ${response.status} | ${response.timings.duration} ms` +
                `${cursor ? ` | next cursor: ${cursor}` : ' | completed'}`
            );
        }

        if (!cursor) {
            nextUrl = null;
            break;
        }

        nextUrl = `${url}?cursor=${encodeURIComponent(cursor)}`;
    }

    addDurationMetric(metricName, totalDuration);

    if (config.logRequests) {
        console.log(
            `[SUCCESS] ${name} | fetched ${pageCount} pages | total ${totalDuration} ms`
        );
    }

    return null;
}
