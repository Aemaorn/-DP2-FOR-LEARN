namespace GHB.DP2.Application.Features.Procurement.Appoint;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Appoint.Abstract;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateAppointRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    AppointDto Appoint,
    IEnumerable<AppointTorDraftCommitteeDto> TorDraftCommittees,
    IEnumerable<DutiesDto> TorDraftCommitteeDuties,
    IEnumerable<AppointMedianPriceCommitteeDto> MedianPriceCommittees,
    IEnumerable<DutiesDto> MedianPriceCommitteeDuties,
    IEnumerable<AppointAcceptorDto> Acceptors);

public record AppointDto(
    Guid ProcurementId,
    int ProcurementBudgetYear,
    DateTimeOffset MemorandumDate,
    string? MemorandumNumber,
    string? Telephone,
    string? Reason,
    AppointStatus Status);

public record AppointTorDraftCommitteeDto(
    Guid UserId,
    string CommitteePositionsCode,
    int Sequence);

public record AppointMedianPriceCommitteeDto(
    Guid UserId,
    string CommitteePositionsCode,
    int Sequence);

public record DutiesDto(string Description, int Sequence);

public class CreateAppointRequestValidator : Validator<CreateAppointRequest>
{
    public CreateAppointRequestValidator()
    {
        this.RuleFor(x => x.Appoint)
            .NotNull()
            .WithMessage("กรุณาระบุข้อมูล");

        // Add validation for nested AppointDto properties
        this.RuleFor(x => x.Appoint.ProcurementId)
            .NotEmpty()
            .WithMessage("ไม่พบรหัสการจัดซื้อจัดจ้าง");

        this.RuleFor(x => x.Appoint.ProcurementBudgetYear)
            .GreaterThan(0)
            .WithMessage("ปีงบประมาณไม่สามารถเป็นค่าศูนย์ได้");

        this.RuleFor(x => x.Appoint.MemorandumDate)
            .NotEmpty()
            .WithMessage("กรุณาระบุวันที่เอกสารบันทึกข้อความแต่งตั้ง");
    }
}

public class CreateAppointEndpoint : AppointEndpointBase<CreateAppointRequest, Results<Ok<PpAppointId>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateAppointEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<CreateAppointEndpoint> logger)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/Appoint"));
        this.Post("appointments");
    }

    protected override async ValueTask<Results<Ok<PpAppointId>, NotFound<string>>> HandleRequestAsync(CreateAppointRequest req, CancellationToken ct)
    {
        var procurement = await this.dbContext.Procurements
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(x => x.Id == ProcurementId.From(req.Appoint.ProcurementId), ct);

        if (procurement is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูล");
        }

        var appoint = PpAppoint.Create(
            procurement,
            req.Appoint.MemorandumDate,
            req.Appoint.MemorandumNumber,
            req.Appoint.Telephone,
            req.Appoint.Reason);

        appoint.SetDocumentDate(req.Appoint.MemorandumDate);

        appoint.SetStatus(req.Appoint.Status);

        this.dbContext.PpAppoints.Add(appoint);

        foreach (var dto in req.TorDraftCommittees)
        {
            var appointTorDraftCommittee = await this.CreateTorDraftCommittee(
                appoint.Id,
                dto.UserId,
                dto.CommitteePositionsCode,
                dto.Sequence,
                ct);

            appoint.AddPpAppointTorDraftCommittee(appointTorDraftCommittee);
        }

        foreach (var dto in req.TorDraftCommitteeDuties)
        {
            var duties = CreateTorDraftCommitteeDuties(
                appoint.Id,
                dto.Description,
                dto.Sequence);

            appoint.AddPpAppointTorDraftCommitteeDuties(duties);
        }

        foreach (var dto in req.MedianPriceCommittees)
        {
            var appointMedianPriceCommittee = await this.CreateMedianPriceCommittee(
                appoint.Id,
                dto.UserId,
                dto.CommitteePositionsCode,
                dto.Sequence,
                ct);

            appoint.AddPpAppointMedianPriceCommittee(appointMedianPriceCommittee);
        }

        foreach (var dto in req.MedianPriceCommitteeDuties)
        {
            var duties = CreateMedianPriceCommitteeDuties(
                appoint.Id,
                dto.Description,
                dto.Sequence);

            appoint.AddPpAppointMedianPriceCommitteeDuties(duties);
        }

        if (req.Acceptors.Any())
        {
            var userIds = req.Acceptors.Select(s => UserId.From(s.UserId)).ToArray();

            var users = await this.dbContext.SuUsers
                                 .Include(u => u.Employee)
                                 .ThenInclude(s => s.View)
                                 .Where(u => userIds.Contains(u.Id))
                                 .ToArrayAsync(ct);

            var newAcceptors = req.Acceptors
                   .Join(
                       users,
                       a => a.UserId,
                       u => u.Id.Value,
                       (a, u) =>
                       {
                           var info = new AcceptorAppointInfoData(
                               a.AcceptorType,
                               u.Id,
                               u.EmployeeCode,
                               u.Employee.View?.FullName ?? a.FullName,
                               u.Employee.ConvertPositionName(procurement.DepartmentId),
                               u.Employee.View?.BusinessUnitName ?? a.BusinessUnitName,
                               a.Sequence);

                           return CreateAppointAcceptor(appoint, info);
                       });

            newAcceptors.Iter(r =>
            {
                r.SetSendToAcceptorId(UserId.From(req.UserId));
                appoint.AddPpAppointAcceptor(r);
            });
        }

        await this.SetDefaultDocumentTemplate(appoint, procurement.SupplyMethodCode, procurement.Budget, ct);
        await this.dbContext.SaveChangesAsync(ct);

        // Reload entity with includes needed by MapToReplaceDto
        var appointWithIncludes = await this.dbContext.PpAppoints
            .Include(x => x.Procurement).ThenInclude(p => p.Department)
            .Include(x => x.Procurement).ThenInclude(p => p.Plan)
            .Include(x => x.Procurement).ThenInclude(p => p.SupplyMethod)
            .Include(x => x.Procurement).ThenInclude(p => p.SupplyMethodType)
            .Include(x => x.Procurement).ThenInclude(p => p.SupplyMethodSpecialType)
            .Include(x => x.TorDraftCommittees).ThenInclude(c => c.User).ThenInclude(u => u.Employee).ThenInclude(e => e.View)
            .Include(x => x.TorDraftCommitteeDuties)
            .Include(x => x.MedianPriceCommittees).ThenInclude(c => c.User).ThenInclude(u => u.Employee).ThenInclude(e => e.View)
            .Include(x => x.MedianPriceCommitteeDuties)
            .Include(x => x.Acceptors)
            .Include(x => x.DocumentHistories)
            .FirstOrDefaultAsync(a => a.Id == appoint.Id, ct);

        if (appointWithIncludes is not null)
        {
            var lastedDraft = appointWithIncludes.LastedDraftDocument;
            if (lastedDraft is not null)
            {
                var documentService = this.Resolve<IDocumentService>();
                var replaceDto = await this.MapToReplaceDto(appointWithIncludes, ct);
                var copiedFileId = await documentService.CopyDocumentTemplateAsync(
                    lastedDraft.FileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: $"{DocumentTemplateGroups.Ap}/{appointWithIncludes.AppointNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                    cancellationToken: ct);

                if (copiedFileId.HasValue)
                {
                    appointWithIncludes.AddDocumentHistory(copiedFileId.Value);
                    await this.dbContext.SaveChangesAsync(ct);
                }
            }
        }

        return TypedResults.Ok(appoint.Id);
    }
}