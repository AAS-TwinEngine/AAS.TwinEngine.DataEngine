import http from 'k6/http';
import { check } from 'k6';
import { Trend, Counter } from 'k6/metrics';

import { config } from './config.js';
import { endpointScenarios } from './scenarios.js';

export const requestFailures =
    new Counter('request_failures');

const durationMetrics = {};

endpointScenarios.forEach(endpoint => {

    durationMetrics[endpoint.metricName] =
        new Trend(endpoint.metricName, true);
});

function extractCursor(payload) {

    return (
        payload?.paging_metadata?.cursor ||
        payload?.pagingMetadata?.cursor ||
        payload?.cursor ||
        null
    );
}

function addDurationMetric(
    metricName,
    duration,
    tags = {}
) {

    durationMetrics[metricName]
        ?.add(duration, tags);
}

function createRequestParams(name, pageIndex) {

    const tags = {
        endpoint: name
    };

    if (pageIndex !== undefined) {
        tags.page_index = String(pageIndex);
    }

    return {
        tags
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
    url,
    pageIndex
) {

    const response = http.get(
        url,
        createRequestParams(name, pageIndex)
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
    maxPages = Number.POSITIVE_INFINITY
) {

    let nextUrl = url;

    let totalDuration = 0;
    let pageCount = 0;

    while (nextUrl && pageCount < maxPages) {

        const response =
            sendRequest(
                name,
                nextUrl,
                pageCount + 1
            );

        const pageIndex = pageCount + 1;

        addDurationMetric(
            metricName,
            response.timings.duration,
            { page_index: String(pageIndex) }
        );

        totalDuration +=
            response.timings.duration;

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

        if (!cursor) {
            break;
        }

        const separator =
            url.includes('?') ? '&' : '?';

        nextUrl =
            `${url}${separator}cursor=${encodeURIComponent(cursor)}`;
    }

    if (config.logRequests) {

        console.log(
            `[SUCCESS] ${name} | ${pageCount} pages | total ${totalDuration.toFixed(2)} ms`
        );
    }

    return null;
}