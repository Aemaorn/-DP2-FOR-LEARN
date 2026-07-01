namespace GHB.DP2.Application.Features.Procurement.Jp006.Abstract;

using Codehard.Common.Extensions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using GreatFriends.ThaiBahtText;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public abstract partial class Jp006EndpointBase<TRequest, TResponse>
{
    private record DocumentReplaceOptions(
        bool IsReplace,
        bool HasCreator,
        bool HasCommittee,
        bool HasAcceptor,
        bool HasPublisher);

    public record EntrepreneurDetail(
        string? SelectionReasonCode,
        string? Remark,
        IEnumerable<PPurchaseOrderPriceDetails> EntrepreneurPriceDetails);

    protected async Task CreateDocumentAsync(
        Domain.Procurement.Procurement procurement,
        PPurchaseOrder entity,
        Guid userId,
        decimal sumAgreePrice,
        CancellationToken ct)
    {
        if (procurement.IsSixtyAndMoreThanOneHundredThousand)
        {
            return;
        }

        // Create JP006 and Winner Documents.
        await this.SetDefaultDocumentTemplate(
            entity,
            procurement.SupplyMethodCode,
            sumAgreePrice,
            ct);

        // Replace Document Tag
        var documentService = this.Resolve<IDocumentService>();

        var lastedJp006Document = entity.GetLatestDocumentHistory(PurchaseOrderDocumentType.Jp006);
        var lastedWinnerDocument = entity.GetLatestDocumentHistory(PurchaseOrderDocumentType.Winner);

        if (lastedJp006Document is null || lastedWinnerDocument is null)
        {
            return;
        }

        var replaceDto = await this.MapPJp006Replace(
            entity,
            UserId.From(userId),
            entity.Status == PurchaseOrderStatus.WaitingCommitteeApproval,
            false,
            false,
            false,
            ct);

        if (replaceDto is null)
        {
            this.ThrowError("ไม่สามารถสร้าง Replace DTO ได้", StatusCodes.Status400BadRequest);
        }

        var parentDirectory = $"{DocumentTemplateGroups.Jp06}/{entity.PurchaseOrderNumber}";

        var newJp006FileId = await documentService.CopyDocumentTemplateAsync(
            lastedJp006Document.FileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: $"{parentDirectory}/Jp006_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newJp006FileId is null)
        {
            this.ThrowError("เกิดข้อผิดพลาดในการแทนที่แท็กเอกสาร JP006", StatusCodes.Status500InternalServerError);
        }

        var newWinnerFileId = await documentService.CopyDocumentTemplateAsync(
            lastedWinnerDocument.FileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: $"{parentDirectory}/Winner_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newWinnerFileId is null)
        {
            this.ThrowError("เกิดข้อผิดพลาดในการแทนที่แท็กเอกสารผู้ชนะ", StatusCodes.Status500InternalServerError);
        }

        entity.AddDocumentHistory(
            PurchaseOrderDocumentType.Jp006,
            newJp006FileId.Value);
        entity.AddDocumentHistory(
            PurchaseOrderDocumentType.Winner,
            newWinnerFileId.Value);

        this.dbContext.PJp006S.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);
    }

    private async ValueTask SetDefaultDocumentTemplate(
        PPurchaseOrder purchaseOrderData,
        ParameterCode supplyMethodCode,
        decimal? budget,
        CancellationToken ct)
    {
        var docId =
            await this.GetDocumentTemplateForResetAsync(
                purchaseOrderData,
                PurchaseOrderDocumentType.Jp006,
                supplyMethodCode,
                budget ?? 0,
                ct);

        var winnerDocId =
            await this.GetDocumentTemplateForResetAsync(
                purchaseOrderData,
                PurchaseOrderDocumentType.Winner,
                supplyMethodCode,
                budget ?? 0,
                ct);

        purchaseOrderData.AddDocumentHistory(PurchaseOrderDocumentType.Jp006, docId, incrementMajor: true);
        purchaseOrderData.AddDocumentHistory(PurchaseOrderDocumentType.Winner, winnerDocId, incrementMajor: true);
    }

    protected async Task<FileId> GetDocumentTemplateForResetAsync(
        PPurchaseOrder purchaseOrderData,
        PurchaseOrderDocumentType documentType,
        ParameterCode supplyMethodCode,
        decimal budget,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        FileId? fileId;

        if (documentType == PurchaseOrderDocumentType.Jp006)
        {
            var query = this.dbContext.SuDocumentTemplates
                            .Where(dt =>
                                dt.Group == DocumentTemplateGroups.Jp06 &&
                                dt.SupplyMethodCode == supplyMethodCode &&
                                dt.AdditionalInfo != null &&
                                dt.AdditionalInfo.RootElement
                                  .GetProperty(nameof(SuDocumentTemplate.IsEvaluationReport))
                                  .GetBoolean() &&
                                dt.BudgetForDocument.Min <= budget &&
                                (dt.BudgetForDocument.Max == null || budget <= dt.BudgetForDocument.Max) &&
                                dt.IsActive &&
                                (dt.IsChange == null || dt.IsChange == false) &&
                                (dt.IsCancel == null || dt.IsCancel == false))
                            .WhereIfTrue(
                                budget > 500000,
                                dt => dt.AdditionalInfo != null &&
                                      dt.AdditionalInfo.RootElement.GetProperty(nameof(SuDocumentTemplate.IsJorPor))
                                        .GetBoolean() == true);

            fileId = await documentService.GetDocumentTemplateAsync(
                query,
                parentDirectory: $"{DocumentTemplateGroups.Jp06}/{purchaseOrderData.PurchaseOrderNumber}/Jp006_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt",
                cancellationToken: ct);
        }
        else
        {
            fileId = await documentService.GetDocumentTemplateAsync(
                dt =>
                    dt.Group == DocumentTemplateGroups.Jp06 &&
                    dt.SupplyMethodCode == supplyMethodCode &&
                    dt.AdditionalInfo != null &&
                    dt.AdditionalInfo.RootElement
                      .GetProperty(nameof(SuDocumentTemplate.IsWinnerAnnouncement))
                      .GetBoolean() &&
                    dt.IsActive &&
                    (dt.IsChange == null || dt.IsChange == false) &&
                    (dt.IsCancel == null || dt.IsCancel == false) &&
                    dt.BudgetForDocument.Min <= budget &&
                    (dt.BudgetForDocument.Max == null || budget <= dt.BudgetForDocument.Max),
                parentDirectory: $"{DocumentTemplateGroups.Jp06}/{purchaseOrderData.PurchaseOrderNumber}/Winner_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt",
                cancellationToken: ct);
        }

        if (fileId == null)
        {
            this.ThrowError(
                $"ไม่พบเอกสารเทมเพลตสำหรับ {supplyMethodCode}",
                StatusCodes.Status404NotFound);
        }

        return (FileId)fileId;
    }

    protected async Task UpdateOrResetDocumentAsync(
        Domain.Procurement.Procurement procurement,
        PPurchaseOrder entity,
        PurchaseOrderDocumentType documentType,
        bool isReset,
        Guid userId,
        CancellationToken ct)
    {
        if (entity is not { Status: PurchaseOrderStatus.Draft or PurchaseOrderStatus.Edit or PurchaseOrderStatus.Rejected } || procurement.IsSixtyAndMoreThanOneHundredThousand)
        {
            return;
        }

        var documentService = this.Resolve<IDocumentService>();
        var parentDirectory = $"{DocumentTemplateGroups.Jp06}/{entity.PurchaseOrderNumber}";
        var docName = documentType == PurchaseOrderDocumentType.Jp006 ? "Jp006" : "Winner";

        var task = isReset switch
        {
            true => ResetDocumentAsync(),
            false => UpdateDocumentByType(),
        };

        await task;

        async Task ResetDocumentAsync()
        {
            var winners = entity.Entrepreneurs.Where(e => e.IsWinner);

            var sumAgreePrice = winners.Any()
                ? winners.Sum(e => e.PJp006PriceDetails.Sum(pd => pd.AgreedPrice * pd.ParcelQuantity))
                : 0m;

            var templateFileId = await this.GetDocumentTemplateForResetAsync(
                entity,
                documentType,
                procurement.SupplyMethodCode,
                sumAgreePrice,
                ct);

            var replaceDto = await this.MapPJp006Replace(
                entity,
                UserId.From(userId),
                hasCreator: false,
                hasCommittee: false,
                hasAcceptor: false,
                hasPublisher: false,
                ct);

            if (replaceDto is null)
            {
                this.ThrowError("ไม่สามารถสร้าง Replace DTO ได้", StatusCodes.Status400BadRequest);
            }

            var newFileId = await documentService.CopyDocumentTemplateAsync(
                templateFileId,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: $"{parentDirectory}/{docName}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

            if (newFileId is null)
            {
                this.ThrowError(DocumentErrorMessages.CopyDocumentFailed, StatusCodes.Status400BadRequest);
            }

            entity.AddDocumentHistory(documentType, newFileId.Value, incrementMajor: true);
        }

        async Task UpdateDocumentByType()
        {
            var lastedDocument = entity.GetLatestDocumentHistory(documentType);

            if (lastedDocument is null)
            {
                this.ThrowError("ไม่พบเอกสาร", StatusCodes.Status500InternalServerError);
            }

            var newId = await documentService.CopyDocumentTemplateAsync(
                lastedDocument.FileId,
                contents => contents,
                parentDirectory: $"{parentDirectory}/{docName}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

            if (newId is null)
            {
                this.ThrowError("เกิดข้อผิดพลาดในการคัดลอกเอกสาร", StatusCodes.Status500InternalServerError);
            }

            entity.AddDocumentHistory(documentType, newId.Value);
        }
    }

    protected async Task StampWaitingCommitteeApprovalDocument(
        Domain.Procurement.Procurement procurement,
        PPurchaseOrder entity,
        PurchaseOrderDocumentType documentType,
        Guid userId,
        CancellationToken ct)
    {
        if (procurement.IsSixtyAndMoreThanOneHundredThousand)
        {
            return;
        }

        var documentService = this.Resolve<IDocumentService>();
        var parentDirectory = $"{DocumentTemplateGroups.Jp06}/{entity.PurchaseOrderNumber}";
        var docName = documentType == PurchaseOrderDocumentType.Jp006 ? "Jp006" : "Winner";

        await this.StampCheckPointLastedDocument(procurement, entity, documentType, ct);

        var lastedDocument = entity.GetLatestDocumentHistory(documentType);

        if (lastedDocument is null)
        {
            this.ThrowError("ไม่พบเอกสาร", StatusCodes.Status500InternalServerError);
        }

        var replaceDto = await this.MapPJp006Replace(
            entity,
            UserId.From(userId),
            hasCreator: true,
            hasCommittee: false,
            hasAcceptor: false,
            hasPublisher: false,
            ct);

        if (replaceDto is null)
        {
            this.ThrowError("ไม่สามารถสร้าง Replace DTO ได้", StatusCodes.Status400BadRequest);
        }

        var newCreatorFileId = await documentService.CopyDocumentTemplateAsync(
            lastedDocument.FileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: $"{parentDirectory}/Jp006_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newCreatorFileId is null)
        {
            this.ThrowError(DocumentErrorMessages.CopyDocumentFailed, StatusCodes.Status400BadRequest);
        }

        entity.AddDocumentHistory(
            documentType,
            newCreatorFileId.Value);
    }

    protected async Task ReplaceDocumentCommitteeApproved(
        Domain.Procurement.Procurement procurement,
        PPurchaseOrder entity,
        CancellationToken ct)
    {
        if (procurement.IsSixtyAndMoreThanOneHundredThousand)
        {
            return;
        }

        var documentService = this.Resolve<IDocumentService>();
        var parentDirectory = $"{DocumentTemplateGroups.Jp06}/{entity.PurchaseOrderNumber}";

        var sendApproveCommitteeUserLog = await this.dbContext.SuActivityLogs
                                                    .Where(c => c.Key == entity.Id.ToString() &&
                                                                c.ActivityInfo.Type == ActivityLogActionTypeConstant.SendCommitteeApprove)
                                                    .OrderByDescending(r => r.AuditInfo.CreatedAt)
                                                    .FirstOrDefaultAsync(ct);

        if (sendApproveCommitteeUserLog == null)
        {
            return;
        }

        var nonStampCreatorDoc = entity.GetIsReplacedDocumentHistory(
            PurchaseOrderDocumentType.Jp006,
            x => x.StatusState == PurchaseOrderStatus.WaitingCommitteeApproval);

        if (nonStampCreatorDoc is null)
        {
            this.ThrowError("ไม่พบเอกสารที่มีสถานะรออนุมัติคณะกรรมการ", StatusCodes.Status404NotFound);
        }

        var replaceDto = await this.MapPJp006Replace(
            entity,
            UserId.From(sendApproveCommitteeUserLog.AuditInfo.CreatedBy),
            hasCreator: true,
            hasCommittee: true,
            hasAcceptor: false,
            hasPublisher: false,
            ct);

        if (replaceDto is null)
        {
            this.ThrowError("ไม่สามารถสร้าง Replace DTO ได้", StatusCodes.Status400BadRequest);
        }

        var newFileId = await documentService.CopyDocumentTemplateAsync(
            nonStampCreatorDoc.FileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: $"{parentDirectory}/Jp006_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newFileId is null)
        {
            this.ThrowError(DocumentErrorMessages.CopyDocumentFailed, StatusCodes.Status400BadRequest);
        }

        entity.AddDocumentHistory(
            PurchaseOrderDocumentType.Jp006,
            newFileId.Value);
    }

    protected async Task StampCommitteeAndAssigneeRecallOrReject(
        Domain.Procurement.Procurement procurement,
        PPurchaseOrder entity,
        CancellationToken ct)
    {
        if (procurement.IsSixtyAndMoreThanOneHundredThousand)
        {
            return;
        }

        var documentService = this.Resolve<IDocumentService>();
        var parentDirectory = $"{DocumentTemplateGroups.Jp06}/{entity.PurchaseOrderNumber}";

        var nonStampCreatorDoc = entity.GetIsReplacedDocumentHistory(
            PurchaseOrderDocumentType.Jp006,
            x => x.StatusState == PurchaseOrderStatus.WaitingCommitteeApproval);

        if (nonStampCreatorDoc is null)
        {
            this.ThrowError("ไม่พบเอกสารที่มีสถานะรออนุมัติคณะกรรมการ", StatusCodes.Status404NotFound);
        }

        var newFileId = await documentService.CopyDocumentTemplateAsync(
            nonStampCreatorDoc.FileId,
            contents => contents,
            parentDirectory: $"{parentDirectory}/Jp006_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        entity.AddDocumentHistory(
            PurchaseOrderDocumentType.Jp006,
            newFileId.Value,
            incrementMajor: true);
    }

    protected async Task StampCheckPointLastedDocument(
        Domain.Procurement.Procurement procurement,
        PPurchaseOrder entity,
        PurchaseOrderDocumentType documentType,
        CancellationToken ct)
    {
        if (procurement.IsSixtyAndMoreThanOneHundredThousand)
        {
            return;
        }

        var documentService = this.Resolve<IDocumentService>();
        var parentDirectory = $"{DocumentTemplateGroups.Jp06}/{entity.PurchaseOrderNumber}";
        var docName = documentType == PurchaseOrderDocumentType.Jp006 ? "Jp006" : "Winner";

        var lastedDocument = entity.GetLatestDocumentHistory(documentType);

        if (lastedDocument is null)
        {
            this.ThrowError("ไม่พบเอกสาร", StatusCodes.Status500InternalServerError);
        }

        var newMajorFileId = await documentService.CopyDocumentTemplateAsync(
            lastedDocument.FileId,
            contents => contents,
            parentDirectory: $"{parentDirectory}/{docName}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newMajorFileId is null)
        {
            this.ThrowError(DocumentErrorMessages.CopyDocumentFailed, StatusCodes.Status400BadRequest);
        }

        entity.AddDocumentHistory(
            documentType,
            newMajorFileId.Value,
            isReplace: true,
            incrementMajor: true);
    }

    protected async Task RestoreJorPorCommentDocument(
        Domain.Procurement.Procurement procurement,
        PPurchaseOrder entity,
        PurchaseOrderDocumentType documentType,
        CancellationToken ct)
    {
        if (procurement.IsSixtyAndMoreThanOneHundredThousand)
        {
            return;
        }

        var documentService = this.Resolve<IDocumentService>();
        var parentDirectory = $"{DocumentTemplateGroups.Jp06}/{entity.PurchaseOrderNumber}";

        var waitingCommentStampDoc = entity.GetIsReplacedDocumentHistory(
            PurchaseOrderDocumentType.Jp006,
            x => x.StatusState == PurchaseOrderStatus.WaitingComment);

        if (waitingCommentStampDoc is null)
        {
            this.ThrowError("ไม่พบเอกสารที่มีสถานะรอความคิดเห็น", StatusCodes.Status404NotFound);
        }

        var newFileId = await documentService.CopyDocumentTemplateAsync(
            waitingCommentStampDoc.FileId,
            contents => contents,
            parentDirectory: $"{parentDirectory}/Jp006_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newFileId is null)
        {
            this.ThrowError(DocumentErrorMessages.CopyDocumentFailed, StatusCodes.Status400BadRequest);
        }

        entity.AddDocumentHistory(
            documentType,
            newFileId.Value,
            isReplace: true,
            incrementMajor: true);
    }

    protected async Task ReplaceDocumentJorPorComment(
        Domain.Procurement.Procurement procurement,
        PPurchaseOrder entity,
        PurchaseOrderDocumentType documentType,
        Guid userId,
        CancellationToken ct)
    {
        if (procurement.IsSixtyAndMoreThanOneHundredThousand)
        {
            return;
        }

        var documentService = this.Resolve<IDocumentService>();
        var parentDirectory = $"{DocumentTemplateGroups.Jp06}/{entity.PurchaseOrderNumber}";

        var waitingCommentStampDoc = entity.GetIsReplacedDocumentHistory(
            PurchaseOrderDocumentType.Jp006,
            x => x.StatusState == PurchaseOrderStatus.WaitingComment);

        if (waitingCommentStampDoc is null)
        {
            this.ThrowError("ไม่พบเอกสารที่มีสถานะรอความคิดเห็น", StatusCodes.Status404NotFound);
        }

        // unnescessary to replace creator, committee, acceptor, publisher cuz current document already have it.
        var replaceDto = await this.MapPJp006Replace(
            entity,
            UserId.From(userId),
            hasCreator: false,
            hasCommittee: false,
            hasAcceptor: false,
            hasPublisher: false,
            ct);

        if (replaceDto is null)
        {
            this.ThrowError("ไม่สามารถสร้าง Replace DTO ได้", StatusCodes.Status400BadRequest);
        }

        var newJorPorCommentFileId = await documentService.CopyDocumentTemplateAsync(
            waitingCommentStampDoc.FileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: $"{parentDirectory}/Jp006_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newJorPorCommentFileId is null)
        {
            this.ThrowError(DocumentErrorMessages.CopyDocumentFailed, StatusCodes.Status400BadRequest);
        }

        entity.AddDocumentHistory(
            documentType,
            newJorPorCommentFileId.Value);
    }

    protected async Task ReplaceDocumentApproverApproved(
        Domain.Procurement.Procurement procurement,
        PPurchaseOrder entity,
        CancellationToken ct)
    {
        if (procurement.IsSixtyAndMoreThanOneHundredThousand)
        {
            return;
        }

        var documentService = this.Resolve<IDocumentService>();
        var parentDirectory = $"{DocumentTemplateGroups.Jp06}/{entity.PurchaseOrderNumber}";

        var sendApproverUserLog = await this.dbContext.SuActivityLogs
                                            .Where(c => c.Key == entity.Id.ToString() &&
                                                        c.ActivityInfo.Type == ActivityLogActionTypeConstant.SendApprove)
                                            .OrderByDescending(r => r.AuditInfo.CreatedAt)
                                            .FirstOrDefaultAsync(ct);

        if (sendApproverUserLog == null)
        {
            return;
        }

        var isReplacedDoc = entity.GetIsReplacedDocumentHistory(
            PurchaseOrderDocumentType.Jp006,
            x => x.StatusState == PurchaseOrderStatus.WaitingApproval);

        if (isReplacedDoc is null)
        {
            this.ThrowError("ไม่พบเอกสารที่มีสถานะรออนุมัติคณะกรรมการ", StatusCodes.Status404NotFound);
        }

        var replaceDto = await this.MapPJp006Replace(
            entity,
            UserId.From(sendApproverUserLog.AuditInfo.CreatedBy),
            hasCreator: false,
            hasCommittee: false,
            hasAcceptor: true,
            hasPublisher: false,
            ct);

        if (replaceDto is null)
        {
            this.ThrowError("ไม่สามารถสร้าง Replace DTO ได้", StatusCodes.Status400BadRequest);
        }

        var newFileId = await documentService.CopyDocumentTemplateAsync(
            isReplacedDoc.FileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: $"{parentDirectory}/Jp006_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newFileId is null)
        {
            this.ThrowError(DocumentErrorMessages.CopyDocumentFailed, StatusCodes.Status400BadRequest);
        }

        entity.AddDocumentHistory(
            PurchaseOrderDocumentType.Jp006,
            newFileId.Value);
    }

    protected async Task ReplaceDocumentPublisherApproved(
        Domain.Procurement.Procurement procurement,
        PPurchaseOrder entity,
        Guid userId,
        CancellationToken ct)
    {
        if (procurement.IsSixtyAndMoreThanOneHundredThousand)
        {
            return;
        }

        var documentService = this.Resolve<IDocumentService>();
        var parentDirectory = $"{DocumentTemplateGroups.Jp06}/{entity.PurchaseOrderNumber}";

        var lastedWinnerDocument = entity.GetLatestDocumentHistory(PurchaseOrderDocumentType.Winner);

        if (lastedWinnerDocument is null)
        {
            this.ThrowError("ไม่พบเอกสารผู้ชนะ", StatusCodes.Status404NotFound);
        }

        var replaceDto = await this.MapPJp006Replace(
            entity,
            UserId.From(userId),
            hasCreator: false,
            hasCommittee: false,
            hasAcceptor: false,
            hasPublisher: true,
            ct);

        if (replaceDto is null)
        {
            this.ThrowError("ไม่สามารถสร้าง Replace DTO ได้", StatusCodes.Status400BadRequest);
        }

        var newFileId = await documentService.CopyDocumentTemplateAsync(
            lastedWinnerDocument.FileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: $"{parentDirectory}/Winner_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newFileId is null)
        {
            this.ThrowError(DocumentErrorMessages.CopyDocumentFailed, StatusCodes.Status400BadRequest);
        }

        entity.AddDocumentHistory(
            PurchaseOrderDocumentType.Winner,
            newFileId.Value,
            isReplace: true,
            incrementMajor: true);
    }

    protected async Task<Jp006ReplaceDto?> MapPJp006Replace(
        PPurchaseOrder? pPurchaseOrder,
        UserId userId,
        bool hasCreator,
        bool hasCommittee,
        bool hasAcceptor,
        bool hasPublisher,
        CancellationToken ct)
    {
        if (pPurchaseOrder == null)
        {
            return null;
        }

        var jp004Data =
            pPurchaseOrder.Procurement
                          .PurchaseRequisitions.FirstOrDefault();

        var jp005Data =
            pPurchaseOrder.Procurement
                          .Jp005.FirstOrDefault();

        var invite =
            pPurchaseOrder.Procurement
                          .Invites.FirstOrDefault();

        if (jp004Data is null || jp005Data is null)
        {
            this.ThrowError("JorPor data not found", StatusCodes.Status404NotFound);
        }

        var user =
            await this.dbContext.SuUsers
                      .Include(u => u.Employee)
                      .ThenInclude(rawEmployee => rawEmployee.View)
                      .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            this.ThrowError($"User with ID {userId} not found.", StatusCodes.Status404NotFound);
        }

        var procurementReplace =
            new Jp06ProcurementReplaceDto(
                pPurchaseOrder.Procurement.PlanId.Map(p => p.Value),
                pPurchaseOrder.Procurement.ProcurementNumber.Value.ToString(),
                pPurchaseOrder.Procurement.Type,
                pPurchaseOrder.Procurement.Step,
                pPurchaseOrder.Procurement.Department.Name,
                pPurchaseOrder.Procurement.DepartmentId.Value,
                pPurchaseOrder.Procurement.Plan.PlanNumber.ToString(),
                pPurchaseOrder.Procurement.Name,
                pPurchaseOrder.Procurement.Budget.Value.ToCurrencyStringWithComma(),
                pPurchaseOrder.Procurement.Budget.ThaiBahtText(),
                pPurchaseOrder.Procurement.BudgetYear,
                pPurchaseOrder.Procurement.SupplyMethod.Label,
                pPurchaseOrder.Procurement.SupplyMethodCode.Value,
                pPurchaseOrder.Procurement.SupplyMethodType?.Label ?? string.Empty,
                pPurchaseOrder.Procurement.SupplyMethodTypeCode.Value.Value,
                pPurchaseOrder.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                pPurchaseOrder.Procurement.SupplyMethodSpecialTypeCode?.Value,
                pPurchaseOrder.Procurement.Status,
                pPurchaseOrder.Procurement.ExpectingProcurementAt,
                pPurchaseOrder.Procurement.IsStock,
                pPurchaseOrder.Procurement.IsCommercialMaterial,
                pPurchaseOrder.Procurement.Plan.Type,
                pPurchaseOrder.Procurement.ProcessType);

        var supplyMethodTypeValue2 =
            await this.GetSupplyMethodTypeValue2(
                procurementReplace.SupplyMethodTypeCode,
                ct);

        var documentFrom =
            jp004Data.Assignees
                     .MaxBy(a => a.Sequence)?
                     .User.Employee.View?
                     .BusinessUnitName
            ?? string.Empty;

        var documentSubject = pPurchaseOrder.Procurement.Name;

        var lastedAssigneeJp04 = jp004Data.Assignees
                                          .Where(x => x.Type == AssigneeType.Assignee)
                                          .OrderBy(a => a.Sequence)
                                          .LastOrDefault();

        if (lastedAssigneeJp04 is null)
        {
            this.ThrowError("ไม่พบข้อมูล assignee Jp04");
        }

        var sumAgreePrice = pPurchaseOrder.Entrepreneurs
                                          .Where(e => e.IsWinner)
                                          .Sum(e => e.PJp006PriceDetails.Sum(pd => pd.AgreedPrice * pd.ParcelQuantity));

        var processType = SectionProcessType.PurchaseOrder;

        var isCommercialMaterial = pPurchaseOrder.Procurement.IsCommercialMaterial;

        if (isCommercialMaterial && pPurchaseOrder.Procurement.SupplyMethodCode.Value == SupplyMethodConstant.Eighty)
        {
            processType = SectionProcessType.PurchaseOrderCommercialParcel;
        }

        var managers = await this.operationService.GetDefaultAcceptorPositionAsync(
            processType,
            lastedAssigneeJp04.UserId.Value,
            sumAgreePrice,
            pPurchaseOrder.Procurement.SupplyMethodCode.Value,
            pPurchaseOrder.Procurement.SupplyMethodCode.Value is SupplyMethodConstant.Eighty ? null : (string?)pPurchaseOrder.Procurement.SupplyMethodSpecialTypeCode,
            ct);

        var sectionApproveName = managers;

        var jp005LastSectionApproverPosition =
            jp005Data?.Acceptors
                     .Where(a => a.Type == AcceptorType.Approver)?
                     .MaxBy(a => a.Sequence)?
                     .User.Employee.View?
                     .FullPositionName
            ?? string.Empty;

        var suParameters =
            await this.dbContext.SuParameters
                      .AsNoTracking()
                      .Where(p =>
                          p.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.SolId))
                      .ToListAsync(ct);

        var budgetDepartments =
            jp004Data?.Budgets
                     .SelectMany(b =>
                         b.PpPurchaseRequisitionBudgetDetails
                          .Select(bd => bd.Department));

        var solNames =
            budgetDepartments?
                .Select(dep =>
                {
                    var param = suParameters.FirstOrDefault(p => p.Code == dep);

                    if (param == null)
                    {
                        return null;
                    }

                    var parts = param.Label.Split(':', 2);

                    var code = parts[0].Trim();
                    var name = parts[1].Trim();

                    return $"{name} ({code})";
                })
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

        var solName = string.Join(", ", solNames ?? []);

        var procurementCommitteeSection = GetValue(
            jp005Data?.Acceptors
                     .Where(a => a.Type == AcceptorType.ProcurementCommittee)
                     .Any(a => a.PositionName == "ผู้จัดซื้อจัดจ้าง") ?? false,
            "ผู้จัดซื้อจัดจ้าง",
            "คณะกรรมการจัดซื้อจัดจ้าง");

        var medianPriceAmount = jp004Data?.MedianPriceAmount.Value;

        var winners = pPurchaseOrder.Entrepreneurs
                                    .Where(e => e.IsWinner)
                                    .OrderBy(e => e.Sequence);

        var agreedPriceTotal = winners.Any()
            ? winners.Sum(e => e.PJp006PriceDetails
                                .Sum(pd => pd.AgreedPrice * pd.ParcelQuantity))
            : (decimal?)null;

        var offerPriceTotal = winners.Any()
            ? winners.Sum(e => e.PJp006PriceDetails
                                .Sum(pd => pd.OfferedPrice * pd.ParcelQuantity))
            : (decimal?)null;

        var entrepreneurPriceTotal =
            pPurchaseOrder.Entrepreneurs
                          .OrderBy(e => e.Sequence)
                          .Sum(e => e.PJp006PriceDetails
                                     .Sum(pd => pd.AgreedPrice * pd.ParcelQuantity));

        var creator = GetValue(
            hasCreator,
            new CreatorDto(
                user.FullName,
                user.FullName,
                user.Employee.View?.FullPositionName ?? string.Empty,
                "เห็นชอบ",
                user.Employee.View?.BusinessUnitId.Value ?? string.Empty),
            null);

        var hasIncludeVat =
            pPurchaseOrder.Entrepreneurs
                          .Where(e => e.IsWinner)
                          .Any(e =>
                              e.PJp006PriceDetails
                               .Any(pd => pd.VatTypeCode == VatTypeConstant.IncluedVat));
        var vatDescription = GetValue(hasIncludeVat, "รวมภาษีมูลค่าเพิ่ม", "ไม่รวมภาษีมูลค่าเพิ่ม");

        var entrepreneurDetail =
            pPurchaseOrder.Entrepreneurs
                          .Where(e => e.IsWinner)
                          .OrderBy(e => e.Sequence)
                          .Select(e =>
                              new EntrepreneurDetail(
                                  e.SelectionReasonCode,
                                  e.Remark,
                                  e.PJp006PriceDetails))
                          .ToArray();

        var winReasonParameters =
            await this.dbContext.SuParameters
                      .AsNoTracking()
                      .Where(p =>
                          p.GroupCode == GroupCode.From(WinReasonConstant.GroupCode))
                      .ToListAsync(ct);

        var selectionReasonTextSource = Jp006EndpointBase<TRequest, TResponse>.GetSelectionReasonTextSource(entrepreneurDetail, winReasonParameters);

        var selectionReasonText = GetValue(entrepreneurDetail.Any(), string.Join(", ", selectionReasonTextSource), string.Empty);

        var remarkText = GetValue(entrepreneurDetail.Any(), string.Join(", ", entrepreneurDetail.Select(r => r.Remark)), string.Empty);

        var glAccounts =
            jp004Data?.Budgets
                     .SelectMany(b =>
                         b.PpPurchaseRequisitionBudgetDetails
                          .Select(bd => bd.AccountNo.Label)).ToArray() ?? [];

        var glAccountText = glAccounts.JoinWithLastPrefix("และ");

        var commandNumber = managers.FirstOrDefault()?.CommandNumber;

        var commandText = this.commandTextService.GetCommandText(
            CommandTextProgram.JorPor06,
            managers,
            pPurchaseOrder.Procurement.SupplyMethodCode,
            sumAgreePrice,
            supplyMethodSpecialType: pPurchaseOrder.Procurement.SupplyMethodSpecialTypeCode,
            supplyMethodSpecialName: pPurchaseOrder.Procurement.SupplyMethodSpecialType?.Label,
            commandNumber: commandNumber);

        var entrepreneurs =
            pPurchaseOrder.Entrepreneurs
                          .Where(e => e.IsWinner)
                          .Select(e => MapToEntrepreneurReplace(e, procurementCommitteeSection, pPurchaseOrder, pPurchaseOrder.Procurement.PurchaseRequisitions.FirstOrDefault()))
                          .OrderBy(x => x.PriceDetails?.Sum(y => y.AgreedPrice) ?? 0m);

        var entrepreneurWinner = pPurchaseOrder.Entrepreneurs
                                               .Where(e => e.IsWinner)
                                               .Select(e => MapToEntrepreneurReplace(e, procurementCommitteeSection, pPurchaseOrder, pPurchaseOrder.Procurement.PurchaseRequisitions.FirstOrDefault()))
                                               .FirstOrDefault();

        var allEntrepreneurs =
            pPurchaseOrder.Entrepreneurs
                          .Select(e => MapToEntrepreneurReplace(e, procurementCommitteeSection, pPurchaseOrder, pPurchaseOrder.Procurement.PurchaseRequisitions.FirstOrDefault()))
                          .OrderBy(x => x.PriceDetails?.Sum(y => y.AgreedPrice) ?? 0m)
                          .Select((x, i) => x with { Sequence = i + 1 });

        var acceptors = GetValue(
            hasAcceptor,
            pPurchaseOrder.Acceptors
                          .Where(a => a is { Type: AcceptorType.Approver, IsUnableToPerformDuties: false })
                          .Select(DelegatorExtensions.DelegatorToAcceptor)
                          .Select(MapToAcceptorReplace)
                          .OrderBy(a => a.Sequence)
                          .ToList(),
            []);

        if (acceptors.Any())
        {
            acceptors[^1] = acceptors.Last() with { Action = "อนุมัติ" };
        }

        acceptors = [.. acceptors.Where(a => a.Status == AcceptorStatus.Approved)];

        var committees = GetValue(
            hasCommittee,
            pPurchaseOrder.Acceptors
                          .Where(a => a.Type == AcceptorType.ProcurementCommittee)
                          .Select(MapToAcceptorReplace)
                          .OrderBy(a => a.Sequence)
                          .ToList(),
            []);

        var assignees = pPurchaseOrder.Assignees
                                      .Select(MapToAssigneeResponse)
                                      .OrderBy(o => o.Sequence)
                                      .ToList();

        var lastCommentAssignee = pPurchaseOrder.Status is PurchaseOrderStatus.WaitingComment
            ? pPurchaseOrder.Assignees
                            .Where(a => a.Type == AssigneeType.Assignee && !string.IsNullOrWhiteSpace(a.Remark))
                            .Select(DelegatorExtensions.DelegatorToAssignee)
                            .OrderByDescending(a => a.ActionAt)
                            .FirstOrDefault()
            : null;

        var jorPorComment = lastCommentAssignee is not null
            ? new JorPorCommentReplace(
                lastCommentAssignee.UserId.Value,
                lastCommentAssignee.FullName,
                lastCommentAssignee.FullName,
                lastCommentAssignee.PositionName,
                lastCommentAssignee.Remark,
                "ผู้จัดทำ")
            : null;

        var publisher = GetValue(
            hasPublisher,
            pPurchaseOrder.Acceptors
                          .Where(a => a.Type == AcceptorType.Approver && a.IsActive)
                          .OrderBy(a => a.Sequence)
                          .Select(DelegatorExtensions.DelegatorToAcceptor)
                          .LastOrDefault(),
            null);

        var acceptorDate = pPurchaseOrder.Status is not (
                    PurchaseOrderStatus.Draft or
                    PurchaseOrderStatus.Rejected or
                    PurchaseOrderStatus.Edit)
                ? pPurchaseOrder.DocumentDate?.ToThaiDateString(format: "d MMMM yyyy") ?? DateTime.Now.ToThaiDateString(format: "d MMMM yyyy")
                : null;

        var publisherDate = hasPublisher ? DateTime.Now.ToThaiDateString(format: "d MMMM yyyy") : null;

        PublisherDto? publisherDto = null;

        if (publisher != null)
        {
            var isMd = await this.dbContext.SuUsers
                .Where(u => u.Id == publisher.UserId)
                .SelectMany(u => u.Employee.Positions)
                .Where(p => p.Position != null && InRefCodeConstant.MD.Contains(p.Position.InRefCode))
                .AnyAsync(ct);

            var publisherPositionName = !isMd
                ? $"{publisher.PositionName ?? string.Empty} ทำการแทน"
                : publisher.PositionName ?? string.Empty;

            var publisherSignature = publisher.Delegatee != null ? publisher.SignatureDelegatee : publisher.Signature;
            var publisherManagingDirector = !isMd ? "กรรมการผู้จัดการ" : string.Empty;

            publisherDto = new PublisherDto(publisherSignature, publisher.FullName, publisherPositionName, string.Empty, string.Empty, publisherManagingDirector);
        }

        var result =
            new Jp006ReplaceDto(
                string.Empty,
                procurementReplace,
                acceptorDate,
                documentFrom,
                documentSubject,
                sectionApproveName?.Select(x => new SectionApprove(x.PositionName)),
                jp005LastSectionApproverPosition,
                solName,
                procurementCommitteeSection,
                medianPriceAmount != null ? medianPriceAmount.Value.ToCurrencyStringWithComma() : string.Empty,
                medianPriceAmount != null ? medianPriceAmount.ThaiBahtText() : string.Empty,
                agreedPriceTotal.ToCurrencyStringWithComma(),
                agreedPriceTotal.ThaiBahtText(),
                offerPriceTotal.ToCurrencyStringWithComma(),
                offerPriceTotal.ThaiBahtText(),
                entrepreneurPriceTotal.ToCurrencyStringWithComma(),
                entrepreneurPriceTotal.ThaiBahtText(),
                vatDescription,
                selectionReasonText,
                remarkText,
                commandText,
                glAccountText,
                supplyMethodTypeValue2,
                creator,
                entrepreneurs,
                allEntrepreneurs,
                entrepreneurWinner,
                acceptors,
                committees,
                assignees,
                publisherDto,
                publisherDate,
                invite != null && invite.IsInvite ? "ได้มีหนังสือเชิญชวน" : "ได้มีโครงการ",
                pPurchaseOrder.PurchaseOrderNumber.ToString() ?? string.Empty,
                lastedAssigneeJp04?.BusinessUnitName ?? string.Empty,
                jorPorComment);

        return result;
    }

    private static List<string> GetSelectionReasonTextSource(IEnumerable<EntrepreneurDetail> entrepreneurDetail, List<SuParameter> winReasonParameters)
    {
        var selectionReasonTextSource = new List<string>();

        foreach (var entrepreneur in entrepreneurDetail)
        {
            if (entrepreneur.SelectionReasonCode == WinReasonConstant.WinReason005)
            {
                selectionReasonTextSource.Add(entrepreneur.Remark ?? string.Empty);

                continue;
            }

            var winReasonText =
                winReasonParameters.FirstOrNone(wr => wr.Code == entrepreneur.SelectionReasonCode)
                                   .Match(wr => wr.Label, () => string.Empty);

            selectionReasonTextSource.Add(winReasonText);
        }

        return selectionReasonTextSource;
    }

    private static T GetValue<T>(bool condition, T valueIfTrue, T valueIfFalse)
    {
        return condition ? valueIfTrue : valueIfFalse;
    }

    private static Jp006EntrepreneurReplaceDto MapToEntrepreneurReplace(PPurchaseOrderEntrepreneur entrepreneur, string procurementCommitteeSection, PPurchaseOrder? purchaseOrder, PpPurchaseRequisition? jorpor04)
    {
        var budget = purchaseOrder?.Procurement?.Budget ?? 0;
        var medianPrice = jorpor04?.MedianPriceAmount ?? 0;

        var totalOfferedPrice = entrepreneur.PJp006PriceDetails.Sum(x => x.OfferedPrice * x.ParcelQuantity);
        var totalAgreedPrice = entrepreneur.PJp006PriceDetails.Sum(x => x.AgreedPrice * x.ParcelQuantity);
        var deliveryPeriod = jorpor04?.DeliveryConditionCode.ToString() != "DelvCUnit005"
            ? string.Format("{0} {1}", jorpor04?.DeliveryPeriod, jorpor04?.DeliveryPeriodType?.Label)
            : jorpor04?.DeliveryDate.ToThaiDateString();
        var isUnderBudget = totalAgreedPrice <= budget;
        var isOverBudget = totalAgreedPrice > budget;
        var isUnderMedianPrice = totalAgreedPrice <= medianPrice;
        var isOverMedianPrice = totalAgreedPrice > medianPrice;

        var budgetDiff = Math.Abs(totalAgreedPrice - budget);
        var budgetDiffPercent = (budgetDiff * 100) / (isUnderBudget ? budget : totalAgreedPrice);

        var medianDiff = Math.Abs(totalAgreedPrice - medianPrice);
        var medianPriceDif = (medianDiff * 100) / (isUnderMedianPrice ? medianPrice : totalAgreedPrice);

        var low = "ต่ำกว่า";
        var high = "สูงกว่า";

        var detail1 = string.Format(
            "{0} พิจารณาแล้วเห็นว่า {1} เป็นผู้ผ่านการพิจารณาและเสนอราคาต่ำสุด โดยเสนอค่า{2} ระยะเวลา {8} เป็นเงินทั้งสิ้น (รวมภาษีมูลค่าเพิ่ม) {4} บาท ({5}) ดังนั้นจึงเห็นควรเจรจาต่อรองราคากับ {1} และได้ต่อรองราคาจนถึงที่สุดแล้ว บริษัทฯ ยินดีลดราคาลงคงเหลือเป็นเงินทั้งสิ้น (รวมภาษีมูลค่าเพิ่ม) {6} ({7})",
            procurementCommitteeSection,
            entrepreneur.SuVendor.EstablishmentName,
            purchaseOrder?.Procurement?.Name,
            deliveryPeriod,
            totalOfferedPrice.ToCurrencyStringWithComma(),
            totalOfferedPrice.ThaiBahtText(),
            totalAgreedPrice.ToCurrencyStringWithComma(),
            totalAgreedPrice.ThaiBahtText(),
            deliveryPeriod);
        var detail2 = string.Format(
            "จากตารางข้างต้น {0} พิจารณาแล้วเห็นว่าราคาดังกล่าวมีความเหมาะสม กล่าวคือ ราคา{1}วงเงินงบประมาณ เป็นจำนวนเงิน {2} บาท คิดเป็นร้อยละ {3} ของวงเงินงบประมาณและ{4}ราคากลาง (ราคากลางอ้างอิง) เป็นจำนวนเงิน {5} บาท คิดเป็นร้อยละ {6} ของราคากลาง (ราคากลางอ้างอิง) ดังนั้น จึงเห็นสมควร{7} ระยะเวลา {8} เป็นจำนวนเงินทั้งสิ้น (รวมภาษีมูลค่าเพิ่ม) {9} บาท ({10}) จาก โดยวิธี{11} ตามรายละเอียดข้างต้น",
            procurementCommitteeSection,
            isUnderBudget ? low : high,
            budgetDiff.ToCurrencyStringWithComma(),
            budgetDiffPercent.ToCurrencyStringWithComma(),
            isUnderMedianPrice ? low : high,
            medianDiff.ToCurrencyStringWithComma(),
            medianPriceDif.ToCurrencyStringWithComma(),
            purchaseOrder?.Procurement?.Name,
            deliveryPeriod,
            totalAgreedPrice.ToCurrencyStringWithComma(),
            totalAgreedPrice.ThaiBahtText(),
            purchaseOrder?.Procurement?.SupplyMethodSpecialType);

        return new Jp006EntrepreneurReplaceDto(
            entrepreneur.Id.Value,
            entrepreneur.SuVendorId.Value,
            entrepreneur.EmailSended,
            entrepreneur.Sequence,
            new EntrepreneurCheckConditions(entrepreneur.CoiResult, entrepreneur.CoiRemark, entrepreneur.CoiDate),
            new EntrepreneurCheckConditions(entrepreneur.WatchlistResult, entrepreneur.WatchlistRemark, entrepreneur.WatchlistDate),
            new EntrepreneurCheckConditions(entrepreneur.EgpResult, entrepreneur.EgpRemark, entrepreneur.EgpDate),
            entrepreneur.SuVendor.TaxpayerIdentificationNo,
            entrepreneur.SuVendor.EntrepreneurTypeInfo.Label,
            entrepreneur.SuVendor.EstablishmentName,
            entrepreneur.SuVendor.Email,
            entrepreneur.SuVendor.Nationality,
            entrepreneur.SuVendor.Type,
            entrepreneur.SuVendor.PlaceName,
            entrepreneur.SuVendor.Tel,
            entrepreneur.IsWinner,
            entrepreneur.SelectionReasonCode,
            entrepreneur.Remark,
            entrepreneur.PJp006PriceDetails.Select(MapToPriceDetailsReplace).OrderBy(s => s.Sequence),
            MapToPriceDetailReplace(entrepreneur.PJp006PriceDetails.OrderBy(s => s.Sequence)),
            entrepreneur.PJp006PriceDetails.All(x => !string.IsNullOrWhiteSpace(x.VatTypeCode)),
            [
                .. entrepreneur.PurchaseOrderShareholders.Select(s => new PurchaseOrderEntrepreneurShareholderReplaceDto(
                    s.Id.Value,
                    s.Sequence,
                    s.TaxId,
                    s.FirstName,
                    s.LastName,
                    s.IsDirector,
                    s.IsShareholder,
                    s.WatchlistResult,
                    s.WatchlistResultRemark,
                    s.WatchlistResultAt,
                    s.CoiResult,
                    s.CoiResultRemark,
                    s.CoiResultAt,
                    s.EgpResult,
                    s.EgpRemark,
                    s.EgpResultAt))
            ],
            detail1,
            detail2);
    }

    private static Jp006PriceDetailsReplaceDto MapToPriceDetailsReplace(PPurchaseOrderPriceDetails priceDetails)
    {
        return new Jp006PriceDetailsReplaceDto(
            priceDetails.Id.Value,
            priceDetails.Sequence,
            priceDetails.ParcelName,
            priceDetails.ParcelQuantity.ToString("N0"),
            priceDetails.ParcelUnitCode,
            priceDetails.VatTypeCode,
            priceDetails.OfferedPrice,
            priceDetails.AgreedPrice,
            priceDetails.Description);
    }

    private static Jp006PriceDetailReplaceDto MapToPriceDetailReplace(IEnumerable<PPurchaseOrderPriceDetails> priceDetails)
    {
        return new Jp006PriceDetailReplaceDto(
            priceDetails.Sum(x => x.OfferedPrice * x.ParcelQuantity).ToCurrencyStringWithComma(),
            priceDetails.Sum(x => x.AgreedPrice * x.ParcelQuantity).ToCurrencyStringWithComma());
    }

    private static Jp006AcceptorReplaceDtoInfo MapToAcceptorReplace(PPurchaseOrderAcceptor acceptor)
    {
        var action =
            acceptor.Type switch
            {
                AcceptorType.Approver => "เห็นชอบ",
                AcceptorType.ProcurementCommittee =>
                    acceptor.Status switch
                    {
                        AcceptorStatus.Approved => "เห็นชอบ",
                        AcceptorStatus.Rejected => "ไม่เห็นชอบ",
                        AcceptorStatus.UnableToPerformDuties => acceptor.Remark,
                        _ => string.Empty,
                    },
                _ => string.Empty,
            };

        return new Jp006AcceptorReplaceDtoInfo(
            action,
            acceptor.Id.Value,
            acceptor.Type,
            acceptor.UserId.Value,
            acceptor.Sequence,
            acceptor.FullName,
            acceptor.PositionName,
            acceptor.BusinessUnitName,
            acceptor.Status,
            acceptor.Remark,
            acceptor.ActionAt,
            Optional(acceptor.CommitteePositionsCode)
                .Map(v => v.Value)
                .IfNoneUnsafe((string?)null),
            acceptor.CommitteePosition?.Label,
            acceptor.IsUnableToPerformDuties,
            acceptor.IsCurrentApprover(),
            acceptor.User?.Employee?.PrimaryDepartment != null ? (string)acceptor.User.Employee.PrimaryDepartment.Id : string.Empty,
            string.Empty);
    }

    private async Task<string> GetSupplyMethodTypeValue2(
        string? supplyMethodType,
        CancellationToken ct)
    {
        var supplyMethodTypeSource =
            await this.dbContext.SuParameters
                      .AsNoTracking()
                      .FirstOrDefaultAsync(
                          p => p.Code == ParameterCode.From(supplyMethodType!), ct);

        if (supplyMethodTypeSource is null)
        {
            return string.Empty;
        }

        var supplyMethodType2Text =
            supplyMethodTypeSource?.Values
                                  .FirstOrDefault(v => v.Key == "Values2")
                                  .Value
                                  .Value?.ToString();

        if (supplyMethodType2Text is null)
        {
            return string.Empty;
        }

        return supplyMethodType2Text;
    }
}