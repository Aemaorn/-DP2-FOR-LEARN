namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateCertificateRequisitionRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid? ContractDraftVendorId,
    Guid Id,
    CamCertificateRequisitionStatus Status,
    DateTime? ReceiveDate,
    string? SbsDocumentNo,
    DateTimeOffset? DocumentDate,
    DateTimeOffset? IssuedDate,
    string? RequestReason,
    string? EntrepreneurName,
    Guid? EntrepreneurId,
    string? EntrepreneurEmail,
    string? ContractNumber,
    string? PoNumber,
    decimal? Budget,
    string? ContractName,
    DateTimeOffset? ContractSignedDate,
    DateTimeOffset? DeliveryDate,
    DateTimeOffset? ContractEndDate,
    bool? IsManual,
    IEnumerable<AcceptorRequest> Acceptors,
    Guid? DocumentId,
    bool? IsReplace,
    bool? IsResetDocument,
    GetById.InspectionCommitteeSectionResponse? InspectionCommittees = null,
    string? SupplyMethodCode = null,
    string? SupplyMethodTypeCode = null,
    string? SupplyMethodSpecialTypeCode = null);

public record UpdateCertificateRequisitionResponse(Guid Id, Guid? NewDocumentFileId);

public class UpdateCertificateRequisitionRequestValidator : Validator<UpdateCertificateRequisitionRequest>
{
    public UpdateCertificateRequisitionRequestValidator()
    {
        this.RuleFor(x => x.ContractDraftVendorId)
            .NotEmpty()
            .When(x => !x.IsManual.GetValueOrDefault())
            .WithMessage("ต้องระบุรหัสคู่สัญญา");

        this.RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ต้องระบุรหัสการขอใบรับรอง");

        this.RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("สถานะไม่ถูกต้อง");

        this.RuleFor(x => x.RequestReason)
            .NotEmpty()
            .WithMessage("ต้องระบุเหตุผลการขอรับหนังสือรับรอง");
    }
}

public class UpdateCertificateRequisitionEndpoint
    : CertificateRequisitionEndpointBase<UpdateCertificateRequisitionRequest, Results<Ok<UpdateCertificateRequisitionResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateCertificateRequisitionEndpoint(
        Dp2DbContext dbContext,
        ILogger<UpdateCertificateRequisitionEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b => b.WithTags("ContractAmendment/CertificateRequisition"));
        this.Put("certificate-requisition/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok<UpdateCertificateRequisitionResponse>, NotFound<string>>> HandleRequestAsync(
        UpdateCertificateRequisitionRequest req,
        CancellationToken ct)
    {
        var certificateRequisitionExisting = await this.ValidateRequestAsync(req, ct, req.IsManual.GetValueOrDefault());

        certificateRequisitionExisting.UpdateInfo(
            new CertificateRequisitionInfo(
                certificateRequisitionExisting.CertificateNo,
                req.ReceiveDate,
                req.SbsDocumentNo,
                req.DocumentDate,
                req.IssuedDate,
                req.RequestReason,
                req.EntrepreneurName,
                req.EntrepreneurId.HasValue ? SuVendorId.From(req.EntrepreneurId.Value) : null,
                req.EntrepreneurEmail,
                req.ContractNumber,
                req.PoNumber,
                req.Budget,
                req.ContractName,
                req.ContractSignedDate,
                req.DeliveryDate,
                req.ContractEndDate,
                req.IsManual,
                string.IsNullOrWhiteSpace(req.SupplyMethodCode) ? null : ParameterCode.From(req.SupplyMethodCode),
                string.IsNullOrWhiteSpace(req.SupplyMethodTypeCode) ? null : ParameterCode.From(req.SupplyMethodTypeCode),
                string.IsNullOrWhiteSpace(req.SupplyMethodSpecialTypeCode) ? null : ParameterCode.From(req.SupplyMethodSpecialTypeCode)));

        var acceptorRequests = req.InspectionCommittees is not null
            ? req.InspectionCommittees.Committees
                 .Select(x =>
                 {
                     var matching = req.Acceptors.FirstOrDefault(a => a.UserId == x.UserId);

                     return new AcceptorRequest(
                         x.Id,
                         AcceptorType.AcceptanceCommittee,
                         x.UserId,
                         x.Sequence,
                         x.CommitteePositionsCode,
                         matching?.IsUnableToPerformDuties);
                 })
                 .ToArray()
            : req.Acceptors.ToArray();

        await this.UpsertAcceptorAsync(
            certificateRequisitionExisting,
            acceptorRequests,
            ct,
            UserId.From(req.UserId));

        FileId? newDocumentFileId = null;

        var mustSaveDocument =
            req.DocumentId.HasValue &&
            req.IsReplace.GetValueOrDefault() &&
            req.Status != CamCertificateRequisitionStatus.WaitingForCommitteeApproval;

        if (mustSaveDocument)
        {
            newDocumentFileId = await this.UpdateDocumentHistoryAsync(
                certificateRequisitionExisting,
                FileId.From(req.DocumentId!.Value),
                req.IsReplace,
                ct);
        }

        certificateRequisitionExisting.UpdateStatus(req.Status);
        await this.UpdateDocumentTemplateAsync(certificateRequisitionExisting, ct, req.IsResetDocument);

        this.dbContext.CamCertificateRequisitions.Update(certificateRequisitionExisting);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new UpdateCertificateRequisitionResponse(certificateRequisitionExisting.Id.Value, newDocumentFileId?.Value));
    }

    private async Task<CamCertificateRequisition> ValidateRequestAsync(
        UpdateCertificateRequisitionRequest req,
        CancellationToken ct,
        bool isManual = false)
    {
        var certReqId = CamCertificateRequisitionId.From(req.Id);

        var certificateRequisitionExisting = isManual
            ? await this.dbContext.CamCertificateRequisitions
                        .FirstOrDefaultAsync(cr => cr.Id == certReqId, ct)
            : await this.GetById(
                ContractDraftVendorId.From(req.ContractDraftVendorId!.Value),
                certReqId,
                ct);

        if (certificateRequisitionExisting is null)
        {
            this.ThrowError(
                r =>
                    req.Id,
                $"ไม่พบข้อมูลการขอใบรับรอง {req.Id})",
                StatusCodes.Status404NotFound);
        }

        var canEdit =
            certificateRequisitionExisting.Status is
                CamCertificateRequisitionStatus.Draft or
                CamCertificateRequisitionStatus.Edit or
                CamCertificateRequisitionStatus.WaitingForCommitteeApproval or
                CamCertificateRequisitionStatus.Rejected;

        if (!canEdit)
        {
            this.ThrowError(
                r =>
                    req.Id,
                $"การขอใบรับรองที่ระบุไม่อยู่ในสถานะที่สามารถแก้ไขได้ (สถานะปัจจุบัน: {certificateRequisitionExisting.Status})",
                StatusCodes.Status409Conflict);
        }

        return certificateRequisitionExisting;
    }

    private async Task UpsertAcceptorAsync(
        CamCertificateRequisition certificateRequisitionExisting,
        AcceptorRequest[] acceptorsRequest,
        CancellationToken ct,
        UserId? sendToAcceptorId = null)
    {
        _ = certificateRequisitionExisting.Acceptors.Where(w => !acceptorsRequest.Select(s => s.Id).Contains(w.Id.Value))
                                          .Iter(s => certificateRequisitionExisting.RemoveAcceptor(s));

        var userIdsIncoming =
            acceptorsRequest.Map(s => s.UserId)
                            .Map(UserId.From)
                            .ToArray();

        var usersIncoming =
            await this.dbContext.SuUsers
                      .Include(r => r.Employee)
                      .ThenInclude(r => r.View)
                      .Where(w => userIdsIncoming.Contains(w.Id))
                      .ToArrayAsync(ct);

        var userNotExistsInDb
            = userIdsIncoming
              .Except(usersIncoming.Map(u => u.Id))
              .ToArray();

        if (userNotExistsInDb.Length > 0)
        {
            this.ThrowError(
                $"User with ID {string.Join(", ", userNotExistsInDb)} not found.",
                StatusCodes.Status404NotFound);
        }

        var newAcceptors =
            acceptorsRequest.Where(ar => !ar.Id.HasValue)
                            .Join(
                                usersIncoming,
                                a => a.UserId,
                                u => u.Id.Value,
                                (a, u) =>
                                {
                                    var acceptors = CamCertificateRequisitionAcceptor.Create(
                                        certificateRequisitionExisting.Id,
                                        a.AcceptorType,
                                        u,
                                        a.Sequence,
                                        certificateRequisitionExisting.Status);

                                    _ = acceptors.SetIsUnableToPerformDuties(a.IsUnableToPerformDuties ?? false);

                                    acceptors.SetSendToAcceptorId(sendToAcceptorId);

                                    _ = acceptors.IsUnableToPerformDuties switch
                                    {
                                        true => acceptors.UnableToPerformDuties(acceptors.Remark),
                                        false => acceptors.Draft(),
                                    };

                                    _ = string.IsNullOrWhiteSpace(a.CommitteePositionsCode) switch
                                    {
                                        true => acceptors.SetCommitteePositionsCode(null),
                                        false => acceptors.SetCommitteePositionsCode(
                                            ParameterCode.From(a.CommitteePositionsCode)),
                                    };

                                    return acceptors;
                                })
                            .ToHashSet();

        _ = certificateRequisitionExisting
            .Acceptors
            .Join(
                acceptorsRequest.Where(w => w.Id.HasValue),
                db => db.Id.Value,
                payload => payload.Id,
                (db, payload) =>
                {
                    db.SetSendToAcceptorId(sendToAcceptorId);
                    db.SetIsUnableToPerformDuties(payload.IsUnableToPerformDuties ?? false)
                      .SetSequence(payload.Sequence);

                    _ = db.IsUnableToPerformDuties switch
                    {
                        true => db.UnableToPerformDuties(db.Remark),
                        false => db.Draft(),
                    };

                    _ = string.IsNullOrWhiteSpace(payload.CommitteePositionsCode) switch
                    {
                        true => db.SetCommitteePositionsCode(null),
                        false => db.SetCommitteePositionsCode(
                            ParameterCode.From(payload.CommitteePositionsCode)),
                    };

                    return db;
                }).ToHashSet();

        _ = newAcceptors.Map(certificateRequisitionExisting.AddAcceptor)
                        .ToHashSet();
    }
}