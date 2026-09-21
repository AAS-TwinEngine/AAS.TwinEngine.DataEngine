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

function fromRandomIds(
    firstDataProperty,
    secondDataProperty,
    pathBuilder
) {

    return ({ baseUrl, data }) => {

        const firstIds =
            data[firstDataProperty];

        const secondIds =
            data[secondDataProperty];

        if (
            !firstIds ||
            firstIds.length === 0 ||
            !secondIds ||
            secondIds.length === 0
        ) {
            return null;
        }

        const firstEncodedId =
            toBase64Url(
                randomItem(firstIds)
            );

        const secondEncodedId =
            toBase64Url(
                randomItem(secondIds)
            );

        return `${baseUrl}${pathBuilder(firstEncodedId, secondEncodedId)}`;
    };
}

function fromRandomIdWithElementPath(
    dataProperty,
    pathBuilder
) {

    return ({ baseUrl, config, data }) => {

        if (!config.submodelElementPath) {
            return null;
        }

        const ids =
            data[dataProperty];

        if (!ids || ids.length === 0) {
            return null;
        }

        const encodedId =
            toBase64Url(
                randomItem(ids)
            );

        const encodedPath =
            encodeURIComponent(
                config.submodelElementPath
            );

        return `${baseUrl}${pathBuilder(encodedId, encodedPath)}`;
    };
}

function fromRandomIdsWithElementPath(
    pathBuilder
) {

    return ({ baseUrl, config, data }) => {

        if (!config.submodelElementPath) {
            return null;
        }

        const shellIds =
            data.shellIds;

        const submodelIds =
            data.submodelIds;

        if (
            !shellIds ||
            shellIds.length === 0 ||
            !submodelIds ||
            submodelIds.length === 0
        ) {
            return null;
        }

        const shellId =
            toBase64Url(
                randomItem(shellIds)
            );

        const submodelId =
            toBase64Url(
                randomItem(submodelIds)
            );

        const encodedPath =
            encodeURIComponent(
                config.submodelElementPath
            );

        return `${baseUrl}${pathBuilder(shellId, submodelId, encodedPath)}`;
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
        key: 'getAssetInformationThumbnail',
        name: 'GetAssetInformationThumbnail',
        metricName: 'get_asset_information_thumbnail_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'shellIds',
        resolveUrl: fromRandomId(
            'shellIds',
            id => `/shells/${id}/asset-information/thumbnail`
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
        key: 'getSubmodelByAasId',
        name: 'GetSubmodelByAasId',
        metricName: 'get_submodel_by_aas_id_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'shellIds',
        resolveUrl: fromRandomIds(
            'shellIds',
            'submodelIds',
            (shellId, submodelId) =>
                `/shells/${shellId}/submodels/${submodelId}`
        )
    },

    {
        key: 'getSubmodelElementsByAasId',
        name: 'GetSubmodelElementsByAasId',
        metricName: 'get_submodel_elements_by_aas_id_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'shellIds',
        resolveUrl: ({ baseUrl, config, data }) => {

            const shellIds = data.shellIds;
            const submodelIds = data.submodelIds;

            if (
                !shellIds?.length ||
                !submodelIds?.length
            ) {
                return null;
            }

            const shellId = toBase64Url(randomItem(shellIds));
            const submodelId = toBase64Url(randomItem(submodelIds));

            return appendLimitQuery(
                `${baseUrl}/shells/${shellId}/submodels/${submodelId}/submodel-elements`,
                config.endpointLimits?.getSubmodelElementsByAasId
            );
        }
    },

    {
        key: 'getSubmodelElementByAasId',
        name: 'GetSubmodelElementByAasId',
        metricName: 'get_submodel_element_by_aas_id_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'shellIds',
        resolveUrl: fromRandomIdsWithElementPath(
            (shellId, submodelId, path) =>
                `/shells/${shellId}/submodels/${submodelId}/submodel-elements/${path}`
        )
    },

    {
        key: 'getFileAttachmentByAasId',
        name: 'GetFileAttachmentByAasId',
        metricName: 'get_file_attachment_by_aas_id_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'shellIds',
        resolveUrl: fromRandomIdsWithElementPath(
            (shellId, submodelId, path) =>
                `/shells/${shellId}/submodels/${submodelId}/submodel-elements/${path}/attachment`
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
        key: 'getSubmodelDescriptorsByAasId',
        name: 'GetSubmodelDescriptorsByAasId',
        metricName: 'get_submodel_descriptors_by_aas_id_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'shellIds',
        resolveUrl: ({ baseUrl, config, data }) => {

            const shellIds = data.shellIds;

            if (!shellIds?.length) {
                return null;
            }

            const shellId = toBase64Url(randomItem(shellIds));

            return appendLimitQuery(
                `${baseUrl}/shell-descriptors/${shellId}/submodel-descriptors`,
                config.endpointLimits?.getSubmodelDescriptorsByAasId
            );
        }
    },

    {
        key: 'getSubmodelDescriptorByAasId',
        name: 'GetSubmodelDescriptorByAasId',
        metricName: 'get_submodel_descriptor_by_aas_id_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'shellIds',
        resolveUrl: fromRandomIds(
            'shellIds',
            'submodelIds',
            (shellId, submodelId) =>
                `/shell-descriptors/${shellId}/submodel-descriptors/${submodelId}`
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
        key: 'getSubmodelElements',
        name: 'GetSubmodelElements',
        metricName: 'get_submodel_elements_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'submodelIds',
        resolveUrl: ({ baseUrl, config, data }) => {

            const submodelIds = data.submodelIds;

            if (!submodelIds?.length) {
                return null;
            }

            const submodelId = toBase64Url(randomItem(submodelIds));

            return appendLimitQuery(
                `${baseUrl}/submodels/${submodelId}/submodel-elements`,
                config.endpointLimits?.getSubmodelElements
            );
        }
    },

    {
        key: 'getSubmodelElement',
        name: 'GetSubmodelElement',
        metricName: 'get_submodel_element_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'submodelIds',
        resolveUrl: fromRandomIdWithElementPath(
            'submodelIds',
            (submodelId, path) =>
                `/submodels/${submodelId}/submodel-elements/${path}`
        )
    },

    {
        key: 'getFileAttachment',
        name: 'GetFileAttachment',
        metricName: 'get_file_attachment_duration',
        requiresDiscoveredIds: true,
        requiredDataProperty: 'submodelIds',
        resolveUrl: fromRandomIdWithElementPath(
            'submodelIds',
            (submodelId, path) =>
                `/submodels/${submodelId}/submodel-elements/${path}/attachment`
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