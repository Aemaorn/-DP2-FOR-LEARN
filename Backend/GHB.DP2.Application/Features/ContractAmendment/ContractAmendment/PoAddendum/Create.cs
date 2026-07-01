namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum.Abstract;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreatePoAddendumRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    CamContractAmendmentId CamContractAmendmentId,
    string ContractNumber,
    string SapNumber,
    string? PoNumber,
    Guid VendorId,
    CamContractAmendmentPoAddendumStatus Status,
    IEnumerable<AcceptorRequest>? Acceptors,
    IEnumerable<AssigneeRequest>? Assignees,
    IEnumerable<PaymentTermRequest>? PaymentTerms);

public class CreatePoAddendumRequestValidator : Validator<CreatePoAddendumRequest>
{
    public CreatePoAddendumRequestValidator()
    {
        this.RuleFor(r => r.CamContractAmendmentId).NotEmpty();
        this.RuleFor(r => r.ContractNumber).NotEmpty();
        this.RuleFor(r => r.PoNumber).NotEmpty();
        this.RuleFor(r => r.SapNumber).NotEmpty();
        this.RuleFor(r => r.VendorId).NotEmpty();
        this.RuleFor(r => r.Status).IsInEnum();
        this.RuleForEach(r => r.Acceptors).SetValidator(new AcceptorRequestValidator());
    }
}

public class CreatePoAddendumEndpoint : PoAddendumAbstractEndpoint<CreatePoAddendumRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreatePoAddendumEndpoint(ILogger<CreatePoAddendumEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("contract-amendment/{CamContractAmendmentId:guid}/po-addendum");
        this.Description(b => b
            .WithTags("ContractAmendment/PoAddendum")
            .WithName("CreatePoAddendum")
            .AllowAnonymous()
            .Produces<Created<Guid>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(CreatePoAddendumRequest req, CancellationToken ct)
    {
        var validator = new CreatePoAddendumRequestValidator();
        var validation = await validator.ValidateAsync(req, ct);
        if (!validation.IsValid)
        {
            var invalidProps = validation.Errors.Select(e => e.PropertyName).Distinct();
            var message = $"ข้อมูลไม่ถูกต้อง: {string.Join(", ", invalidProps)}";
            this.ThrowError(message, StatusCodes.Status400BadRequest);
        }

        var cam = await this.dbContext.Set<CamContractAmendment>()
            .SingleOrDefaultAsync(c => c.Id == req.CamContractAmendmentId, ct);

        if (cam is null)
        {
            this.ThrowError(r => r.CamContractAmendmentId, "ไม่พบข้อมูลการแก้ไขสัญญา", StatusCodes.Status404NotFound);
        }

        var vendor = await this.dbContext.SuVendors.SingleOrDefaultAsync(v => v.Id == SuVendorId.From(req.VendorId), ct);
        if (vendor is null)
        {
            this.ThrowError(r => r.VendorId, "ไม่พบข้อมูลผู้ประกอบการ", StatusCodes.Status404NotFound);
        }

        var entity = CamContractAmendmentPoAddendum.Create(
            req.CamContractAmendmentId,
            req.ContractNumber,
            req.SapNumber,
            req.PoNumber,
            vendor!);

        if (req.PaymentTerms != null)
        {
            this.UpsertPaymentTerm(entity, req.PaymentTerms);
        }

        if (req.Acceptors != null && req.Acceptors.Any())
        {
            await this.UpsertAcceptors(entity, [.. req.Acceptors], req.Status, ct);
        }

        if (req.Assignees != null && req.Assignees.Any())
        {
            await this.UpsertAssignee(entity, [.. req.Assignees], ct);
        }

        cam.SetStatus(CamContractAmendmentStatus.InProgress);

        await this.SetDefaultDocumentTemplate(entity, ct);
        this.dbContext.CamContractAmendments.Update(cam);
        this.dbContext.CamContractAmendmentPoAddendums.Add(entity);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, entity.Id.Value);
    }
}