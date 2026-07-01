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

public class RejectContractDraftRequest
{
    public Guid ProcurementId { get; set; }

    public Guid ContractDraftId { get; set; }

    public Guid VendorId { get; set; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; set; }

    public string Remark { get; init; }
}

public class RejectContractDraftEndpoint
    : ContractDraftEndpointBase<RejectContractDraftRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectContractDraftEndpoint(
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
        this.Put("procurement/{ProcurementId:guid}/contract-draft/{ContractDraftId:guid}/vendor/{VendorId:guid}/reject");
        this.Options(b =>
            b.WithTags(nameof(ContractDraft))
             .WithName("RejectContractDraft")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectContractDraftRequest req, CancellationToken ct)
    {
        var contractDraft =
            await this.dbContext
                      .CaContractDrafts
                      .Include(c => c.Vendors)
                      .ThenInclude(v => v.Acceptors)
                      .FirstOrDefaultAsync(
                          c =>
                              c.Id == ContractDraftId.From(req.ContractDraftId) &&
                              c.ProcurementId == ProcurementId.From(req.ProcurementId),
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

        var userId = UserId.From(req.UserId);
        var acceptor = vendor.Acceptors
                             .Map(DelegatorExtensions.DelegatorToAcceptor)
                             .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                 ? a.UserId == userId
                                 : a.Delegatee?.SuUserId == userId);

        if (acceptor is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้อนุมัติในรายการนี้");
        }

        var currentAcceptorUser =
            vendor.Acceptors
                  .FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(remark: req.Remark);

        vendor.SetRejected(
            req.Remark);

        _ = SendNotificationAsync(contractDraft);

        this.dbContext.CaContractDrafts.Update(contractDraft);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static async Task SendNotificationAsync(CaContractDraft contractDraft)
    {
        var notificationProgram = NotificationProgram.ContractAgreement;

        var programUrl = contractDraft.Procurement.Type == ProcurementType.Rent
            ? ProgramConstant.BranchSpaceRent.Url
            : ProgramConstant.PreProcurementAppointment.Url;

        await Notification
              .Crate(
                  UserId.From(contractDraft.AuditInfo.CreatedBy),
                  NotificationConstant.ReturnToCreator.Title,
                  string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.ContractDraft.Name, contractDraft.Procurement.ProcurementNumber),
                  notificationProgram)
              .SetReferenceId(contractDraft.Id.Value)
              .SetLinkUrl(string.Format(programUrl, contractDraft.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}