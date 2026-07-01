namespace GHB.DP2.Application.Features.SystemUtility.SuDocumentTemplate;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetSuDocumentTemplatePdfByIdRequest
{
    public Guid Id { get; init; }
}

public class GetSuDocumentTemplatePdfById : EndpointBase<GetSuDocumentTemplatePdfByIdRequest, IResult>
{
    private readonly IFileServiceClient fileServiceClient;
    private readonly Dp2DbContext dbContext;

    public GetSuDocumentTemplatePdfById(
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext,
        ILogger<GetSuDocumentTemplatePdfById> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuDocumentTemplate"));
        this.Get("/st/st007/{id:guid}/pdf");
        this.AllowAnonymous();
    }

    protected override async ValueTask<IResult> HandleRequestAsync(GetSuDocumentTemplatePdfByIdRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.SuDocumentTemplates
                             .Include(suDocumentTemplate => suDocumentTemplate.BudgetForDocument)
                             .FirstOrDefaultAsync(x => x.Id == SuDocumentTemplateId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound($"SuDocumentTemplate with Id {req.Id} not found");
        }

        var file = await this.fileServiceClient.DownloadAsync(
            data.PreviewPfdFileId,
            cancellationToken: ct);

        if (file == null)
        {
            return TypedResults.NotFound($"File with ID {data.PreviewPfdFileId} not found.");
        }

        var fileMeta = await this.fileServiceClient.GetFileMetadataAsync(data.PreviewPfdFileId, CancellationToken.None);

        return Results.File(file.Contents, file.MimeType, fileMeta?.FileName ?? DateTimeOffset.Now.ToString());
    }
}