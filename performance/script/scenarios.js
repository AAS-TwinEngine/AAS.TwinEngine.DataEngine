import encoding from 'k6/encoding';

function randomItem(items) {

	return items[
		Math.floor(Math.random() * items.length)
	];
}

function toBase64Url(value) {

	return encoding.b64encode(
		value,
		'rawurl'
	);
}

function fromRandomShellId(pathBuilder) {

	return ({ baseUrl, data }) => {

		if (!data.shellIds || data.shellIds.length === 0) {
			return null;
		}

		const id = toBase64Url(
			randomItem(data.shellIds)
		);

		return `${baseUrl}${pathBuilder(id)}`;
	};
}

function fromRandomSubmodelId(pathBuilder) {

	return ({ baseUrl, data }) => {

		if (!data.submodelIds || data.submodelIds.length === 0) {
			return null;
		}

		const id = toBase64Url(
			randomItem(data.submodelIds)
		);

		return `${baseUrl}${pathBuilder(id)}`;
	};
}

export const endpointScenarios = [
	{
		key: 'getShells',
		name: 'GetShells',
		metricName: 'get_shells_duration',
		resolveUrl: ({ baseUrl }) => `${baseUrl}/shells`
	},
	{
		key: 'getShellById',
		name: 'GetShellById',
		metricName: 'get_shell_by_id_duration',
		resolveUrl: fromRandomShellId(id => `/shells/${id}`)
	},
	{
		key: 'getAssetInformation',
		name: 'GetAssetInformation',
		metricName: 'get_asset_information_duration',
		resolveUrl: fromRandomShellId(
			id => `/shells/${id}/asset-information`
		)
	},
	{
		key: 'getSubmodelReferences',
		name: 'GetSubmodelReferences',
		metricName: 'get_submodel_references_duration',
		resolveUrl: fromRandomShellId(
			id => `/shells/${id}/submodel-refs`
		)
	},
	{
		key: 'getShellDescriptors',
		name: 'GetShellDescriptors',
		metricName: 'get_shell_descriptors_duration',
		resolveUrl: ({ baseUrl }) => `${baseUrl}/shell-descriptors`
	},
	{
		key: 'getShellDescriptorById',
		name: 'GetShellDescriptorById',
		metricName: 'get_shell_descriptor_by_id_duration',
		resolveUrl: fromRandomShellId(
			id => `/shell-descriptors/${id}`
		)
	},
	{
		key: 'getSubmodelDescriptors',
		name: 'GetSubmodelDescriptors',
		metricName: 'get_submodel_descriptors_duration',
		resolveUrl: ({ baseUrl }) => `${baseUrl}/submodel-descriptors`
	},
	{
		key: 'getSubmodelDescriptorById',
		name: 'GetSubmodelDescriptorById',
		metricName: 'get_submodel_descriptor_by_id_duration',
		resolveUrl: fromRandomSubmodelId(
			id => `/submodel-descriptors/${id}`
		)
	},
	{
		key: 'getSubmodels',
		name: 'GetSubmodels',
		metricName: 'get_submodels_duration',
		resolveUrl: ({ baseUrl }) => `${baseUrl}/submodels`
	},
	{
		key: 'getSubmodelById',
		name: 'GetSubmodelById',
		metricName: 'get_submodel_by_id_duration',
		resolveUrl: fromRandomSubmodelId(id => `/submodels/${id}`)
	}
];

export function resolveExecutableScenarios(config, data) {

	return endpointScenarios
		.filter(endpoint => config.endpoints[endpoint.key])
		.map(endpoint => {

			const url = endpoint.resolveUrl({
				baseUrl: config.baseUrl,
				data
			});

			if (!url) {
				return null;
			}

			return {
				key: endpoint.key,
				name: endpoint.name,
				metricName: endpoint.metricName,
				url
			};
		})
		.filter(Boolean);
}

export function resolveScenarioByKey(config, data, endpointKey) {

	if (!endpointKey || !config.endpoints[endpointKey]) {
		return null;
	}

	const endpoint = endpointScenarios
		.find(item => item.key === endpointKey);

	if (!endpoint) {
		return null;
	}

	const url = endpoint.resolveUrl({
		baseUrl: config.baseUrl,
		data
	});

	if (!url) {
		return null;
	}

	return {
		key: endpoint.key,
		name: endpoint.name,
		metricName: endpoint.metricName,
		url
	};
}

export function getEndpointMetricNames() {

	return endpointScenarios
		.map(endpoint => endpoint.metricName);
}
