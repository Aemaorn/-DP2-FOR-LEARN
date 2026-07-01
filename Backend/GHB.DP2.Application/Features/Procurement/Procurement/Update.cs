namespace GHB.DP2.Application.Features.Procurement.Procurement;

using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;

public class UpdateProcurementRequest
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public Guid ProcurementId { get; init; }

    public ProcurementType ProcurementType { get; set; }

    public string DepartmentCode { get; init; }

    public string SupplyMethodCode { get; init; }

    public string? SupplyMethodTypeCode { get; init; }

    public string? SupplyMethodSpecialTypeCode { get; init; }

    public string PlanName { get; set; }

    public decimal? Budget { get; set; }

    public ProcurementStatus Status { get; init; }

    public string? RemarkClosed { get; init; }

    public IEnumerable<UpsertProcurementAttachmentsDto>? Attachments { get; init; }
}

public class UpdateProcurementRequestValidator : Validator<UpdateProcurementRequest>
{
    public UpdateProcurementRequestValidator()
    {
        this.RuleFor(r => r.ProcurementId)
            .NotEmpty()
            .NotNull()
            .WithMessage("กรุณาระบุ ProcurementId");

        this.RuleFor(r => r.Status)
            .IsInEnum()
            .WithMessage("สถานะอยู่นอกเหนือที่มีอยู่")
            .Must(c => c is ProcurementStatus.Draft or ProcurementStatus.InProgress or ProcurementStatus.Cancelled)
            .WithMessage(@$"สถานะจะต้องเป็น ""แบบร่าง"" หรือ ""กำลังดำเนินการ"" หรือ ""ปิดงาน"" เท่านั้น ");

        this.RuleFor(p => p.DepartmentCode)
            .NotEmpty()
            .WithMessage("กรุณาระบุฝ่าย/ภาคเขต");

        this.RuleFor(p => p.SupplyMethodCode)
            .NotEmpty()
            .WithMessage("กรุณาระบุวิธีการจัดหา");

        this.RuleFor(p => p.PlanName)
            .NotEmpty()
            .WithMessage("กรุณาระบุเรื่อง");

        this.When(p => p.ProcurementType == ProcurementType.Procurement, () =>
        {
            this.RuleFor(p => p.Budget)
                .GreaterThan(0)
                .WithMessage("งบประมาณจะต้องมากกว่า 0")
                .NotEmpty()
                .WithMessage("กรุณาระบุงบประมาณ");
        });
    }
}

public class UpdateProcurementEndpoint : EndpointBase<UpdateProcurementRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateProcurementEndpoint(
        ILogger<UpdateProcurementEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement")
             .WithName("UpdateProcurement")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<UpdateProcurementRequest>("application/json"));
        this.Put("procurement/{ProcurementId:guid}");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpdateProcurementRequest req, CancellationToken ct)
    {
        await this.ValidateRequestAsync(req, ct);
        var data = await this.UpdateProcurement(req, ct);

        this.dbContext.Procurements.Update(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task ValidateRequestAsync(UpdateProcurementRequest req, CancellationToken ct)
    {
        var department = await this.dbContext.RawBusinessUnits
                                   .FirstOrDefaultAsync(d => d.Id == BusinessUnitId.From(req.DepartmentCode), ct);

        if (department is null)
        {
            this.ThrowError(
                r => r.DepartmentCode,
                $"ไม่พบหน่วยงานที่มีรหัส {req.DepartmentCode}");
        }

        // Validate supply method parameter
        var supplyMethod = await this.dbContext.SuParameters
                                     .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.SupplyMethodCode), ct);

        if (supplyMethod is null)
        {
            this.ThrowError(
                r => r.SupplyMethodCode,
                $"ไม่พบวิธีการจัดหา",
                StatusCodes.Status404NotFound);
        }

        // Validate optional parameter codes
        if (req.SupplyMethodTypeCode is not null)
        {
            var supplyMethodType = await this.dbContext.SuParameters
                                             .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.SupplyMethodTypeCode), ct);

            if (supplyMethodType is null)
            {
                this.ThrowError(
                    r => r.SupplyMethodTypeCode,
                    $"ไม่พบวิธีการจัดหา",
                    StatusCodes.Status404NotFound);
            }
        }

        if (req.SupplyMethodSpecialTypeCode is not null)
        {
            var supplyMethodSpecialType = await this.dbContext.SuParameters
                                                    .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.SupplyMethodSpecialTypeCode), ct);

            if (supplyMethodSpecialType is null)
            {
                this.ThrowError(
                    r => r.SupplyMethodSpecialTypeCode,
                    $"ไม่พบวิธีการจัดหา",
                    StatusCodes.Status404NotFound);
            }
        }
    }

    private async Task<Procurement> UpdateProcurement(UpdateProcurementRequest req, CancellationToken ct)
    {
        var procurement = await this.dbContext.Procurements
                                    .Include(p => p.Attachments)
                                    .ThenInclude(a => a.ProcurementAttachmentInfos)
                                    .SingleOrDefaultAsync(w => w.Id == ProcurementId.From(req.ProcurementId), ct);

        if (procurement is null)
        {
            this.ThrowError("ไม่พบข้อมูล", StatusCodes.Status404NotFound);
        }

        if (req.ProcurementType == ProcurementType.Procurement)
        {
            var processType = SectionProcessType.TOR;
            if (procurement.SupplyMethodCode.Value == SupplyMethodConstant.Eighty && procurement.IsCommercialMaterial)
            {
                processType = SectionProcessType.TORCommercialParcel;
            }

            var hasMd = await this.dbContext.SuSections
                            .Include(s => s.Approvers)
                            .AnyAsync(
                                s =>
                                    s.Approvers.Any(a =>
                                        a.Budget >= req.Budget &&
                                        a.InRefCode == InRefCodeConstant.Bp002 &&
                                        a.ProcessType == processType) &&
                                    s.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode) &&
                                    (s.SupplyMethodSpecialTypeCode == (req.SupplyMethodSpecialTypeCode != null
                                                                        ? ParameterCode.From(req.SupplyMethodSpecialTypeCode)
                                                                        : null) ||
                                                                       s.SupplyMethodSpecialTypeCode == null),
                                ct);

            procurement.SetHasMd(hasMd)
                       .SetBudget(req.Budget ?? 0);
        }

        procurement.SetSupplyMethod(
                       ParameterCode.From(req.SupplyMethodCode),
                       req.SupplyMethodTypeCode.IsNullOrEmpty() ? null : ParameterCode.From(req.SupplyMethodTypeCode!),
                       req.SupplyMethodSpecialTypeCode.IsNullOrEmpty() ? null : ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
                   .SetDepartmentId(BusinessUnitId.From(req.DepartmentCode))
                   .SetName(req.PlanName);

        if (req.Status == ProcurementStatus.Cancelled)
        {
            procurement.SetClosed(req.RemarkClosed);
            this.AddClosedAttachments(procurement, req.Attachments);
        }
        else if (procurement.Status == ProcurementStatus.Cancelled)
        {
            procurement.SetCancelClosed();
        }
        else
        {
            procurement.SetStatus(req.Status);
        }

        return procurement;
    }

    private void AddClosedAttachments(Procurement procurement, IEnumerable<UpsertProcurementAttachmentsDto>? attachments)
    {
        if (attachments is null)
        {
            return;
        }

        var attachmentArray = attachments.ToArray();

        if (attachmentArray.Length == 0)
        {
            return;
        }

        var existingFileIds = procurement.Attachments
                                         .SelectMany(a => a.ProcurementAttachmentInfos.Select(i => i.FileId))
                                         .ToHashSet();
        var groupSequence = procurement.Attachments.Count == 0 ? 0 : procurement.Attachments.Max(a => a.Sequence);

        foreach (var dto in attachmentArray)
        {
            var newFiles = dto.FileAttachments
                              .Where(f => !existingFileIds.Contains(FileId.From(f.FileId)))
                              .ToArray();

            if (newFiles.Length == 0)
            {
                continue;
            }

            groupSequence++;
            var attachment = ProcurementAttachment.Create(
                procurement.Id,
                groupSequence,
                ParameterCode.From(dto.DocumentTypeCode),
                dto.Remark);

            procurement.AddAttachment(attachment);
            this.dbContext.ProcurementAttachments.Add(attachment);

            var fileSequence = 0;

            foreach (var file in newFiles)
            {
                fileSequence++;
                var info = ProcurementAttachmentInfo.Create(
                    attachment.Id,
                    fileSequence,
                    FileId.From(file.FileId),
                    file.FileName,
                    file.IsPublic);

                attachment.AddAttachmentInfos(info);
                this.dbContext.ProcurementAttachmentInfos.Add(info);
            }
        }
    }
}