namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class ApproveContractDraftRequest
{
    public Guid ProcurementId { get; set; }

    public Guid ContractDraftId { get; set; }

    public Guid VendorId { get; set; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; set; }

    public string? Remark { get; init; }
}

public class ApproveContractDraftEndpoint
    : ContractDraftEndpointBase<ApproveContractDraftRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ApproveContractDraftEndpoint(
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<GetVendorEndpoint> logger,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, commandTextService, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/contract-draft/{ContractDraftId:guid}/vendor/{VendorId:guid}/approve");
        this.Options(b =>
            b.WithTags(nameof(ContractDraft))
             .WithName("ApproveContractDraft")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApproveContractDraftRequest req, CancellationToken ct)
    {
        var contractDraft =
            await this.dbContext
                      .CaContractDrafts
                      .Include(c => c.Vendors)
                      .ThenInclude(v => v.Acceptors)
                      .ThenInclude(po => po.User)
                      .ThenInclude(po => po.Employee)
                      .Include(p => p.Procurement)
                      .Include(a => a.AuditInfo)
                      .FirstOrDefaultAsync(
                          c =>
                              c.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                              c.Id == ContractDraftId.From(req.ContractDraftId),
                          ct);

        if (contractDraft == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลร่างสัญญา");
        }

        var vendor = contractDraft.Vendors
                                  .FirstOrDefault(v =>
                                      v.Id == ContractDraftVendorId.From(req.VendorId));

        if (vendor == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้ขายในร่างสัญญานี้");
        }

        if (!vendor.IsPending)
        {
            return TypedResults.BadRequest("ร่างสัญญานี้ไม่อยู่ในสถานะรอการอนุมัติ");
        }

        var userid = UserId.From(req.UserId);
        var acceptor = vendor.Acceptors
                             .Map(DelegatorExtensions.DelegatorToAcceptor)
                             .FirstOrDefault(a =>
                                 a.Delegatee?.SuUserId == null
                                     ? a.UserId == userid
                                     : a.Delegatee?.SuUserId == userid
                                       && a.IsActive);

        if (acceptor == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้อนุมัติในรายการนี้");
        }

        if (!acceptor.IsCurrent)
        {
            return TypedResults.BadRequest("ยังไม่ถึงลำดับการอนุมัติของผู้ใช้งานนี้");
        }

        if (vendor.Status != ContractDraftVendorStatus.Pending)
        {
            return TypedResults.BadRequest("สถานะร่างสัญญาไม่อยู่ระหว่างรออนุมัติ");
        }

        vendor.SetApproved(
            acceptor.Id,
            req.Remark,
            acceptor.DelegateeId);

        // Only set parent draft approved when all vendors and all their approvers finished
        if (contractDraft.Vendors.All(v => v.IsApproved))
        {
            contractDraft.SetApproved();
        }

        if (vendor.Status == ContractDraftVendorStatus.Pending || vendor.Status == ContractDraftVendorStatus.Approved)
        {
            var processingOptions = new DocumentProcessingOptions(
                contractDraft.Procurement.SupplyMethodCode,
                contractDraft.Procurement.SupplyMethodSpecialTypeCode,
                true,
                false,
                true);

            await this.UpdateDocumentAsync(vendor, processingOptions, UserId.From(req.UserId), ct);
        }

        if (vendor.Status == ContractDraftVendorStatus.Approved)
        {
            var programName = contractDraft.Procurement.Type == ProcurementType.Rent
                ? ProgramConstant.BranchSpaceRent.Name
                : ProgramConstant.ContractDraft.Name;

            _ = SendNotificationAsync(
                contractDraft,
                UserId.From(contractDraft.AuditInfo.CreatedBy),
                NotificationConstant.InformCommittee.Title,
                string.Format(
                    NotificationConstant.InformCommittee.Message,
                    programName,
                    contractDraft.Procurement.ProcurementNumber));
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static async Task SendNotificationAsync(CaContractDraft contractDraft, UserId userId, string title, string message)
    {
        var notificationProgram = NotificationProgram.ContractAgreement;

        var programUrl = contractDraft.Procurement.Type == ProcurementType.Rent
            ? ProgramConstant.BranchSpaceRent.Url
            : ProgramConstant.Procurement.Url;

        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  notificationProgram)
              .SetReferenceId(contractDraft.Id.Value)
              .SetLinkUrl(string.Format(programUrl, contractDraft.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}