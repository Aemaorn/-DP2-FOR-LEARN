namespace GHB.DP2.Application.Features.ContractManagement.ContractTermination;

using FluentValidation;
using GHB.DP2.Domain.Common;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Application.Features.ContractManagement.ContractTermination.Abstract;

public record UpdateAcceptorDutiesStatusRequest(
    Guid ContractId,
    Guid Id,
    Guid AcceptorId,
    bool IsUnableToPerformDuties,
    string? Remark);

public class UpdateContractTerminationValidator : Validator<UpdateAcceptorDutiesStatusRequest>
{
    public UpdateContractTerminationValidator()
    {
        this.RuleFor(x => x.ContractId)
            .NotNull()
            .NotEmpty()
            .WithMessage("ต้องระบุการบอกเลิกสัญญา");

        this.RuleFor(x => x.Id)
            .NotNull()
            .NotEmpty()
            .WithMessage("ต้องระบุการบอกเลิกสัญญา");

        this.RuleFor(x => x.AcceptorId)
            .NotNull()
            .NotEmpty()
            .WithMessage("กรุณาระบุบุคคล/คณะกรรมการตรวจรับพัสดุ");

        this.RuleFor(x => x.IsUnableToPerformDuties)
            .NotNull()
            .WithMessage("กรุณาระบุสถานะการไม่สามารถปฏิบัติหน้าที่ได้");
    }
}

public class UpdateAcceptorDutiesStatusEndpoint : ContractTerminationEndpoint<UpdateAcceptorDutiesStatusRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateAcceptorDutiesStatusEndpoint(
        Dp2DbContext dbContext,
        ILogger<UpdateAcceptorDutiesStatusEndpoint> logger)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractTermination")
             .WithName("UpdateAcceptorDutiesStatus")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<UpdateAcceptorDutiesStatusRequest>("application/json"));
        this.Put("contract/{ContractId:guid}/contract-termination/{Id:guid}/acceptor/{AcceptorId:guid}/duties-status");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateAcceptorDutiesStatusRequest req, CancellationToken ct)
    {
        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractId), ct);

        var termination = entity.CmContractTerminations.FirstOrDefault(s => s.Id == CmContractTerminationId.From(req.Id));

        if (termination == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการยกเลิกสัญญา");
        }

        // Update acceptor duties status
        var acceptor = termination.Acceptors.FirstOrDefault(x => x.Id == AcceptorId.From(req.AcceptorId));

        if (acceptor == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้รับผิดชอบ");
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
