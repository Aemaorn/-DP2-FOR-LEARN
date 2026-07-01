namespace GHB.DP2.Application.Features.Procurement.MedianPrice;

using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Abstract;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Dto;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateMedianPriceRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    string Object,
    string Reason,
    string? Telephone,
    string? SpecialDescription,
    string? JobDescription,
    string PriceReasonablenessInfo,
    string MedianPriceDocumentTemplateCode,
    DateTimeOffset? DocumentDate,
    MedianPriceStatus Status,
    CreateBudgetAllocations BudgetAllocations,
    CreateStaff? Staff,
    MedianPriceExpenseDescriptionInfo? ExpenseDescription,
    MedianPriceAcceptorInfo[]? Acceptors,
    MedianPriceAssigneeInfo[]? Assignees,
    bool HasMd)
{
    public PpMedianPrice MapToEntity(Procurement procurement)
    {
        var medianPrice =
            PpMedianPrice.Create(procurement)
                         .SetObject(this.Object)
                         .SetReason(this.Reason)
                         .SetSpecialDescription(this.SpecialDescription ?? string.Empty)
                         .SetJobDescription(this.JobDescription)
                         .SetPriceReasonablenessInfo(this.PriceReasonablenessInfo)
                         .SetTelephone(this.Telephone);

        medianPrice.AddBudgetAllocations(this.BudgetAllocations.MapToEntity());

        if (this.Staff is not null)
        {
            medianPrice.AddStaff(this.Staff.MapToEntity());
        }

        if (this.ExpenseDescription is not null)
        {
            medianPrice.AddExpenseDescription(
                this.ExpenseDescription.MapToEntity());
        }

        if (this.DocumentDate is not null)
        {
            medianPrice.SetDocumentDate(this.DocumentDate.Value);
        }

        return medianPrice;
    }

    public class Validator : Validator<CreateMedianPriceRequest>
    {
        public Validator()
        {
            this.RuleFor(x => x.ProcurementId)
                .NotEmpty()
                .WithMessage("ต้องมีข้อมูลจัดซื้อจัดจ้างเบื้องต้น");

            this.RuleFor(x => x.Object)
                .NotEmpty()
                .WithMessage("กรุณาระบุวัตถุประสงค์การขอซื้อ");

            this.RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("กรุณาระบุเหตุผลและความจำเป็นที่จะซื้อหรือจ้างหรือเช่า");

            this.RuleFor(x => x.PriceReasonablenessInfo)
                .NotEmpty()
                .WithMessage("กรุณาระบุข้อมูลประกอบการพิจารณาด้านความเหมาะสมของราคา");

            this.RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("สถานะราคากลางไม่ถูกต้อง")
                .Must(s => s is MedianPriceStatus.Draft or MedianPriceStatus.WaitingCommitteeApproval)
                .WithMessage("สถานะราคากลางต้องเป็น 'ร่าง' หรือ 'รอการอนุมัติจากคณะกรรมการ'");

            this.RuleFor(x => x.BudgetAllocations)
                .NotNull()
                .WithMessage("กรุณาระบุข้อมูลตารางแสดงวงเงินงบประมาณที่ได้รับจัดสรรและรายละเอียดค่าใช้จ่าย")
                .SetValidator(new CreateBudgetAllocations.Validator());

            this.RuleFor(x => x.Staff)
                .SetValidator(new CreateStaff.Validator()!)
                .When(x => x.Staff is not null)
                .WithMessage("กรุณาระบุข้อมูลรายละเอียดบุคลากร");

            this.RuleFor(x => x.Acceptors)
                .NotNull()
                .When(x => x.Status is MedianPriceStatus.WaitingCommitteeApproval)
                .WithMessage("ต้องมีบุคคล/คณะกรรมการกำหนดราคากลางอย่างน้อย 1 คน");

            this.RuleFor(x => x.Acceptors)
                .Must(x => x != null && x.Any(m => m.AcceptorType == AcceptorType.MedianPriceCommittee))
                .When(w => w.Status is MedianPriceStatus.WaitingCommitteeApproval && !w.HasMd)
                .WithMessage("ต้องมีผู้มีอำนาจเห็นชอบ/อนุมัติอย่างน้อย 1 คน");

            this.RuleFor(x => x.Acceptors)
                .Must(x => x != null && x.Any(m => m.AcceptorType == AcceptorType.DepartmentDirectorAgree))
                .When(w => w.Status is MedianPriceStatus.WaitingCommitteeApproval && w.HasMd)
                .WithMessage("ต้องมีสายงานเห็นชอบอย่างน้อย 1 คน");

            this.RuleFor(x => x.Assignees)
                .Must(x => x is not null && x.Any(a => a.AssigneeType == AssigneeType.Director))
                .When(x => x.Status is MedianPriceStatus.WaitingCommitteeApproval && x.HasMd)
                .WithMessage("ต้องมีผู้มอบหมายประเภทผู้อำนวยการอย่างน้อย 1 คน");
        }
    }
}

public record CreateBudgetAllocations(
    decimal Budget,
    decimal ReferenceMedianPrice,
    CreateBudgetAllocationsDetail[] Details)
{
    public PpMedianPriceBudgetAllocations MapToEntity()
    {
        var budgetAllocations = PpMedianPriceBudgetAllocations.Create(
            this.Budget,
            this.ReferenceMedianPrice);

        foreach (var detail in this.Details)
        {
            budgetAllocations.AddDetail(detail.MapToEntity());
        }

        return budgetAllocations;
    }

    public class Validator : Validator<CreateBudgetAllocations>
    {
        public Validator()
        {
            this.RuleFor(x => x.Budget)
                .GreaterThan(0)
                .WithMessage("วงเงินงบประมาณ (บาท) จะต้องมากกว่า 0");

            this.RuleFor(x => x.ReferenceMedianPrice)
                .GreaterThan(0)
                .WithMessage("ราคากลางอ้างอิง จะต้องมากกว่า 0");

            this.RuleFor(x => x.Details)
                .NotEmpty()
                .WithMessage("ต้องมีข้อมูลแหล่งที่มาอย่างน้อย 1 รายการ");
        }
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CreateBudgetAllocationsWithDetail), nameof(BudgetAllocationsDetailType.With))]
[JsonDerivedType(typeof(CreateBudgetAllocationsWithoutDetail), nameof(BudgetAllocationsDetailType.Without))]
public abstract record CreateBudgetAllocationsDetail(
    int Sequence,
    string Source)
{
    public abstract PpMedianPriceBudgetAllocationsDetail MapToEntity();
}

public record CreateBudgetAllocationsWithDetail(
    int Sequence,
    string Source,
    decimal ReferenceBudge) : CreateBudgetAllocationsDetail(Sequence, Source)
{
    public override PpMedianPriceBudgetAllocationsDetail MapToEntity()
    {
        return PpMedianPriceBudgetAllocationsWithDetail.CreateWithDetail(
            this.Sequence,
            this.Source,
            this.ReferenceBudge);
    }

    public class Validator : Validator<CreateBudgetAllocationsWithDetail>
    {
        public Validator()
        {
            this.RuleFor(x => x.Sequence)
                .GreaterThan(0)
                .WithMessage("ลำดับต้องมากกว่า 0");

            this.RuleFor(x => x.Source)
                .NotEmpty()
                .WithMessage("กรุณาระบุแหล่งที่มา");

            this.RuleFor(x => x.ReferenceBudge)
                .GreaterThan(0)
                .WithMessage("ราคาอ้างอิงต้องมากกว่า 0 บาท");
        }
    }
}

public record CreateBudgetAllocationsWithoutDetail(
    int Sequence,
    string Source) : CreateBudgetAllocationsDetail(Sequence, Source)
{
    public override PpMedianPriceBudgetAllocationsDetail MapToEntity()
    {
        return PpMedianPriceBudgetAllocationsWithoutDetail.CreateWithoutDetail(
            this.Sequence,
            this.Source);
    }

    public class Validator : Validator<CreateBudgetAllocationsWithoutDetail>
    {
        public Validator()
        {
            this.RuleFor(x => x.Sequence)
                .GreaterThan(0)
                .WithMessage("ลำดับต้องมากกว่า 0");

            this.RuleFor(x => x.Source)
                .NotEmpty()
                .WithMessage("กรุณาระบุแหล่งที่มา");
        }
    }
}

public record CreateStaff(
    decimal PersonnelCompensation,
    int PersonnelCount,
    CreateStaffDetail[] Details)
{
    public PpMedianPriceStaff MapToEntity()
    {
        var staff = PpMedianPriceStaff.Create(
            this.PersonnelCompensation,
            this.PersonnelCount);

        foreach (var detail in this.Details)
        {
            staff.AddDetail(detail.MapToEntity());
        }

        return staff;
    }

    public class Validator : Validator<CreateStaff>
    {
        public Validator()
        {
            this.RuleFor(x => x.PersonnelCompensation)
                .GreaterThan(0)
                .WithMessage("ค่าตอบแทนบุคลากร (บาท) จะต้องมากกว่า 0");

            this.RuleFor(x => x.PersonnelCount)
                .NotNull()
                .WithMessage("กรุณาระบุจำนวนที่ปรึกษา (คน)");

            this.RuleFor(x => x.Details)
                .NotEmpty()
                .WithMessage("ต้องมีข้อมูลรายการบุคลากรมาอย่างน้อย 1 รายการ");
        }
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CreateStaffPersonnelDetail), nameof(MedianPriceStaffType.Personal))]
[JsonDerivedType(typeof(CreateStaffConsultantTypeDetail), nameof(MedianPriceStaffType.ConsultantTypes))]
[JsonDerivedType(typeof(CreateStaffConsultantQualificationDetail), nameof(MedianPriceStaffType.ConsultantQualifications))]
public abstract record CreateStaffDetail(
    int Sequence,
    string Description)
{
    public abstract PpMedianPriceStaffDetail MapToEntity();
}

public record CreateStaffPersonnelDetail(
    int Sequence,
    string Description,
    int PersonnelCount) : CreateStaffDetail(Sequence, Description)
{
    public override PpMedianPriceStaffDetail MapToEntity()
    {
        return PpMedianPriceStaffPersonal.Create(
            this.Sequence,
            this.Description,
            this.PersonnelCount);
    }
}

public record CreateStaffConsultantTypeDetail(
    int Sequence,
    string Description) : CreateStaffDetail(Sequence, Description)
{
    public override PpMedianPriceStaffDetail MapToEntity()
    {
        return PpMedianPriceStaffConsultantTypes.Create(
            this.Sequence,
            this.Description);
    }
}

public record CreateStaffConsultantQualificationDetail(
    int Sequence,
    string Description) : CreateStaffDetail(Sequence, Description)
{
    public override PpMedianPriceStaffDetail MapToEntity()
    {
        return PpMedianPriceStaffConsultantQualifications.Create(
            this.Sequence,
            this.Description);
    }
}

public class CreateMedianPriceEndpoint : MedianPriceEndpointBase<CreateMedianPriceRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreateMedianPriceEndpoint(
        ILogger<CreateMedianPriceEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/median-price");
        this.Description(b => b
                              .WithTags(nameof(MedianPrice))
                              .WithName("CreateMedianPrice")
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(
        CreateMedianPriceRequest req,
        CancellationToken ct)
    {
        var procurementExisting =
            await this.dbContext.Procurements
                      .SingleOrDefaultAsync(
                          p => p.Id == ProcurementId.From(req.ProcurementId),
                          ct);

        if (procurementExisting is null)
        {
            this.ThrowError(
                $"Procurement with ID {req.ProcurementId} not found.",
                StatusCodes.Status404NotFound);
        }

        var medianPrice = req.MapToEntity(procurementExisting);

        if (req.Acceptors is not null)
        {
            await this.UpsertAcceptors(medianPrice, req.Acceptors, procurementExisting.DepartmentId, UserId.From(req.UserId));
        }

        if (req.Assignees is not null)
        {
            await this.UpsertAssignee(medianPrice, req.Assignees, cancellationToken: ct);
        }

        await this.AddDocumentTemplate(medianPrice, req.MedianPriceDocumentTemplateCode, ct);

        // Update status
        UpdateStatus(medianPrice, req.Status);

        await this.SetDefaultDocumentTemplate(
            medianPrice,
            req.MedianPriceDocumentTemplateCode,
            ct);

        this.dbContext.PpMedianPrices.Add(medianPrice);

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        // Reload entity with includes needed by MapToReplaceDtoAsync
        var medianPriceReloaded = await this.GetMedianPriceById(medianPrice.Id, medianPrice.ProcurementId, ct);
        var lastedDraft = medianPriceReloaded.LastedDraftDocument;

        if (lastedDraft is not null)
        {
            var documentService = this.Resolve<IDocumentService>();
            var replaceDto = await this.MapToReplaceDtoAsync(
                medianPriceReloaded.Procurement,
                medianPriceReloaded,
                ct,
                creatorUserId: null,
                isPreview: false);

            var copiedFileId = await documentService.CopyDocumentTemplateAsync(
                lastedDraft.FileId,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: $"{DocumentTemplateGroups.Mdp}/{medianPriceReloaded.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

            if (copiedFileId.HasValue)
            {
                medianPriceReloaded.AddDocumentHistory(copiedFileId.Value);
                await this.dbContext.SaveChangesAsync(ct);
            }
        }

        return TypedResults.Created(string.Empty, medianPrice.Id.Value);
    }

    private async Task AddDocumentTemplate(
        PpMedianPrice medianPrice,
        string medianPriceDocumentTemplateCode,
        CancellationToken ct)
    {
        var documentTemplate =
            await this.dbContext.SuDocumentTemplates
                      .FirstOrDefaultAsync(dt => dt.Code == medianPriceDocumentTemplateCode, ct);

        if (documentTemplate is null)
        {
            this.ThrowError("ไม่พบเอกสาร Median price Template ที่ระบุ", StatusCodes.Status404NotFound);
        }

        if (documentTemplate.Id == medianPrice.DocumentTemplateId)
        {
            return;
        }

        medianPrice.SetDocumentTemplate(documentTemplate.Id);
    }
}