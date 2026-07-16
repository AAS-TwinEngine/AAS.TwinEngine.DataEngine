import http from 'k6/http';
import { config } from './config.js';

const SHELLS_PATH = '/shells';
const SAMPLE_LOG_COUNT = 10;

function asArray(payload) {

    if (Array.isArray(payload)) {
        return payload;
    }

    if (payload && Array.isArray(payload.result)) {
        return payload.result;
    }

    if (payload && Array.isArray(payload.items)) {
        return payload.items;
    }

    return [];
}

function extractId(item) {

    return item?.id ||
        item?.identification?.id ||
        null;
}

function extractCursor(payload) {

    return payload?.paging_metadata?.cursor ||
        payload?.pagingMetadata?.cursor ||
        payload?.cursor ||
        null;
}

function extractSubmodelIds(shell) {

    const submodelIds = [];

    for (const reference of shell?.submodels || []) {

        for (const key of reference?.keys || []) {

            if (key?.type === 'Submodel' && key?.value) {
                submodelIds.push(key.value);
            }
        }
    }

    return submodelIds;
}

function logDiscoveredIdSamples(shellIds, submodelIds) {

    const shellIdSamples = shellIds
        .slice(0, SAMPLE_LOG_COUNT);

    const submodelIdSamples = submodelIds
        .slice(0, SAMPLE_LOG_COUNT);

    console.log(
        `Sample shellIds (${shellIdSamples.length}): ${JSON.stringify(shellIdSamples)}`
    );

    console.log(
        `Sample submodelIds (${submodelIdSamples.length}): ${JSON.stringify(submodelIdSamples)}`
    );
}

function discoverShellAndSubmodelIds() {

    const shellIds = new Set();
    const submodelIds = new Set();
    const limit = config.discovery.pageLimit;
    let cursor = null;
    let reachedMaxDiscoveredIds = false;

    do {

        const queryParameters = [`limit=${limit}`];

        if (cursor) {
            queryParameters.push(
                `cursor=${encodeURIComponent(cursor)}`
            );
        }

        const url = `${config.baseUrl}${SHELLS_PATH}?${queryParameters.join('&')}`;

        try {

            const response =
                http.get(url);

            if (response.status !== 200) {

                console.error(
                    `Failed discovering shells: ${response.status}`
                );

                break;
            }

            const payload = response.json();
            const shells = asArray(payload);

            for (const shell of shells) {

                const shellId = extractId(shell);

                if (shellId) {
                    shellIds.add(shellId);
                }

                extractSubmodelIds(shell)
                    .forEach(submodelId => {
                        submodelIds.add(submodelId);
                    });

                if (
                    config.discovery.maxDiscoveredIds > 0 &&
                    shellIds.size >= config.discovery.maxDiscoveredIds
                ) {
                    reachedMaxDiscoveredIds = true;
                    break;
                }
            }

            if (reachedMaxDiscoveredIds) {
                cursor = null;
            }
            else {
                cursor = extractCursor(payload);
            }

            console.log(
                `Discovered ${shellIds.size} shell ids and ${submodelIds.size} submodel ids so far` +
                `${cursor ? `, next cursor: ${cursor}` : ''}`
            );
        }
        catch (error) {

            console.error(
                `Exception discovering shells: ${error}`
            );

            break;
        }
    }
    while (cursor);
    
    const discoveredShellIds = [...shellIds];
    const discoveredSubmodelIds = [...submodelIds];

    logDiscoveredIdSamples(
        discoveredShellIds,
        discoveredSubmodelIds
    );

    return {
        shellIds: discoveredShellIds,
        submodelIds: discoveredSubmodelIds
    };
}

export function discoverIds() {

    console.log("=== Discovering IDs ===");

    return discoverShellAndSubmodelIds();
}
