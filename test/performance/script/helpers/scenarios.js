import encoding from 'k6/encoding';

function randomItem(items) {

    return items[
        Math.floor(
            Math.random() * items.length
        )
    ];
}

function toBase64Url(value) {

    return encoding.b64encode(
        value,
        'rawurl'
    );
}

function fromRandomId(
    dataProperty,
    pathBuilder
) {

    return ({ baseUrl, data }) => {

        const ids =
            data[dataProperty];

        if (!ids || ids.length === 0) {
            return null;
        }

        const encodedId =
            toBase64Url(
                randomItem(ids)
            );

        return `${baseUrl}${pathBuilder(encodedId)}`;
    };
}

function appendLimitQuery(url, limit) {

    const parsedLimit =
        Number.parseInt(limit, 10);

    const normalizedLimit =
        Number.isFinite(parsedLimit) && parsedLimit > 0
            ? parsedLimit
            : 100;

    const separator =
        url.includes('?')
            ? '&'
            : '?';

    return `${url}${separator}limit=${normalizedLimit}`;
}

export const endpointScenarios = [

    {
        key: 'getShells',
        name: 'GetShells',
        metricName: 'get_shells_duration',
        resolveUrl: ({ baseUrl, config }) =>
            appendLimitQuery(
                `${baseUrl}/shells`,
                config.endpointLimits?.getShells
            )
    },

    {
        key: 'getShellById',
        name: 'GetShellById',
        metricName: 'get_shell_by_id_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'shellIds',
        resolveUrl: fromRandomId(
            'shellIds',
            id => `/shells/${id}`
        )
    },

    {
        key: 'getAssetInformation',
        name: 'GetAssetInformation',
        metricName: 'get_asset_information_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'shellIds',
        resolveUrl: fromRandomId(
            'shellIds',
            id => `/shells/${id}/asset-information`
        )
    },

    {
        key: 'getSubmodelReferences',
        name: 'GetSubmodelReferences',
        metricName: 'get_submodel_references_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'shellIds',
        resolveUrl: fromRandomId(
            'shellIds',
            id => `/shells/${id}/submodel-refs`
        )
    },

    {
        key: 'getShellDescriptors',
        name: 'GetShellDescriptors',
        metricName: 'get_shell_descriptors_duration',
        resolveUrl: ({ baseUrl, config }) =>
            appendLimitQuery(
                `${baseUrl}/shell-descriptors`,
                config.endpointLimits?.getShellDescriptors
            )
    },

    {
        key: 'getShellDescriptorById',
        name: 'GetShellDescriptorById',
        metricName: 'get_shell_descriptor_by_id_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'shellIds',
        resolveUrl: fromRandomId(
            'shellIds',
            id => `/shell-descriptors/${id}`
        )
    },

    {
        key: 'getSubmodelDescriptors',
        name: 'GetSubmodelDescriptors',
        metricName: 'get_submodel_descriptors_duration',
        resolveUrl: ({ baseUrl, config }) =>
            appendLimitQuery(
                `${baseUrl}/submodel-descriptors`,
                config.endpointLimits?.getSubmodelDescriptors
            )
    },

    {
        key: 'getSubmodelDescriptorById',
        name: 'GetSubmodelDescriptorById',
        metricName: 'get_submodel_descriptor_by_id_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'submodelIds',
        resolveUrl: fromRandomId(
            'submodelIds',
            id => `/submodel-descriptors/${id}`
        )
    },

    {
        key: 'getSubmodels',
        name: 'GetSubmodels',
        metricName: 'get_submodels_duration',
        resolveUrl: ({ baseUrl, config }) =>
            appendLimitQuery(
                `${baseUrl}/submodels`,
                config.endpointLimits?.getSubmodels
            )
    },

    {
        key: 'getSubmodelById',
        name: 'GetSubmodelById',
        metricName: 'get_submodel_by_id_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'submodelIds',
        resolveUrl: fromRandomId(
            'submodelIds',
            id => `/submodels/${id}`
        )
    },

    {
        key: 'loadAllShellDescriptors',
        name: 'LoadAllShellDescriptors',
        metricName: 'load_all_data_duration',
        requestMode: 'paged',
        resolveUrl: ({ baseUrl, config }) =>
            `${baseUrl}/shell-descriptors?limit=${config.loadAllShellDescriptors.limit}`
    }
];

const endpointScenarioMap =
    Object.fromEntries(
        endpointScenarios.map(
            endpoint => [
                endpoint.key,
                endpoint
            ]
        )
    );

export const endpointMetricNames =
    endpointScenarios.map(
        endpoint => endpoint.metricName
    );

export function getEnabledIdDependentEndpointKeys(config) {

    return endpointScenarios
        .filter(endpoint =>
            endpoint.requiresDiscoveredIds &&
            config.endpoints[endpoint.key]
        )
        .map(endpoint => endpoint.key);
}

export function getEnabledDiscoveryRequirements(config) {

    const requirements = {};

    endpointScenarios
        .filter(endpoint =>
            endpoint.requiresDiscoveredIds &&
            endpoint.requiredDataProperty &&
            config.endpoints[endpoint.key]
        )
        .forEach(endpoint => {

            const key =
                endpoint.requiredDataProperty;

            if (!requirements[key]) {
                requirements[key] = [];
            }

            requirements[key].push(endpoint.key);
        });

    return requirements;
}

export function resolveScenarioByKey(
    config,
    data,
    endpointKey
) {

    const endpoint =
        endpointScenarioMap[
            endpointKey
        ];

    if (
        !endpoint ||
        !config.endpoints[
            endpointKey
        ]
    ) {
        return null;
    }

    const url =
        endpoint.resolveUrl({
            baseUrl:
                config.baseUrl,
            config,
            data
        });

    if (!url) {
        return null;
    }

    return {
        ...endpoint,
        url
    };
}