using System.Text.Json;

using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;

namespace AAS.TwinEngine.DataEngine.ModuleTests.ApplicationLogic.Services.SubmodelRegistry;

internal static class TestData
{
    public static SubmodelDescriptor CreateSubmodelDescriptor()
    {
        var json = """
            {
                "description": [
                    {
                        "language": "en",
                        "text": "The Submodel defines a set meta data for the handover of documentation."
                    }
                ],
                "displayName": null,
                "extensions": null,
                "administration": {
                    "embeddedDataSpecifications": null,
                    "version": "1",
                    "revision": "2",
                    "creator": null,
                    "templateId": null
                },
                "idShort": "HandoverDocumentation",
                "id": "http://twinengine/admin-shell.io",
                "semanticId": {
                    "type": "ModelReference",
                    "keys": [
                        {
                            "type": "Submodel",
                            "value": "0173-1#0001"
                        }
                    ],
                    "referredSemanticId": null
                },
                "supplementalSemanticId": [],
                "endpoints": [
                    {
                        "interface": "SUBMODEL-3.0",
                        "protocolInformation": {
                            "href": "",
                            "endpointProtocol": "http",
                            "endpointProtocolVersion": null,
                            "subprotocol": null,
                            "subprotocolBody": null,
                            "subprotocolBodyEncoding": null,
                            "securityAttributes": null
                        }
                    }
                ]
            }
            """;

        return JsonSerializer.Deserialize<SubmodelDescriptor>(json);
    }
}
