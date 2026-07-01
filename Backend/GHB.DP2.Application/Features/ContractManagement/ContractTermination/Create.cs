namespace GHB.DP2.Application.Features.ContractManagement.ContractTermination;

using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractManagement.ContractTermination.Abstract;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateContractTerminationRequest(
    Guid ContractDraftVendorId,
    string? TerminateType,
    string? TerminateReasonOther,
    DateTimeOffset? TerminationDate,
    string? TerminateReason,
    string? TerminateReasonDetail,
    CmContractTerminationStatus Status,
    IEnumerable<AssigneeRequest>? Assignees);

public class CreateContractTerminationValidator : Validator<CreateContractTerminationRequest>
{
    public CreateContractTerminationValidator()
    {
        this.RuleFor(x => x.ContractDraftVendorId)
            .NotEmpty()
            .WithMessage("กรุณาระบุรหัสสัญญา");
    }
}

public class CreateContractTerminationEndpoint : ContractTerminationEndpoint<CreateContractTerminationRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreateContractTerminationEndpoint(ILogger<CreateContractTerminationEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("contract/{ContractDraftVendorId:guid}/contract-termination");
        this.Description(b => b
                              .WithTags("ContractManagement/ContractTermination")
                              .WithName("CreateContractTermination")
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(CreateContractTerminationRequest req, CancellationToken ct)
    {
        var contractDraftVendor = await this.dbContext.CaContractDraftVendors
                                            .Include(cdv => cdv.ContractDraft)
                                            .ThenInclude(c => c.Procurement)
                                            .ThenInclude(p => p.SupplyMethod)
                                            .AsSplitQuery()
                                            .SingleOrDefaultAsync(d => d.Id == ContractDraftVendorId.From(req.ContractDraftVendorId), ct);

        if (contractDraftVendor is null)
        {
            this.ThrowError($"ไม่พบสัญญาที่มีรหัส {req.ContractDraftVendorId}", StatusCodes.Status404NotFound);
        }

        var entity = CmContractTermination
                     .Create(contractDraftVendor.Id)
                     .SetValues(
                         !string.IsNullOrWhiteSpace(req.TerminateType) ? ParameterCode.From(req.TerminateType) : null,
                         req.TerminateReason,
                         req.TerminationDate)
                     .SetTerminateReasonOther(req.TerminateReasonOther)
                     .SetStatus(req.Status);

        await this.AddCommitteeAcceptors(contractDraftVendor.ContractDraft.ProcurementId, entity, ct);

        if (req.Assignees != null)
        {
            await this.UpsertAssignee(entity, req.Assignees, ct);
        }

        this.dbContext.CmContractTerminations.Add(entity);
        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        var reloadContactDraftVendor = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractDraftVendorId), ct);

        var termination = reloadContactDraftVendor.CmContractTerminations
                                                  .FirstOrDefault(s => s.Id == entity.Id);

        if (termination is null)
        {
            this.ThrowError($"ไม่พบข้อมูลการบอกเลิกสัญญาของสัญญา {reloadContactDraftVendor.ContractNumber}", StatusCodes.Status404NotFound);
        }

        await this.CreateDocumentAsync(reloadContactDraftVendor, termination, ct);
        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Created(string.Empty, entity.Id.Value);
    }
}