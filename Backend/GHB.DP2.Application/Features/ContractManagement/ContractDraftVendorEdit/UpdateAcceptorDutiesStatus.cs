namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using FluentValidation;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record UpdateContractDraftVendorEditAcceptorDutiesStatusRequest(
    Guid Id,
    Guid AcceptorId,
    bool IsUnableToPerformDuties,
    string? Remark);

public class UpdateContractDraftVendorEditAcceptorDutiesStatusValidator
    : Validator<UpdateContractDraftVendorEditAcceptorDutiesStatusRequest>
{
    public UpdateContractDraftVendorEditAcceptorDutiesStatusValidator()
    {
        this.RuleFor(x => x.Id)
            .NotNull()
            .NotEmpty()
            .WithMessage("กรุณาระบุแก้ไขร่างสัญญา");

        this.RuleFor(x => x.AcceptorId)
            .NotNull()
            .NotEmpty()
            .WithMessage("กรุณาระบุบุคคล/คณะกรรมการตรวจรับพัสดุ");

        this.RuleFor(x => x.IsUnableToPerformDuties)
            .NotNull()
            .WithMessage("กรุณาระบุสถานะการไม่สามารถปฏิบัติหน้าที่ได้");
    }
}

public class UpdateContractDraftVendorEditAcceptorDutiesStatusEndpoint
    : ContractDraftVendorEditEndpoint<
        UpdateContractDraftVendorEditAcceptorDutiesStatusRequest,
        Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateContractDraftVendorEditAcceptorDutiesStatusEndpoint(
        ILogger<UpdateContractDraftVendorEditAcceptorDutiesStatusEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractDraftVendorEdit")
             .WithName("UpdateContractDraftVendorEditAcceptorDutiesStatus")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<UpdateContractDraftVendorEditAcceptorDutiesStatusRequest>("application/json"));

        this.Put("contract/contract-draft-vendor-edit/{Id:guid}/acceptor/{AcceptorId:guid}/duties-status");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>>
        HandleRequestAsync(UpdateContractDraftVendorEditAcceptorDutiesStatusRequest req, CancellationToken ct)
    {
        var entity = await this.GetEditByIdAsync(ContractDraftVendorEditId.From(req.Id), ct);

        if (entity.Status is not (ContractDraftVendorEditStatus.WaitingCommitteeApproval
            or ContractDraftVendorEditStatus.Draft
            or ContractDraftVendorEditStatus.Editing
            or ContractDraftVendorEditStatus.Rejected))
        {
            return TypedResults.BadRequest("ไม่สามารถแก้ไขสถานะการปฏิบัติงานในสถานะนี้ได้");
        }

        var acceptor = entity.Acceptors
            .FirstOrDefault(a =>
                a.Id == AcceptorId.From(req.AcceptorId) &&
                a.Type == AcceptorType.AcceptanceCommittee);

        if (acceptor is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลบุคคล/คณะกรรมการตรวจรับพัสดุ");
        }

        acceptor.SetIsUnableToPerformDuties(req.IsUnableToPerformDuties);

        if (req.IsUnableToPerformDuties)
        {
            acceptor.UnableToPerformDuties(req.Remark);
        }
        else
        {
            acceptor.Pending();
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
