const defaultConfig = {
    baseUrl: "http://localhost:8080",

    load: {
        vus: 1,
        maxDuration: "10m",
        loadAllShellDescriptorsMaxDuration: "60m",
        gracefulStop: "30s",
        setupTimeout: "10m"
    },

    logRequests: false,

    discovery: {
        pageLimit: 1000,
        maxDiscoveredIds: 10
    },

    endpoints: {
        getShells: { enabled: true, requests: 10, limit: 100 },
        getShellById: { enabled: true, requests: 10 },
        getAssetInformation: { enabled: true, requests: 10 },
        getSubmodelReferences: { enabled: true, requests: 10 },
        getShellDescriptors: { enabled: true, requests: 10, limit: 100 },
        getShellDescriptorById: { enabled: true, requests: 10 },
        getSubmodelDescriptors: { enabled: true, requests: 10, limit: 100 },
        getSubmodelDescriptorById: { enabled: true, requests: 10 },
        getSubmodels: { enabled: false, requests: 10, limit: 100 },
        getSubmodelById: { enabled: true, requests: 10 },
        loadAllShellDescriptors: { enabled: false, requests: 2, limit: 1000 }
    },
    reports: {
        outputPath: "results",

        exportCsv: true,
        exportJson: true,
        exportHtml: true
    }
};

function loadDotEnv() {

    try {

        const content = open('../.env');
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
    catch {
        return {};
    }
}

const fileEnv = loadDotEnv();

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

    const normalized =
        String(value).trim().toLowerCase();

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

    const parsed =
        Number.parseInt(value, 10);

    if (!Number.isFinite(parsed) || parsed < 1) {
        return fallback;
    }

    return parsed;
}

function parseNonNegativeInteger(value, fallback) {

    if (value === undefined) {
        return fallback;
    }

    const parsed =
        Number.parseInt(value, 10);

    if (!Number.isFinite(parsed) || parsed < 0) {
        return fallback;
    }

    return parsed;
}

function toEndpointEnvKey(endpointKey) {

    return `ENDPOINT_${endpointKey
        .replace(/([A-Z])/g, "_$1")
        .toUpperCase()}`;
}

function toEndpointRequestsEnvKey(endpointKey) {

    return `${toEndpointEnvKey(endpointKey)}_REQUESTS`;
}

function toEndpointLimitEnvKey(endpointKey) {

    return `${toEndpointEnvKey(endpointKey)}_LIMIT`;
}

function buildEndpoints() {

    const endpoints = {};
    const endpointRequests = {};
    const endpointLimits = {};

    Object.keys(defaultConfig.endpoints)
        .forEach(key => {

            const endpoint =
                defaultConfig.endpoints[key];

            let endpointEnabledValue =
                getEnvValue(
                    toEndpointEnvKey(key)
                );

            if (key === 'loadAllShellDescriptors' && endpointEnabledValue === undefined) {
                endpointEnabledValue = getEnvValue('ENDPOINT_LOAD_ALL_DATA');
            }

            endpoints[key] = parseBoolean(
                endpointEnabledValue,
                endpoint.enabled
            );

            let endpointRequestsValue =
                getEnvValue(
                    toEndpointRequestsEnvKey(key)
                );

            if (key === 'loadAllShellDescriptors' && endpointRequestsValue === undefined) {
                endpointRequestsValue = getEnvValue('ENDPOINT_LOAD_ALL_DATA_REQUESTS');
            }

            endpointRequests[key] = parsePositiveInteger(
                endpointRequestsValue,
                endpoint.requests
            );

            if (endpoint.limit !== undefined) {

                const endpointLimitValue =
                    getEnvValue(
                        toEndpointLimitEnvKey(key)
                    );

                endpointLimits[key] = parsePositiveInteger(
                    endpointLimitValue,
                    endpoint.limit
                );
            }
        });

    return {
        endpoints,
        endpointRequests,
        endpointLimits
    };
}

const endpointConfig =
    buildEndpoints();

export const config = {

    baseUrl:
        getEnvValue('BASE_URL') ||
        defaultConfig.baseUrl,

    load: {

        vus: parsePositiveInteger(
            getEnvValue('VUS'),
            defaultConfig.load.vus
        ),

        maxDuration:
            getEnvValue('MAX_DURATION') ||
            defaultConfig.load.maxDuration,

        loadAllShellDescriptorsMaxDuration:
            getEnvValue('LOAD_ALL_SHELL_DESCRIPTORS_MAX_DURATION') ||
            getEnvValue('LOAD_ALL_DATA_MAX_DURATION') ||
            defaultConfig.load.loadAllShellDescriptorsMaxDuration,

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

    loadAllShellDescriptors: {
        limit: parsePositiveInteger(
            getEnvValue('LIMIT_FOR_ALL_SHELL_DESCRIPTORS') ?? getEnvValue('LIMIT_FOR_ALL_DATA_LOAD') ?? getEnvValue('LIMITFORALLDATALOAD'),
            defaultConfig.endpoints.loadAllShellDescriptors.limit
        )
    },

   reports: {
    outputPath:
        getEnvValue('REPORT_OUTPUT_PATH') ||
        'results',

    exportCsv: parseBoolean(
        getEnvValue('EXPORT_CSV'),
        true
    ),

    exportJson: parseBoolean(
        getEnvValue('EXPORT_JSON'),
        true
    )
    },

    endpoints:
        endpointConfig.endpoints,

    endpointRequests:
        endpointConfig.endpointRequests,

    endpointLimits:
        endpointConfig.endpointLimits
};