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

export const endpointScenarios = [

    {
        key: 'getShells',
        name: 'GetShells',
        metricName: 'get_shells_duration',
        resolveUrl: ({ baseUrl }) =>
            `${baseUrl}/shells`
    },

    {
        key: 'getShellById',
        name: 'GetShellById',
        metricName: 'get_shell_by_id_duration',
        resolveUrl: fromRandomId(
            'shellIds',
            id => `/shells/${id}`
        )
    },

    {
        key: 'getAssetInformation',
        name: 'GetAssetInformation',
        metricName: 'get_asset_information_duration',
        resolveUrl: fromRandomId(
            'shellIds',
            id => `/shells/${id}/asset-information`
        )
    },

    {
        key: 'getSubmodelReferences',
        name: 'GetSubmodelReferences',
        metricName: 'get_submodel_references_duration',
        resolveUrl: fromRandomId(
            'shellIds',
            id => `/shells/${id}/submodel-refs`
        )
    },

    {
        key: 'getShellDescriptors',
        name: 'GetShellDescriptors',
        metricName: 'get_shell_descriptors_duration',
        resolveUrl: ({ baseUrl }) =>
            `${baseUrl}/shell-descriptors`
    },

    {
        key: 'getShellDescriptorById',
        name: 'GetShellDescriptorById',
        metricName: 'get_shell_descriptor_by_id_duration',
        resolveUrl: fromRandomId(
            'shellIds',
            id => `/shell-descriptors/${id}`
        )
    },

    {
        key: 'getSubmodelDescriptors',
        name: 'GetSubmodelDescriptors',
        metricName: 'get_submodel_descriptors_duration',
        resolveUrl: ({ baseUrl }) =>
            `${baseUrl}/submodel-descriptors`
    },

    {
        key: 'getSubmodelDescriptorById',
        name: 'GetSubmodelDescriptorById',
        metricName: 'get_submodel_descriptor_by_id_duration',
        resolveUrl: fromRandomId(
            'submodelIds',
            id => `/submodel-descriptors/${id}`
        )
    },

    {
        key: 'getSubmodels',
        name: 'GetSubmodels',
        metricName: 'get_submodels_duration',
        resolveUrl: ({ baseUrl }) =>
            `${baseUrl}/submodels`
    },

    {
        key: 'getSubmodelById',
        name: 'GetSubmodelById',
        metricName: 'get_submodel_by_id_duration',
        resolveUrl: fromRandomId(
            'submodelIds',
            id => `/submodels/${id}`
        )
    },

    {
        key: 'loadAllData',
        name: 'LoadAllData',
        metricName: 'load_all_data_duration',
        requestMode: 'paged',
        resolveUrl: ({ baseUrl }) =>
            `${baseUrl}/shell-descriptors`
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