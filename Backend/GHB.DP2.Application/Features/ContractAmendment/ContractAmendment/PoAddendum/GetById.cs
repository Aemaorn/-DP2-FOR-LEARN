namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;

public class GetById
{
    public record GetPoAddendumByIdRequest(CamContractAmendmentId CamContractAmendmentId, CamContractAmendmentPoAddendumId? Id);

    public record PoAddendumDocumentVersionResponse(
        Guid FileId,
        string Version,
        DateTimeOffset CreatedAt,
        string CreatedByName,
        bool IsCurrent);

    public record PaymentTermDto(
        Guid? Id,
        int PaymentTermNo,
        int LeadTime,
        DateTimeOffset? DeliveryDate,
        decimal InstallmentPercentage,
        decimal Amount,
        decimal AdvanceDeductionAmount,
        decimal PerformanceDeductionAmount,
        string Title,
        string Description,
        int Sequence);

    public record ContractInfo(
        string ContractNo,
        Guid? VendorId,
        string VendorName,
        string SapNumber,
        string? PoNumber);

    public record PoAddendumVendorInfo(Guid VendorId, string TaxpayerIdentificationNo, string EstablishmentName, string Email);

    public record GetPoAddendumByIdResponse(
        CamContractAmendmentPoAddendumId? Id,
        CamContractAmendmentId CamContractAmendmentId,
        Guid? ContractAddendumDocumentId,
        bool? IsContractAddendumDocumentIdReplaced,
        Guid? ContractAmendmentRequestDocumentId,
        bool? IsContractAmendmentRequestDocumentIdReplaced,
        ContractInfo? OldContract,
        ContractInfo? NewContract,
        CamContractAmendmentPoAddendumStatus Status,
        PoAddendumVendorInfo? Vendor,
        IEnumerable<AcceptorNoIdResponse>? Acceptors,
        IEnumerable<AssigneeNoIdResponse>? Assignees,
        IEnumerable<PaymentTermDto>? NewPaymentTerms,
        IEnumerable<PaymentTermDto>? OldPaymentTerms,
        PoAddendumDocumentVersionResponse[]? ContractAddendumDocumentVersions,
        PoAddendumDocumentVersionResponse[]? ContractAmendmentRequestDocumentVersions);

    public class GetPoAddendumByIdEndpoint : PoAddendumAbstractEndpoint<GetPoAddendumByIdRequest, Results<Ok<GetPoAddendumByIdResponse>, NotFound<string>>>
    {
        private readonly Dp2DbContext dbContext;

        public GetPoAddendumByIdEndpoint(Dp2DbContext dbContext, ILogger<GetPoAddendumByIdEndpoint> logger)
            : base(logger, dbContext)
        {
            this.dbContext = dbContext;
        }

        public override void Configure()
        {
            this.Get("contract-amendment/{CamContractAmendmentId:guid}/po-addendum/{Id:guid?}");
            this.Description(b =>
                b.WithTags("ContractAmendment/PoAddendum")
                 .Produces<GetPoAddendumByIdResponse>()
                 .Produces<string>(StatusCodes.Status404NotFound));
        }

        protected override async ValueTask<Results<Ok<GetPoAddendumByIdResponse>, NotFound<string>>> HandleRequestAsync(GetPoAddendumByIdRequest req, CancellationToken ct)
        {
            var cam = await this.GetContractAmendmentWithDetailsAsync(req.CamContractAmendmentId, ct);
            if (cam is null || cam.ContractDraftVendor is null)
            {
                return TypedResults.NotFound("ไม่พบข้อมูลการแก้ไขสัญญาหรือคู่ค้าสัญญาที่เกี่ยวข้อง");
            }

            var oldPaymentTerms = GetOldPaymentTerms(cam.ContractDraftVendor);

            if (req.Id is null)
            {
                return await this.HandleNewPoAddendumRequestAsync(req, cam, oldPaymentTerms, ct);
            }

            return await this.HandleExistingPoAddendumRequestAsync(req, cam, oldPaymentTerms, ct);
        }

        private async Task<CamContractAmendment?> GetContractAmendmentWithDetailsAsync(CamContractAmendmentId camContractAmendmentId, CancellationToken ct)
        {
            return await this.dbContext.CamContractAmendments
                                .Include(c => c.ContractDraftVendor)
                                .ThenInclude(v => v.PaymentTerms)
                                .Include(c => c.ContractDraftVendor)
                                .ThenInclude(v => v.Vendor)
                                .ThenInclude(v => v.VendorInfo)
                                .Include(c => c.ContractDraftVendor)
                                .ThenInclude(cd => cd.ContractDraft)
                                .ThenInclude(p => p.Procurement)
                                .AsSplitQuery()
                                .SingleOrDefaultAsync(c => c.Id == camContractAmendmentId, ct);
        }

        private static IEnumerable<PaymentTermDto> GetOldPaymentTerms(CaContractDraftVendor contractDraftVendor)
        {
            return contractDraftVendor.PaymentTerms
                                     .OrderBy(p => p.Sequence)
                                     .Select(p => new PaymentTermDto(
                                         p.Id.Value,
                                         p.PaymentTermNo ?? 0,
                                         p.LeadTime ?? 0,
                                         p.DeliveryDate,
                                         p.InstallmentPercentage ?? 0,
                                         p.Amount ?? 0,
                                         p.AdvanceDeductionAmount ?? 0,
                                         p.PerformanceDeductionAmount ?? 0,
                                         string.Empty,
                                         p.Description ?? string.Empty,
                                         p.Sequence));
        }

        private async Task<Results<Ok<GetPoAddendumByIdResponse>, NotFound<string>>> HandleNewPoAddendumRequestAsync(
            GetPoAddendumByIdRequest req,
            CamContractAmendment cam,
            IEnumerable<PaymentTermDto> oldPaymentTerms,
            CancellationToken ct)
        {
            var committeeRes = await this.GetDefaultCommitteeAsync(cam.ContractDraftVendor, ct);
            var defaultAssignees = await this.GetDefaultAssigneesAsync(ct);
            var vendorInfo = GetVendorInfo(cam.ContractDraftVendor.Vendor?.VendorInfo);

            var response = this.CreateNewPoAddendumResponse(req, cam, vendorInfo, committeeRes, defaultAssignees, oldPaymentTerms);
            return TypedResults.Ok(response);
        }

        private async Task<List<AcceptorNoIdResponse>> GetDefaultCommitteeAsync(CaContractDraftVendor contractDraftVendor, CancellationToken ct)
        {
            var inspectCommittee = contractDraftVendor.ContractDraft.Procurement.Type is ProcurementType.Procurement
                ? await this.GetProcurementCommitteeAsync(contractDraftVendor.ContractDraft.Procurement.Id, ct)
                : await this.GetPrincipleApprovalCommitteeAsync(contractDraftVendor.ContractDraft.Procurement.Id, ct);

            return inspectCommittee;
        }

        private async Task<List<AcceptorNoIdResponse>> GetProcurementCommitteeAsync(ProcurementId procurementId, CancellationToken ct)
        {
            var jp005 = await this.dbContext.PJp005S
                          .FirstOrDefaultAsync(w => w.ProcurementId == procurementId, ct);

            if (jp005 is null)
            {
                return await this.dbContext.PPurchaseOrderApprovals
                    .Where(w => w.ProcurementId == procurementId)
                    .SelectMany(s => s.Committees)
                    .Include(u => u.User)
                    .ThenInclude(e => e.Employee)
                    .ThenInclude(v => v.View)
                    .Where(w => w.GroupType == GroupType.InspectionCommittee)
                    .OrderBy(o => o.Sequence)
                    .AsAsyncEnumerable()
                    .Select(
                        s => CreateAcceptorResponse(
                            new AcceptorInfo(
                                s.SuUserId.Value,
                                s.Sequence,
                                s.FullName,
                                s.FullPositionName,
                                s.User.Employee.View != null ? s.User.Employee.View.BusinessUnitName : string.Empty,
                                s.CommitteePositionsCode.Value,
                                s.CommitteePositionsName,
                                s.User.Employee.View != null ? (string)s.User.Employee.View.BusinessUnitId : string.Empty)))
                    .ToListAsync(ct);
            }

            return await this.dbContext.PJp005S
                .Where(w => w.ProcurementId == procurementId)
                .SelectMany(s => s.Committees)
                .Include(u => u.User)
                .ThenInclude(e => e.Employee)
                .ThenInclude(v => v.View)
                .Where(w => w.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                .OrderBy(o => o.Sequence)
                .AsAsyncEnumerable()
                .Select(
                    s => CreateAcceptorResponse(
                        new AcceptorInfo(
                            s.SuUserId.Value,
                            s.Sequence,
                            s.FullName,
                            s.FullPositionName,
                            s.User.Employee.View != null ? s.User.Employee.View.BusinessUnitName : string.Empty,
                            s.CommitteePositionsCode.Value,
                            s.CommitteePositionsName,
                            s.User.Employee.View != null ? (string)s.User.Employee.View.BusinessUnitId : string.Empty)))
                .ToListAsync(ct);
        }

        private async Task<List<AcceptorNoIdResponse>> GetPrincipleApprovalCommitteeAsync(ProcurementId procurementId, CancellationToken ct)
        {
            return await this.dbContext.PPrincipleApprovals
                .Where(w => w.ProcurementId == procurementId)
                .SelectMany(s => s.PrincipleApprovalCommittees)
                .Where(w => w.GroupType == CommitteeGroupType.AcceptanceCommittee)
                .OrderBy(o => o.Sequence)
                .AsAsyncEnumerable()
                .Select(s => CreateAcceptorResponse(new AcceptorInfo(
                    s.SuUserId.Value,
                    s.Sequence,
                    s.FullName,
                    s.FullPositionName,
                    s.User.Employee.View != null ? s.User.Employee.View.BusinessUnitName : string.Empty,
                    s.CommitteePositionsCode.Value,
                    s.CommitteePositionsName,
                    s.User.Employee.View != null ? (string)s.User.Employee.View.BusinessUnitId : string.Empty)))
                .ToListAsync(ct);
        }

        private record AcceptorInfo(
            Guid UserId,
            int Sequence,
            string FullName,
            string FullPositionName,
            string BusinessUnitName,
            string CommitteePositionsCode,
            string CommitteePositionsName,
            string BusinessUnitId);

        private static AcceptorNoIdResponse CreateAcceptorResponse(AcceptorInfo acceptorInfo)
        {
            return new AcceptorNoIdResponse(
                null,
                AcceptorType.AcceptanceCommittee,
                acceptorInfo.UserId,
                acceptorInfo.Sequence,
                acceptorInfo.FullName,
                acceptorInfo.FullPositionName,
                acceptorInfo.BusinessUnitName,
                AcceptorStatus.Draft,
                null,
                null,
                acceptorInfo.CommitteePositionsCode,
                acceptorInfo.CommitteePositionsName,
                false,
                acceptorInfo.BusinessUnitId,
                null,
                false);
        }

        private async Task<List<AssigneeNoIdResponse>> GetDefaultAssigneesAsync(CancellationToken ct)
        {
            var defaultAssignees = new List<AssigneeNoIdResponse>();
            var jorPorSectionHead = await this.GetJorPorSectionHeadAsync(ct);

            if (jorPorSectionHead != null)
            {
                var director = CreateDirectorAssignee(jorPorSectionHead);
                defaultAssignees.Add(director);
            }

            return defaultAssignees;
        }

        private async Task<SuUser?> GetJorPorSectionHeadAsync(CancellationToken ct)
        {
            return await this.dbContext.RawEmployeePositions
                              .Include(r => r.Employee)
                              .ThenInclude(r => r.View)
                              .Where(p =>
                                  p.BusinessUnitId == BusinessUnitId.From(JorPor.DefaultSectionHead.BusinessUnitId) &&
                                  p.Position.Name == JorPor.DefaultSectionHead.PositionName)
                              .Select(p => p.Employee)
                              .SelectMany(e => e.Users)
                              .FirstOrDefaultAsync(ct);
        }

        private static AssigneeNoIdResponse CreateDirectorAssignee(SuUser jorPorSectionHead)
        {
            return new AssigneeNoIdResponse(
                null,
                AssigneeGroup.Contract,
                AssigneeType.Director,
                jorPorSectionHead.Id.Value,
                1,
                jorPorSectionHead.Employee.View?.FullName ?? string.Empty,
                jorPorSectionHead.Employee.View?.FullPositionName ?? string.Empty,
                jorPorSectionHead.Employee.View?.BusinessUnitName ?? string.Empty,
                AssigneeStatus.Draft);
        }

        private static PoAddendumVendorInfo? GetVendorInfo(SuVendor? vendorInfo)
        {
            return vendorInfo != null
                ? new PoAddendumVendorInfo(vendorInfo.Id.Value, vendorInfo.TaxpayerIdentificationNo, vendorInfo.EstablishmentName, vendorInfo.Email)
                : null;
        }

        private GetPoAddendumByIdResponse CreateNewPoAddendumResponse(
            GetPoAddendumByIdRequest req,
            CamContractAmendment cam,
            PoAddendumVendorInfo? vendorInfo,
            List<AcceptorNoIdResponse> committeeRes,
            List<AssigneeNoIdResponse> defaultAssignees,
            IEnumerable<PaymentTermDto> oldPaymentTerms)
        {
            return new GetPoAddendumByIdResponse(
                null,
                req.CamContractAmendmentId,
                null,
                null,
                null,
                null,
                CreateOldContractInfo(cam.ContractDraftVendor, vendorInfo),
                CreateNewContractInfo(cam.ContractDraftVendor, vendorInfo),
                CamContractAmendmentPoAddendumStatus.Draft,
                vendorInfo,
                committeeRes,
                defaultAssignees,
                [],
                oldPaymentTerms,
                null,
                null);
        }

        private static ContractInfo CreateOldContractInfo(CaContractDraftVendor contractDraftVendor, PoAddendumVendorInfo? vendorInfo)
        {
            return new ContractInfo(
                contractDraftVendor.ContractNumber,
                vendorInfo?.VendorId,
                vendorInfo?.EstablishmentName ?? string.Empty,
                string.Empty,
                contractDraftVendor.PoNumber);
        }

        private static ContractInfo CreateNewContractInfo(CaContractDraftVendor contractDraftVendor, PoAddendumVendorInfo? vendorInfo)
        {
            return new ContractInfo(
                string.Concat(contractDraftVendor.ContractNumber, "(2)"),
                vendorInfo?.VendorId,
                vendorInfo?.EstablishmentName ?? string.Empty,
                string.Empty,
                contractDraftVendor.PoNumber);
        }

        private async Task<Results<Ok<GetPoAddendumByIdResponse>, NotFound<string>>> HandleExistingPoAddendumRequestAsync(
            GetPoAddendumByIdRequest req,
            CamContractAmendment cam,
            IEnumerable<PaymentTermDto> oldPaymentTerms,
            CancellationToken ct)
        {
            var po = await this.GetPoAddendumWithDetailsAsync(req.Id!.Value, req.CamContractAmendmentId, ct);
            if (po is null)
            {
                return TypedResults.NotFound("ไม่พบข้อมูลบันทึกต่อท้าย ที่ระบุ");
            }

            var newPaymentTerms = GetNewPaymentTerms(po);
            var acceptors = GetAcceptors(po);
            var assignees = GetAssignees(po);
            var oldVendorInfo = GetVendorInfo(cam.ContractDraftVendor.Vendor?.VendorInfo);
            var newVendorInfo = GetVendorInfo(po.Vendor);

            var result = this.CreateExistingPoAddendumResponse(po, cam, oldVendorInfo, newVendorInfo, acceptors, assignees, newPaymentTerms, oldPaymentTerms);
            return TypedResults.Ok(result);
        }

        private async Task<CamContractAmendmentPoAddendum?> GetPoAddendumWithDetailsAsync(CamContractAmendmentPoAddendumId poAddendumId, CamContractAmendmentId camContractAmendmentId, CancellationToken ct)
        {
            return await this.dbContext.CamContractAmendmentPoAddendums
                               .Include(p => p.Vendor)
                               .Include(p => p.Acceptors).ThenInclude(a => a.User)
                               .Include(p => p.Acceptors).ThenInclude(a => a.CommitteePosition)
                               .Include(p => p.Assignees).ThenInclude(a => a.User)
                               .Include(p => p.PaymentTerms)
                               .Include(p => p.CamContractAmendment)
                               .Include(p => p.DocumentHistories)
                               .AsSplitQuery()
                               .SingleOrDefaultAsync(p => p.Id == poAddendumId && p.CamContractAmendmentId == camContractAmendmentId, ct);
        }

        private static IEnumerable<PaymentTermDto> GetNewPaymentTerms(CamContractAmendmentPoAddendum po)
        {
            return po.PaymentTerms.OrderBy(p => p.Sequence)
                                  .Select(p => new PaymentTermDto(
                                      p.Id.Value,
                                      p.PaymentTermNo,
                                      p.LeadTime,
                                      p.DeliveryDate,
                                      p.InstallmentPercentage,
                                      p.Amount,
                                      p.AdvanceDeductionAmount,
                                      p.PerformanceDeductionAmount,
                                      p.Title,
                                      p.Description,
                                      p.Sequence));
        }

        private static List<AcceptorNoIdResponse> GetAcceptors(CamContractAmendmentPoAddendum po)
        {
            var currentAcceptors = po.Acceptors
                                       .Where(x => x.Type != AcceptorType.AcceptanceCommittee)
                                       .Select(DelegatorExtensions.DelegatorToAcceptor)
                                       .ToList();

            var currentCommittees = po.Acceptors
                                       .Where(x => x.Type == AcceptorType.AcceptanceCommittee)
                                       .ToList();

            return [.. currentAcceptors.Union(currentCommittees)
                                   .OrderBy(a => a.Sequence)
                                   .Select(a => new AcceptorNoIdResponse(
                                       a.Id.Value,
                                       a.Type,
                                       a.UserId.Value,
                                       a.Sequence,
                                       a.FullName,
                                       a.PositionName,
                                       a.BusinessUnitName,
                                       a.Status,
                                       a.Remark,
                                       a.ActionAt,
                                       a.CommitteePositionsCode.HasValue ? (string)a.CommitteePositionsCode : string.Empty,
                                       a.CommitteePosition?.Label ?? string.Empty,
                                       a.IsUnableToPerformDuties,
                                       string.Empty,
                                       a.DelegateeId?.Value,
                                       a.IsCurrentApprover(),
                                       a.Delegatee?.SuUserId.Value))];
        }

        private static List<AssigneeNoIdResponse> GetAssignees(CamContractAmendmentPoAddendum po)
        {
            return [.. po.Assignees
                     .Where(a => !a.IsDeleted)
                     .OrderBy(a => a.Sequence)
                     .Select(DelegatorExtensions.DelegatorToAssignee)
                     .Select(a => new AssigneeNoIdResponse(
                         a.Id.Value,
                         a.Group,
                         a.Type,
                         a.UserId.Value,
                         a.Sequence,
                         a.FullName,
                         a.PositionName,
                         a.BusinessUnitName,
                         a.Status,
                         a.Remark,
                         a.ActionAt,
                         a.Delegatee?.SuUserId.Value))];
        }

        private GetPoAddendumByIdResponse CreateExistingPoAddendumResponse(
            CamContractAmendmentPoAddendum po,
            CamContractAmendment cam,
            PoAddendumVendorInfo? oldVendorInfo,
            PoAddendumVendorInfo? newVendorInfo,
            List<AcceptorNoIdResponse> acceptors,
            List<AssigneeNoIdResponse> assignees,
            IEnumerable<PaymentTermDto> newPaymentTerms,
            IEnumerable<PaymentTermDto> oldPaymentTerms)
        {
            var contractAddendumVersions = po.DocumentHistories
                .Where(d => d.DocumentType == CamContractAmendmentPoAddendumDocumentType.ContractAddendum)
                .OrderVersions()
                .Select(d => new PoAddendumDocumentVersionResponse(
                    d.FileId.Value,
                    d.Version,
                    d.CreatedAt,
                    d.CreatedByName ?? string.Empty,
                    d.FileId == po.LastedContractAddendumDocument?.FileId))
                .ToArray();

            var contractAmendmentRequestVersions = po.DocumentHistories
                .Where(d => d.DocumentType == CamContractAmendmentPoAddendumDocumentType.ContractAmendmentRequest)
                .OrderVersions()
                .Select(d => new PoAddendumDocumentVersionResponse(
                    d.FileId.Value,
                    d.Version,
                    d.CreatedAt,
                    d.CreatedByName ?? string.Empty,
                    d.FileId == po.LastedContractAmendmentRequestDocument?.FileId))
                .ToArray();

            return new GetPoAddendumByIdResponse(
                po.Id,
                po.CamContractAmendmentId,
                po.LastedContractAddendumDocument?.FileId.Value,
                po.LastedContractAddendumDocument?.IsReplaced,
                po.LastedContractAmendmentRequestDocument?.FileId.Value,
                po.LastedContractAmendmentRequestDocument?.IsReplaced,
                CreateOldContractInfoForExisting(cam.ContractDraftVendor, oldVendorInfo),
                CreateNewContractInfoForExisting(po),
                po.Status,
                newVendorInfo,
                acceptors,
                assignees,
                newPaymentTerms,
                oldPaymentTerms,
                contractAddendumVersions,
                contractAmendmentRequestVersions);
        }

        private static ContractInfo CreateOldContractInfoForExisting(CaContractDraftVendor contractDraftVendor, PoAddendumVendorInfo? oldVendorInfo)
        {
            return new ContractInfo(
                contractDraftVendor.ContractNumber,
                oldVendorInfo?.VendorId,
                oldVendorInfo?.EstablishmentName ?? string.Empty,
                string.Empty,
                contractDraftVendor.PoNumber);
        }

        private static ContractInfo CreateNewContractInfoForExisting(CamContractAmendmentPoAddendum po)
        {
            return new ContractInfo(
                po.ContractNumber,
                po.Vendor.Id.Value,
                po.Vendor.EstablishmentName,
                po.SapNumber,
                po.PoNumber);
        }
    }
}