const defaultConfig = {
    baseUrl: "http://localhost:8080",

    load: {
        vus: 1,
        maxDuration: "10m",
        loadAllDataMaxDuration: "60m",
        gracefulStop: "30s",
        setupTimeout: "10m"
    },

    logRequests: false,

    discovery: {
        pageLimit: 1000,
        maxDiscoveredIds: 10
    },

    endpoints: {
        getShells: {
            enabled: true,
            requests: 10
        },
        getShellById: {
            enabled: true,
            requests: 10
        },
        getAssetInformation: {
            enabled: true,
            requests: 10
        },
        getSubmodelReferences: {
            enabled: true,
            requests: 10
        },
        getShellDescriptors: {
            enabled: true,
            requests: 10
        },
        getShellDescriptorById: {
            enabled: true,
            requests: 10
        },
        getSubmodelDescriptors: {
            enabled: true,
            requests: 10
        },
        getSubmodelDescriptorById: {
            enabled: true,
            requests: 10
        },
        getSubmodels: {
            enabled: false,
            requests: 10
        },
        getSubmodelById: {
            enabled: true,
            requests: 10
        },
        loadAllData: {
            enabled: false,
            requests: 2
        }
    }
};

function loadDotEnv() {

    try {

        const content = open('./.env');
        const envValues = {};

        content.split(/\r?\n/)
            .forEach(line => {

                const trimmedLine = line.trim();

                if (!trimmedLine || trimmedLine.startsWith('#')) {
                    return;
                }

                const separatorIndex =
                    trimmedLine.indexOf('=');

                if (separatorIndex < 1) {
                    return;
                }

                const key =
                    trimmedLine.substring(0, separatorIndex).trim();

                let value =
                    trimmedLine.substring(separatorIndex + 1).trim();

                if (
                    (value.startsWith('"') && value.endsWith('"')) ||
                    (value.startsWith("'") && value.endsWith("'"))
                ) {
                    value = value.substring(1, value.length - 1);
                }

                envValues[key] = value;
            });

        return envValues;
    }
    catch (error) {
        return {};
    }
}

const fileEnv =
    loadDotEnv();

function getEnvValue(key) {

    if (__ENV[key] !== undefined) {
        return __ENV[key];
    }

    return fileEnv[key];
}

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

function parseNonNegativeInteger(value, fallback) {

    if (value === undefined) {
        return fallback;
    }

    const parsed = Number.parseInt(value, 10);

    if (!Number.isFinite(parsed) || parsed < 0) {
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

function parseEndpointCounts(value) {

    const counts = {};

    if (!value) {
        return counts;
    }

    String(value)
        .split(",")
        .map(item => item.trim())
        .filter(Boolean)
        .forEach(entry => {

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

function toEndpointRequestsEnvKey(endpointKey) {

    return `${toEndpointEnvKey(endpointKey)}_REQUESTS`;
}

function buildEndpointConfig(defaultEndpoints) {

    const endpoints =
        getDefaultEndpointEnabledMap(defaultEndpoints);

    Object.keys(defaultEndpoints)
        .forEach(key => {

            const envKey = toEndpointEnvKey(key);

            endpoints[key] = parseBoolean(
                getEnvValue(envKey),
                endpoints[key]
            );
        });

    return endpoints;
}

function buildEndpointRequestEnvOverrides(defaultEndpoints) {

    const requestOverrides = {};

    Object.keys(defaultEndpoints)
        .forEach(key => {

            const envKey =
                toEndpointRequestsEnvKey(key);

            const parsedCount = parsePositiveInteger(
                getEnvValue(envKey),
                undefined
            );

            if (parsedCount !== undefined) {
                requestOverrides[key] = parsedCount;
            }
        });

    return requestOverrides;
}

export const config = {
    baseUrl: getEnvValue('BASE_URL') || defaultConfig.baseUrl,

    load: {
        vus: parsePositiveInteger(
            getEnvValue('VUS'),
            defaultConfig.load.vus
        ),
        maxDuration:
            getEnvValue('MAX_DURATION') ||
            defaultConfig.load.maxDuration,
        loadAllDataMaxDuration:
            getEnvValue('LOAD_ALL_DATA_MAX_DURATION') ||
            defaultConfig.load.loadAllDataMaxDuration,
        gracefulStop:
            getEnvValue('GRACEFUL_STOP') ||
            defaultConfig.load.gracefulStop,
        setupTimeout:
            getEnvValue('SETUP_TIMEOUT') ||
            defaultConfig.load.setupTimeout
    },

    logRequests: parseBoolean(
        getEnvValue('LOG_REQUESTS'),
        defaultConfig.logRequests
    ),

    discovery: {
        pageLimit: parsePositiveInteger(
            getEnvValue('DISCOVER_PAGE_LIMIT'),
            defaultConfig.discovery.pageLimit
        ),

        maxDiscoveredIds: parseNonNegativeInteger(
            getEnvValue('MAX_DISCOVERED_IDS'),
            defaultConfig.discovery.maxDiscoveredIds
        )
    },

    endpointRequests: buildEndpointRequestConfig(
        defaultConfig.endpoints,
        {
            ...getDefaultEndpointRequestsMap(defaultConfig.endpoints),
            ...buildEndpointRequestEnvOverrides(defaultConfig.endpoints)
        },
        getEnvValue('ENDPOINT_REQUESTS')
    ),

    endpoints: buildEndpointConfig(defaultConfig.endpoints)
};
