namespace GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn.Abstract;
using GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn.Dto;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateContractGuaranteeReturnRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ContractDraftVendorId,
    DateTimeOffset GuaranteeReturnDate,
    string? ContractDescription,
    string? ProofOfPaymentDescription,
    string? GuranteeDescription,
    decimal ReturnAmount,
    bool IsDeducted,
    decimal? DeductedAmount,
    decimal NetReturnAmount,
    string? AdditionalComment,
    CmContractGuaranteeReturnStatus Status,
    IEnumerable<AcceptorRequest>? Acceptors,
    IEnumerable<AssigneeRequest>? Assignees,
    IEnumerable<ConditionRequest>? Conditions,
    IEnumerable<RequiredDocumentRequest>? RequiredDocuments,
    DateTimeOffset? DocumentDate = null);

public class CreateContractGuaranteeReturnValidator : Validator<CreateContractGuaranteeReturnRequest>
{
    public CreateContractGuaranteeReturnValidator()
    {
        this.RuleFor(x => x.ContractDraftVendorId)
            .NotEmpty()
            .WithMessage("กรุณาระบุรหัสสัญญา");
        this.RuleFor(x => x.GuaranteeReturnDate)
            .NotEmpty()
            .WithMessage("กรุณาระบุวันที่คืนหลักประกัน");
        this.RuleFor(x => x.ReturnAmount)
            .GreaterThan(0)
            .WithMessage("จำนวนเงินคืนหลักประกันต้องมากกว่า 0");
        this.RuleFor(x => x.NetReturnAmount)
            .GreaterThan(0)
            .WithMessage("จำนวนเงินสุทธิคืนหลักประกันต้องมากกว่า 0");
        this.RuleFor(x => x.IsDeducted)
            .NotNull()
            .WithMessage("กรุณาระบุว่ามีการหักเงินคืนหลักประกันหรือไม่");
        this.RuleFor(x => x.DeductedAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.IsDeducted)
            .WithMessage("จำนวนเงินหักคืนหลักประกันต้องมากกว่าหรือเท่ากับ 0");

        this.When(x => x.Status == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval, () =>
        {
            this.RuleFor(x => x.Acceptors)
                .NotNull().WithMessage("ต้องระบุผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องระบุผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน");
            this.RuleFor(x => x.Assignees)
                .NotNull().WithMessage("ต้องระบุผู้รับผิดชอบอย่างน้อย 1 คน")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องระบุผู้รับผิดชอบอย่างน้อย 1 คน");
        });
    }
}

public class CreateContractGuaranteeReturnEndpoint : ContractGuaranteeReturnEndpoint<CreateContractGuaranteeReturnRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreateContractGuaranteeReturnEndpoint(
        ILogger<CreateContractGuaranteeReturnEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("contract/{ContractDraftVendorId:guid}/contract-guarantee-return");
        this.Description(b => b
                              .WithTags("ContractManagement/ContractGuaranteeReturn")
                              .WithName("CreateContractGuaranteeReturn")
                              .AllowAnonymous()
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(CreateContractGuaranteeReturnRequest req, CancellationToken ct)
    {
        var contractDraftVendor = await this.dbContext.CaContractDraftVendors
                                            .Include(caContractDraftVendor => caContractDraftVendor.ContractDraft)
                                            .ThenInclude(caContractDraft => caContractDraft.Procurement)
                                            .SingleOrDefaultAsync(d => d.Id == ContractDraftVendorId.From(req.ContractDraftVendorId), ct);

        if (contractDraftVendor is null)
        {
            this.ThrowError($"ไม่พบสัญญาที่มีรหัส {req.ContractDraftVendorId}", StatusCodes.Status404NotFound);
        }

        var entity = CmContractGuaranteeReturn.Create(contractDraftVendor.Id)
                                              .SetValues(
                                                  req.GuaranteeReturnDate,
                                                  req.ReturnAmount,
                                                  req.IsDeducted,
                                                  req.DeductedAmount,
                                                  req.NetReturnAmount,
                                                  req.AdditionalComment)
                                              .SetStatus(req.Status)
                                              .SetDescriptions(
                                                  req.ContractDescription,
                                                  req.ProofOfPaymentDescription,
                                                  req.GuranteeDescription);

        if (req.Acceptors != null)
        {
            await this.UpsertAcceptors(entity, [.. req.Acceptors], req.Status, ct, UserId.From(req.UserId));
        }

        if (req.Assignees != null)
        {
            await this.UpsertAssignee(entity, [.. req.Assignees], ct, UserId.From(req.UserId));
        }

        if (req.Conditions != null)
        {
            this.UpsertConditions(entity, req.Conditions);
        }

        if (req.RequiredDocuments != null)
        {
            this.UpsertRequiredDocument(entity, req.RequiredDocuments);
        }

        if (req.DocumentDate is not null)
        {
            entity.SetDocumentDate(req.DocumentDate);
        }

        await this.SetDefaultDocumentTemplate(entity, contractDraftVendor.ContractDraft.Procurement.SupplyMethodCode, ct);
        this.dbContext.CmContractGuaranteeReturns.Add(entity);
        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Created(string.Empty, entity.Id.Value);
    }
}