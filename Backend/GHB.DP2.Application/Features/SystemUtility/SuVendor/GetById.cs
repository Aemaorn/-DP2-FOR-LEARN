namespace GHB.DP2.Application.Features.SystemUtility.SuVendor;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetSuVendorByIdRequest
{
    public Guid Id { get; init; }
}

public record GetSuVendorByIdResponse(
    SuVendorId Id,
    string Nationality,
    string Type,
    string EntrepreneurType,
    string TaxpayerIdentificationNo,
    string EstablishmentName,
    string PlaceName,
    Address Address,
    string? Tel,
    string? Fax,
    string SapVendorNumber,
    string SapBranchNumber,
    string Email,
    IEnumerable<SuVendorAttachments> Attachments);

public record SuVendorAttachments(
    SuVendorAttachmentId Id,
    int Sequence,
    string FileName,
    FileId FileId,
    bool IsPrivate,
    Guid CreateById);

public class GetSuVendorById : SecureEndpointBase<GetSuVendorByIdRequest, Results<Ok<GetSuVendorByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetSuVendorById(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<GetSuVendorById> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuVendor"));
        this.Get("/st/st003/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok<GetSuVendorByIdResponse>, NotFound<string>>> HandleRequestAsync(GetSuVendorByIdRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.SuVendors
                             .Include(s => s.EntrepreneurTypeInfo)
                             .Include(s => s.Attachments)
                             .FirstOrDefaultAsync(x => x.Id == SuVendorId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound($"GetSuVendor with Id {req.Id} not found");
        }

        var response = new GetSuVendorByIdResponse(
            data.Id,
            data.Nationality.ToString(),
            data.Type.ToString(),
            data.EntrepreneurType.Value,
            data.TaxpayerIdentificationNo,
            data.EstablishmentName,
            data.PlaceName,
            new Address(
                data.HouseNumber,
                data.RoomNumber,
                data.Floor,
                data.VillageName,
                data.Moo,
                data.Allay,
                data.Road,
                data.RawProvinceCode,
                data.RawDistrictCode,
                data.RawSubDistrictCode,
                data.PostalCode),
            data.Tel,
            data.Fax,
            data.SapVendorNumber,
            data.SapBranchNumber,
            data.Email,
            data.Attachments
            .OrderBy(x => x.Sequence)
            .Select(x => new SuVendorAttachments(
                x.Id,
                x.Sequence,
                x.FileName,
                x.FileId,
                x.IsPrivate,
                x.AuditInfo.CreatedBy)));

        return TypedResults.Ok(response);
    }
}