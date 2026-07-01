namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation;
using GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateCertificateRequisitionRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid? ContractDraftVendorId,
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
    GetById.InspectionCommitteeSectionResponse? InspectionCommittees = null,
    string? SupplyMethodCode = null,
    string? SupplyMethodTypeCode = null,
    string? SupplyMethodSpecialTypeCode = null);

public class CreateCertificateRequisitionRequestValidator : Validator<CreateCertificateRequisitionRequest>
{
    public CreateCertificateRequisitionRequestValidator()
    {
        this.RuleFor(x => x.ContractDraftVendorId)
            .NotEmpty()
            .When(x => !x.IsManual.GetValueOrDefault())
            .WithMessage("ต้องระบุรหัสคู่สัญญา");

        this.RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("สถานะไม่ถูกต้อง");

        this.RuleFor(x => x.RequestReason)
            .NotEmpty()
            .WithMessage("ต้องระบุเหตุผลการขอรับหนังสือรับรอง");
    }
}

public class CreateCertificateRequisitionEndpoint
    : CertificateRequisitionEndpointBase<CreateCertificateRequisitionRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreateCertificateRequisitionEndpoint(
        Dp2DbContext dbContext,
        ILogger<CreateContractAgreementInvitationEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b => b.WithTags("ContractAmendment/CertificateRequisition"));
        this.Post("certificate-requisition");
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(
        CreateCertificateRequisitionRequest req,
        CancellationToken ct)
    {
        if (!req.IsManual.GetValueOrDefault())
        {
            await this.ValidateRequestAsync(req, ct);
        }

        var newCertificateNumber = await this.GenerateCertificateNumberAsync(ct);
        var newCertificateRequisition =
            CamCertificateRequisition.Create(
                req.ContractDraftVendorId.HasValue ? ContractDraftVendorId.From(req.ContractDraftVendorId.Value) : null,
                new CertificateRequisitionInfo(
                    newCertificateNumber,
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

        this.dbContext.CamCertificateRequisitions.Add(newCertificateRequisition);

        var acceptorRequests = req.InspectionCommittees is not null
            ? req.InspectionCommittees.Committees
                 .Select(x => new AcceptorRequest(
                     x.Id,
                     AcceptorType.AcceptanceCommittee,
                     x.UserId,
                     x.Sequence,
                     x.CommitteePositionsCode,
                     false))
                 .ToArray()
            : req.Acceptors.ToArray();

        var acceptors =
            await this.CreateAcceptorAsync(
                newCertificateRequisition.Id,
                req.Status,
                acceptorRequests,
                UserId.From(req.UserId));

        _ =
            acceptors
                .Map(newCertificateRequisition.AddAcceptor)
                .ToArray();

        newCertificateRequisition.UpdateStatus(req.Status);
        await this.dbContext.SaveChangesAsync(ct);

        var savedCertificateRequisition =
            await this.dbContext.CamCertificateRequisitions
                      .Include(cr => cr.SupplyMethodType)
                      .Include(cr => cr.Acceptors)
                      .FirstAsync(cr => cr.Id == newCertificateRequisition.Id, ct);

        await this.SetDefaultDocumentTemplateAsync(savedCertificateRequisition, ct);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(
            string.Empty,
            newCertificateRequisition.Id.Value);
    }

    private async Task ValidateRequestAsync(
        CreateCertificateRequisitionRequest req,
        CancellationToken ct)
    {
        // TODO: Check create permission : current user must be an acceptor (AcceptanceCommittee)
        var contractDraftVendorExisting =
            await this.dbContext.CaContractDraftVendors
                      .Include(cv => cv.DeliveryAcceptances)
                      .FirstOrDefaultAsync(
                          cv =>
                              cv.IsDeleted == false &&
                              cv.Id == ContractDraftVendorId.From(req.ContractDraftVendorId!.Value),
                          ct);

        if (contractDraftVendorExisting is null)
        {
            this.ThrowError(
                r => r.ContractDraftVendorId,
                $"ไม่พบข้อมูลคู่ค้าสัญญา",
                StatusCodes.Status404NotFound);
        }

        if (contractDraftVendorExisting.Status != ContractDraftVendorStatus.Approved)
        {
            this.ThrowError(
                r => req.ContractDraftVendorId,
                $"สถานะของคู่ค้าสัญญา {req.ContractDraftVendorId} ยังไม่ได้รับการอนุมัติ",
                StatusCodes.Status404NotFound);
        }

        if (contractDraftVendorExisting.DeliveryAcceptances.Any(da => da.Status != CmDeliveryAcceptanceStatus.Completed))
        {
            this.ThrowError(
                r => r.ContractDraftVendorId,
                $"ไม่พบข้อมูลการตรวจรับที่เสร็จสมบูรณ์สำหรับคู่ค้าสัญญา {req.ContractDraftVendorId}",
                StatusCodes.Status404NotFound);
        }
    }

    private async Task<CertificateNumber> GenerateCertificateNumberAsync(CancellationToken ct)
    {
        var currentYearPrefix = CertificateNumber.GetCertificateNumberYearPrefix();

        var latestCertificate =
            await this.dbContext.CamCertificateRequisitions
                      .OrderByDescending(cr => cr.CertificateNo)
                      .FirstOrDefaultAsync(ct);

        if (latestCertificate is null)
        {
            return CertificateNumber.New();
        }

        return !latestCertificate.CertificateNo.Value.StartsWith(currentYearPrefix)
            ? CertificateNumber.New()
            : latestCertificate.CertificateNo.Next();
    }

    private async ValueTask<IEnumerable<CamCertificateRequisitionAcceptor>> CreateAcceptorAsync(
        CamCertificateRequisitionId certificateRequisitionId,
        CamCertificateRequisitionStatus status,
        AcceptorRequest[] acceptors,
        UserId? sendToAcceptorId = null)
    {
        var acceptorUserIds =
            acceptors.Select(a => UserId.From(a.UserId));

        var users =
            await this.dbContext.SuUsers
                      .Include(e => e.Employee)
                      .ThenInclude(e => e.View)
                      .Where(u => acceptorUserIds.Contains(u.Id))
                      .ToArrayAsync(CancellationToken.None);

        this.ValidateUsers(
            users,
            [.. acceptorUserIds]);

        var acceptorUsers =
            acceptors.Join(
                users,
                acceptorRequest => acceptorRequest.UserId,
                user => user.Id.Value,
                (acceptorRequest, user) =>
                {
                    var acceptor = CamCertificateRequisitionAcceptor.Create(
                                                                        certificateRequisitionId,
                                                                        acceptorRequest.AcceptorType,
                                                                        user,
                                                                        acceptorRequest.Sequence,
                                                                        status)
                                                                    .SetIsUnableToPerformDuties(acceptorRequest.IsUnableToPerformDuties ?? false);

                    acceptor.SetSendToAcceptorId(sendToAcceptorId);

                    _ = string.IsNullOrWhiteSpace(acceptorRequest.CommitteePositionsCode)
                        ? acceptor.SetCommitteePositionsCode(null)
                        : acceptor.SetCommitteePositionsCode(
                            ParameterCode.From(acceptorRequest.CommitteePositionsCode));

                    return acceptor;
                });

        return acceptorUsers;
    }
}