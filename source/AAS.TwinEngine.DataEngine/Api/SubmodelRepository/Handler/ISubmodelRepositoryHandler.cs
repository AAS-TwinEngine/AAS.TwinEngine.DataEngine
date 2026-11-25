using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.Shared.ContentModifiers;
﻿using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;

public interface ISubmodelRepositoryHandler
{
    Task<ISubmodel> GetSubmodel(GetSubmodelRequest request, CancellationToken cancellationToken);

    Task<ISubmodelElement> GetSubmodelElement(GetSubmodelElementRequest request, CancellationToken cancellationToken);

    Task<SubmodelsDto> GetAllSubmodels(GetAllSubmodelsRequest request, CancellationToken cancellationToken);

    Task<SubmodelElementsDto> GetAllSubmodelElements(GetAllSubmodelElementsRequest request, CancellationToken cancellationToken);

    Task<FileAttachmentResult> GetFileAttachment(GetSubmodelElementRequest request, CancellationToken cancellationToken);

    Task<JsonNode> GetSubmodelContent(GetSubmodelRequest request, Content content, CancellationToken cancellationToken);

    Task<JsonNode?> GetSubmodelElementContent(GetSubmodelElementRequest request, Content content, CancellationToken cancellationToken);

    Task<PagedContentDto> GetAllSubmodelsContent(GetAllSubmodelsRequest request, Content content, CancellationToken cancellationToken);

    Task<PagedContentDto> GetAllSubmodelElementsContent(GetAllSubmodelElementsRequest request, Content content, CancellationToken cancellationToken);
}
