import http from 'k6/http';
import { check } from 'k6';
import { Trend, Counter } from 'k6/metrics';

import { config } from './config.js';
import { endpointScenarios } from './scenarios.js';
import {
    appendCursor,
    extractCursor,
    shouldFetchNextPage
} from './pagination.js';

export const requestFailures =
    new Counter('request_failures');

const durationMetrics = {};

endpointScenarios.forEach(endpoint => {

    durationMetrics[endpoint.metricName] =
        new Trend(endpoint.metricName, true);
});

function addDurationMetric(
    metricName,
    duration,
    tags
) {

    durationMetrics[metricName]
        ?.add(duration, tags);
}

function createRequestParams(name) {

    return {
        tags: {
            endpoint: name
        }
    };
}

function logSuccess(
    name,
    duration,
    extra = ''
) {

    if (!config.logRequests) {
        return;
    }

    console.log(
        `[SUCCESS] ${name} | ${duration.toFixed(2)} ms ${extra}`
    );
}

function logFailure(
    name,
    response
) {

    requestFailures.add(1);

    console.error(
        `[FAILED] ${name} | ${response.status}`
    );

    if (response.body) {

        console.error(
            response.body.substring(0, 500)
        );
    }
}

function validateResponse(
    name,
    response
) {

    const success = check(response, {
        [`${name} status 200`]:
            r => r.status === 200
    });

    if (!success) {
        logFailure(name, response);
    }

    return success;
}

function sendRequest(
    name,
    url
) {

    const response = http.get(
        url,
        createRequestParams(name)
    );

    validateResponse(
        name,
        response
    );

    return response;
}

export function executeRequest(
    name,
    url,
    metricName
) {

    const response =
        sendRequest(name, url);

    addDurationMetric(
        metricName,
        response.timings.duration
    );

    logSuccess(
        name,
        response.timings.duration
    );

    return response;
}

export function executePagedRequest(
    name,
    url,
    metricName,
    maxRequests
) {

    let nextUrl = url;

    let pageCount = 0;

    while (nextUrl && pageCount < maxRequests) {

        const response =
            sendRequest(
                name,
                nextUrl
            );

        addDurationMetric(
            metricName,
            response.timings.duration
        );

        pageCount++;

        if (response.status !== 200) {
            return response;
        }

        const payload =
            response.json();

        const cursor =
            extractCursor(payload);

        logSuccess(
            name,
            response.timings.duration,
            `| page ${pageCount}`
        );

        if (!shouldFetchNextPage(cursor, pageCount, maxRequests)) {
            break;
        }

        nextUrl = appendCursor(url, cursor);
    }

    if (config.logRequests) {

        console.log(
            `[SUCCESS] ${name} | ${pageCount} pages`
        );
    }

    return null;
}