import http from 'k6/http';
import { config } from './config.js';

const SHELLS_ENDPOINT = '/shells';
const SAMPLE_LOG_COUNT = 10;

function asArray(payload) {

    if (Array.isArray(payload)) {
        return payload;
    }

    if (Array.isArray(payload?.result)) {
        return payload.result;
    }

    if (Array.isArray(payload?.items)) {
        return payload.items;
    }

    return [];
}

function extractId(item) {

    return (
        item?.id ||
        item?.identification?.id ||
        null
    );
}

function extractCursor(payload) {

    return (
        payload?.paging_metadata?.cursor ||
        payload?.pagingMetadata?.cursor ||
        payload?.cursor ||
        null
    );
}

function extractSubmodelIds(shell) {

    const submodelIds = [];

    for (const reference of shell?.submodels || []) {

        for (const key of reference?.keys || []) {

            if (
                key?.type === 'Submodel' &&
                key?.value
            ) {
                submodelIds.push(key.value);
            }
        }
    }

    return submodelIds;
}

function buildShellUrl(cursor) {

    const query = [
        `limit=${config.discovery.pageLimit}`
    ];

    if (cursor) {

        query.push(
            `cursor=${encodeURIComponent(cursor)}`
        );
    }

    return `${config.baseUrl}${SHELLS_ENDPOINT}?${query.join('&')}`;
}

function reachedDiscoveryLimit(shellIds) {

    return (
        config.discovery.maxDiscoveredIds > 0 &&
        shellIds.size >= config.discovery.maxDiscoveredIds
    );
}

function processShells(
    shells,
    shellIds,
    submodelIds
) {

    for (const shell of shells) {

        const shellId =
            extractId(shell);

        if (shellId) {
            shellIds.add(shellId);
        }

        extractSubmodelIds(shell)
            .forEach(id =>
                submodelIds.add(id)
            );

        if (
            reachedDiscoveryLimit(shellIds)
        ) {
            return true;
        }
    }

    return false;
}

// Log a sample of discovered IDs to the console for debugging purposes
function logSamples(
    shellIds,
    submodelIds
) {

    console.log(
        `Sample shellIds: ${JSON.stringify(
            shellIds.slice(0, SAMPLE_LOG_COUNT)
        )}`
    );

    console.log(
        `Sample submodelIds: ${JSON.stringify(
            submodelIds.slice(0, SAMPLE_LOG_COUNT)
        )}`
    );
}

export function discoverIds() {

    console.log(
        '=== Discovering IDs ==='
    );

    const shellIds =
        new Set();

    const submodelIds =
        new Set();

    let cursor = null;
    let stopDiscovery = false;

    do {

        try {

            const response =
                http.get(
                    buildShellUrl(cursor)
                );

            if (
                response.status !== 200
            ) {

                console.error(
                    `Failed discovering shells: ${response.status}`
                );

                break;
            }

            const payload =
                response.json();

            const shells =
                asArray(payload);

            stopDiscovery =
                processShells(
                    shells,
                    shellIds,
                    submodelIds
                );

            cursor =
                stopDiscovery
                    ? null
                    : extractCursor(payload);

            console.log(
                `Discovered ${shellIds.size} shell ids and ${submodelIds.size} submodel ids` +
                (cursor
                    ? ` | next cursor: ${cursor}`
                    : '')
            );
        }
        catch (error) {

            console.error(
                `Discovery failed: ${error}`
            );

            break;
        }
    }
    while (cursor);

    const discoveredShellIds =
        [...shellIds];

    const discoveredSubmodelIds =
        [...submodelIds];

    // logSamples(
    //     discoveredShellIds,
    //     discoveredSubmodelIds
    // ); - Commented out to reduce console output during performance tests

    return {
        shellIds: discoveredShellIds,
        submodelIds: discoveredSubmodelIds
    };
}