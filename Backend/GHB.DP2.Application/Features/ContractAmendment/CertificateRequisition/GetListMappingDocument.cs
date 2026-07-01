namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition;

using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class GetListMappingCertificateRequisitionDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingCertificateRequisitionDocumentEndpoint(ILogger<GetListMappingCertificateRequisitionDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ContractAmendment/CertificateRequisition"));
        this.Get("contract-draft-vendor/certificate-requisition/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(CertificateRequisitionReplace);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}