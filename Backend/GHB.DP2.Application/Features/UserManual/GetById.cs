namespace GHB.DP2.Application.Features.UserManual;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetUserManualByIdRequest
{
    public Guid Id { get; init; }
}

public record GetUserManualByIdResponse(
    Guid Id,
    string Code,
    string Name,
    FileId? PreviewPdfFileId,
    string? PreviewPdfFileName);

public class GetUserManualById : EndpointBase<GetUserManualByIdRequest, Results<Ok<GetUserManualByIdResponse>, NotFound<string>>>
{
    private const string UserManualGroup = "UserManual";

    private readonly Dp2DbContext dbContext;

    public GetUserManualById(
        Dp2DbContext dbContext,
        ILogger<GetUserManualById> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("UserManual"));
        this.Get("/user-manuals/{Id:guid}");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Results<Ok<GetUserManualByIdResponse>, NotFound<string>>> HandleRequestAsync(GetUserManualByIdRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.SuDocumentTemplates
                             .AsNoTracking()
                             .FirstOrDefaultAsync(
                                 x => x.Id == SuDocumentTemplateId.From(req.Id) &&
                                      x.Group == UserManualGroup &&
                                      x.IsActive,
                                 ct);

        if (data is null)
        {
            return TypedResults.NotFound($"UserManual with Id {req.Id} not found");
        }

        var response = new GetUserManualByIdResponse(
            data.Id.Value,
            data.Code,
            data.Name,
            data.PreviewPfdFileId,
            data.PreviewPfdFileName);

        return TypedResults.Ok(response);
    }
}
