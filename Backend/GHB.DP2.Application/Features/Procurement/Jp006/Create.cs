namespace GHB.DP2.Application.Features.Procurement.Jp006;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp006.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Domain.Procurement;
using HttpConflict = Microsoft.AspNetCore.Http.HttpResults.Conflict<string>;

public class CreateJp006Endpoint : Jp006EndpointBase<CreateJp006Request, Results<Created<Guid>, HttpConflict>>
{
    private readonly Dp2DbContext dbContext;

    public CreateJp006Endpoint(
        ILogger<AssigneePurchaseOrderEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, operationService, commandTextService, fileServiceClient, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/jp006");
        this.Description(b => b
                              .WithTags(nameof(Jp006))
                              .WithName("CreatePJp006")
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Created<Guid>, HttpConflict>> HandleRequestAsync(
        CreateJp006Request req,
        CancellationToken ct)
    {
        using var tx = await this.dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        var procurementExisting = await this.ValidateProcurementAsync(req.ProcurementId, ct);

        var existingJp006 = await this.dbContext.PJp006S
            .AnyAsync(
                p => p.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                     p.Status != PurchaseOrderStatus.Cancelled,
                ct);

        if (existingJp006)
        {
            return TypedResults.Conflict("มีข้อมูล จพ.006 อยู่แล้วสำหรับการจัดซื้อจัดจ้างนี้");
        }

        var jp006 = req.MapToEntity(procurementExisting);

        if (req.Acceptors is not null)
        {
            await this.UpsertAcceptors(jp006, req.Acceptors, procurementExisting.DepartmentId, UserId.From(req.UserId));
        }

        if (req.Assignees is not null)
        {
            await this.UpsertAssignee(jp006, req.Assignees, ct, UserId.From(req.UserId));
        }

        _ = await req.Entrepreneurs.Select(async x =>
        {
            var entrepreneursData = jp006.Entrepreneurs.FirstOrDefault(e => e.SuVendorId == SuVendorId.From(x.VendorId));

            if (entrepreneursData is null)
            {
                return unit;
            }

            if (x.Attachments is not null && x.Attachments.Any())
            {
                await this.ValidateDocumentTypeCode(x.Attachments, ct);
                await this.UpsertAttachments(entrepreneursData, x.Attachments);
            }

            return unit;
        }).SequenceSerial();

        if (req.DocumentDate is not null)
        {
            jp006.SetDocumentDate(req.DocumentDate);
        }

        jp006.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            "สร้างข้อมูลขออนุมัติสั่งซื้อ/สั่งจ้าง(จพ.006)",
            jp006.Status.ToString()));

        this.dbContext.PJp006S.Add(jp006);

        try
        {
            await this.dbContext.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            return TypedResults.Conflict("มีข้อมูล จพ.006 อยู่แล้วสำหรับการจัดซื้อจัดจ้างนี้");
        }

        return TypedResults.Created(string.Empty, jp006.Id.Value);
    }
}