const defaultConfig = {
    baseUrl: "http://localhost:8080",

    load: {
        vus: 1,
        maxDuration: "10m",
        gracefulStop: "30s"
    },

    logRequests: false,

    discovery: {
        shellsPath: "/shells",
        submodelDescriptorsPath: "/submodel-descriptors",
        pageLimit: 1000,
        maxDiscoveredIds: 0
    },

    endpoints: {
        getShells: {
            enabled: true,
            requests: 100
        },
        getShellById: {
            enabled: true,
            requests: 100
        },
        getAssetInformation: {
            enabled: true,
            requests: 100
        },
        getSubmodelReferences: {
            enabled: true,
            requests: 100
        },
        getShellDescriptors: {
            enabled: true,
            requests: 100
        },
        getShellDescriptorById: {
            enabled: true,
            requests: 100
        },
        getSubmodelDescriptors: {
            enabled: true,
            requests: 100
        },
        getSubmodelDescriptorById: {
            enabled: true,
            requests: 100
        },
        getSubmodels: {
            enabled: false,
            requests: 100
        },
        getSubmodelById: {
            enabled: true,
            requests: 100
        }
    },

    endpointRequests: {}
};

function parseBoolean(value, fallback) {

    if (value === undefined) {
        return fallback;
    }

    const normalized = String(value).trim().toLowerCase();

    if (["true", "1", "yes", "y", "on"].includes(normalized)) {
        return true;
    }

    if (["false", "0", "no", "n", "off"].includes(normalized)) {
        return false;
    }

    return fallback;
}

function parsePositiveInteger(value, fallback) {

    if (value === undefined) {
        return fallback;
    }

    const parsed = Number.parseInt(value, 10);

    if (!Number.isFinite(parsed) || parsed < 1) {
        return fallback;
    }

    return parsed;
}

function parsePositiveNumber(value, fallback) {

    if (value === undefined) {
        return fallback;
    }

    const parsed = Number.parseFloat(value);

    if (!Number.isFinite(parsed) || parsed < 0) {
        return fallback;
    }

    return parsed;
}

function parseCsv(value) {

    if (!value) {
        return [];
    }

    return String(value)
        .split(",")
        .map(item => item.trim())
        .filter(Boolean);
}

function parseEndpointCounts(value) {

    const counts = {};

    parseCsv(value).forEach(entry => {

        const separatorIndex = entry.indexOf(':');

        if (separatorIndex < 1) {
            return;
        }

        const endpointKey =
            entry.substring(0, separatorIndex).trim();

        const countValue =
            entry.substring(separatorIndex + 1).trim();

        const parsedCount = Number.parseInt(countValue, 10);

        if (
            !endpointKey ||
            !Number.isFinite(parsedCount) ||
            parsedCount < 1
        ) {
            return;
        }

        counts[endpointKey] = parsedCount;
    });

    return counts;
}

function endpointEnabledByDefault(endpointValue) {

    if (typeof endpointValue === 'boolean') {
        return endpointValue;
    }

    return endpointValue?.enabled !== false;
}

function endpointRequestsByDefault(endpointValue) {

    if (
        endpointValue &&
        typeof endpointValue === 'object' &&
        endpointValue.requests !== undefined
    ) {
        return endpointValue.requests;
    }

    return 1;
}

function getDefaultEndpointEnabledMap(defaultEndpoints) {

    const endpointEnabled = {};

    Object.keys(defaultEndpoints)
        .forEach(key => {
            endpointEnabled[key] = endpointEnabledByDefault(
                defaultEndpoints[key]
            );
        });

    return endpointEnabled;
}

function getDefaultEndpointRequestsMap(defaultEndpoints) {

    const endpointRequests = {};

    Object.keys(defaultEndpoints)
        .forEach(key => {

            const parsedRequestCount = Number.parseInt(
                endpointRequestsByDefault(defaultEndpoints[key]),
                10
            );

            if (
                Number.isFinite(parsedRequestCount) &&
                parsedRequestCount > 0
            ) {
                endpointRequests[key] = parsedRequestCount;
            }
        });

    return endpointRequests;
}

function buildEndpointRequestConfig(defaultEndpoints, defaultRequests, envValue) {

    const requests = {
        ...defaultRequests,
        ...parseEndpointCounts(envValue)
    };

    Object.keys(requests)
        .forEach(key => {

            const parsedCount = Number.parseInt(requests[key], 10);

            if (
                !(key in defaultEndpoints) ||
                !Number.isFinite(parsedCount) ||
                parsedCount < 1
            ) {
                delete requests[key];
                return;
            }

            requests[key] = parsedCount;
        });

    return requests;
}

function toEndpointEnvKey(endpointKey) {

    return `ENDPOINT_${endpointKey
        .replace(/([A-Z])/g, "_$1")
        .toUpperCase()}`;
}

function buildEndpointConfig(defaultEndpoints) {

    const endpoints =
        getDefaultEndpointEnabledMap(defaultEndpoints);

    const enabledEndpoints =
        parseCsv(__ENV.ENABLED_ENDPOINTS);

    if (enabledEndpoints.length > 0) {

        Object.keys(endpoints).forEach(key => {
            endpoints[key] = false;
        });

        enabledEndpoints.forEach(key => {

            if (key in endpoints) {
                endpoints[key] = true;
            }
        });
    }

    parseCsv(__ENV.DISABLED_ENDPOINTS)
        .forEach(key => {

            if (key in endpoints) {
                endpoints[key] = false;
            }
        });

    Object.keys(defaultEndpoints)
        .forEach(key => {

            const envKey = toEndpointEnvKey(key);

            endpoints[key] = parseBoolean(
                __ENV[envKey],
                endpoints[key]
            );
        });

    return endpoints;
}

export const config = {
    baseUrl: __ENV.BASE_URL || defaultConfig.baseUrl,

    load: {
        vus: parsePositiveInteger(
            __ENV.VUS,
            defaultConfig.load.vus
        ),
        maxDuration:
            __ENV.MAX_DURATION ||
            defaultConfig.load.maxDuration,
        gracefulStop:
            __ENV.GRACEFUL_STOP ||
            defaultConfig.load.gracefulStop
    },

    logRequests: parseBoolean(
        __ENV.LOG_REQUESTS,
        defaultConfig.logRequests
    ),

    discovery: {
        shellsPath:
            __ENV.DISCOVER_SHELLS_PATH ||
            defaultConfig.discovery.shellsPath,

        submodelDescriptorsPath:
            __ENV.DISCOVER_SUBMODEL_DESCRIPTORS_PATH ||
            defaultConfig.discovery.submodelDescriptorsPath,

        pageLimit: parsePositiveInteger(
            __ENV.DISCOVER_PAGE_LIMIT,
            defaultConfig.discovery.pageLimit
        ),

        maxDiscoveredIds: parsePositiveInteger(
            __ENV.MAX_DISCOVERED_IDS,
            defaultConfig.discovery.maxDiscoveredIds
        )
    },

    endpointRequests: buildEndpointRequestConfig(
        defaultConfig.endpoints,
        {
            ...getDefaultEndpointRequestsMap(defaultConfig.endpoints),
            ...defaultConfig.endpointRequests
        },
        __ENV.ENDPOINT_REQUESTS
    ),

    endpoints: buildEndpointConfig(defaultConfig.endpoints)
};
