namespace GHB.DP2.Application.Features.Plan.PlanAnnouncement.Abstract;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Plan.Dto;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public abstract partial class PlanAnnouncementEndpointBase<TRequest, TResponse>
{
    protected async Task<(FileId ApproveFileId, FileId AnnouncementFileId)> UpdateDocumentHistory(
        PlanAnnouncement announcement,
        bool isReplace,
        bool hasPublicPlan = false,
        bool hasAcceptors = false,
        bool hasAssignees = false,
        CancellationToken cancellationToken = default)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var approveDocument = hasPublicPlan ? announcement.Document : announcement.LastDraftDocument;
        var announceDocument = hasPublicPlan ? announcement.AnnouncementDocument : announcement.LastDraftAnnouncementDocument;

        var approveDocumentWithAcceptor = hasAcceptors
            ? announcement.LastedWaitingAcceptorDocument
            : approveDocument;

        var announceDocumentWithAcceptor = hasAcceptors
            ? announcement.LastedWaitingAcceptorAnnouncementDocument
            : announceDocument;

        if (approveDocumentWithAcceptor is null || announceDocumentWithAcceptor is null)
        {
            this.ThrowError(
                "ไม่พบเอกสารแบบฟอร์มการขออนุมัติแผนประจำปี",
                StatusCodes.Status404NotFound);
        }

        var approveFileId = await ReplaceDocument(approveDocumentWithAcceptor.FileId, PlanAnnouncementDocumentType.Approve);
        var announceFileId = await ReplaceDocument(announceDocumentWithAcceptor.FileId, PlanAnnouncementDocumentType.Announcement);

        announcement.AddDocumentHistory(
            PlanAnnouncementDocumentType.Approve,
            approveFileId,
            hasPublicPlan || hasAcceptors);

        announcement.AddDocumentHistory(
            PlanAnnouncementDocumentType.Announcement,
            announceFileId,
            hasPublicPlan || hasAcceptors);

        return (approveFileId, announceFileId);

        async Task<FileId> ReplaceDocument(FileId fileId, PlanAnnouncementDocumentType documentType)
        {
            if (!isReplace)
            {
                return fileId;
            }

            var replaceDto =
                this.MapToPlanAnnouncementReplateAsync(announcement, hasPublicPlan, hasAcceptors, hasAssignees);

            var parentDirectory =
                $"{DocumentTemplateGroups.PlanAnnouncement}/{announcement.PlanAnnouncementNumber}_{documentType.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

            var copyFileId =
                await documentService.CopyDocumentTemplateAsync(
                    fileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: parentDirectory,
                    cancellationToken: cancellationToken);

            if (copyFileId is null)
            {
                this.ThrowError(
                    "ไม่สามารถคัดลอกเอกสารแบบฟอร์มการขออนุมัติแผนประจำปีได้",
                    StatusCodes.Status500InternalServerError);
            }

            return copyFileId.Value;
        }
    }

    protected PlanAnnouncementReplate MapToPlanAnnouncementReplateAsync(
        Domain.Plan.PlanAnnouncement planAnnouncement,
        bool hasPublicPlan = false,
        bool hasAcceptors = false,
        bool hasAssignees = false)
    {
        var planSelected =
            planAnnouncement.AnnouncementSelectedInformations
                            .Select(s => s.Plan)
                            .Where(plan => plan is not null)
                            .Select((plan, index) => new PlanSelectedReplate(
                                index + 1,
                                plan.PlanNumber.Value,
                                plan.EgpNumber,
                                plan.Name,
                                plan.Budget.ToCurrencyStringWithComma(),
                                plan.ExpectingProcurementAt.ToOffset(TimeSpan.FromHours(7)).ToThaiDateString(format: "MM/yyyy")))
                            .ToHashSet();

        if (planSelected.Count > 0)
        {
            planSelected.Add(planSelected.First());
        }

        var createPlan =
            hasAssignees
                ? Optional(this.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value)
                  .Map(Guid.Parse)
                  .Map(UserId.From)
                  .MatchUnsafe(
                      id =>
                      {
                          if (planAnnouncement.Status is PlanAnnouncementStatus.Draft)
                          {
                              return null;
                          }

                          var createPlanReplace =
                              planAnnouncement.Assignees
                                              .Select(DelegatorExtensions.DelegatorToAssignee)
                                              .Where(u => u.Delegatee == null ? u.UserId == id : u.Delegatee.SuUserId == id)
                                              .Select(u => new CreatePlanReplace(
                                                  "ผู้จัดทำ",
                                                  u.FullName,
                                                  u.PositionName,
                                                  u.User.Employee.PrimaryBusinessUnit?.Name))
                                              .FirstOrDefault();

                          return createPlanReplace;
                      },
                      () => null)
                : null;

        var lastedAssignee = planAnnouncement.Assignees
                                             .OrderByDescending(a => a.Sequence)
                                             .FirstOrDefault();

        var organizationLevel600 = lastedAssignee?.User?.Employee?.PrimaryBusinessUnit?.OrganizationLevel == EmployeeConstant.OrganizationLevel.Segment;
        var buName = organizationLevel600 ? lastedAssignee?.User?.Employee?.PrimaryBusinessUnit?.Name : string.Empty;

        var department = $"{buName} {lastedAssignee?.User?.Employee?.View?.BusinessUnitName}";

        var budgetTotal =
            planAnnouncement.AnnouncementSelectedInformations
                            .Where(s => s.Plan is not null)
                            .Select(s => s.Plan.Budget)
                            .Sum();
        var budgetText =
            budgetTotal.ThaiBahtText();

        var publicPlanReplace =
            hasPublicPlan
                ? planAnnouncement.Assignees
                                  .Where(a =>
                                      a is { Type: AssigneeType.Director })
                                  .Select(DelegatorExtensions.DelegatorToAssignee)
                                  .OrderByDescending(a => a.Sequence)
                                  .Select(a => new PublicPlanReplace(
                                      a.DelegateeId != null ? a.SignatureDelegatee : a.Signature,
                                      a.FullName,
                                      a.PositionName,
                                      string.Empty))
                                  .FirstOrDefault()
                : null;

        var lastAcceptors =
            hasAcceptors
                ? planAnnouncement.Acceptors
                                  .Where(a =>
                                      a is { Type: AcceptorType.Approver })
                                  .OrderBy(a => a.Sequence)
                                  .LastOrDefault()
                : new PlanAnnouncementAcceptor();

        var acceptors =
            hasAcceptors
                ? planAnnouncement.Acceptors
                                  .Where(a =>
                                      a is { Type: AcceptorType.Approver, Status: AcceptorStatus.Approved })
                                  .Select(DelegatorExtensions.DelegatorToAcceptor)
                                  .OrderBy(a => a.Sequence)
                                  .Select(a =>
                                  {
                                      var action =
                                          (a.Status, lastAcceptors == a) switch
                                          {
                                              (AcceptorStatus.Approved, false) => "เห็นชอบ",
                                              (AcceptorStatus.Approved, true) => "อนุมัติ",
                                              _ => "ไม่เห็นชอบ",
                                          };

                                      return new AcceptorReplace(
                                          action,
                                          a.FullName,
                                          a.PositionName,
                                          string.Empty);
                                  })
                : new List<AcceptorReplace>();

        var publicDate =
            planAnnouncement.Status == PlanAnnouncementStatus.Announcement
                ? DateTimeOffset.UtcNow.ToThaiDateString()
                : null;

        var approvedDate =
           planAnnouncement.Status != PlanAnnouncementStatus.Draft && planAnnouncement.Status != PlanAnnouncementStatus.WaitingAssign
               ? planAnnouncement.DocumentDate?.ToThaiDateString() ?? DateTimeOffset.UtcNow.ToThaiDateString()
               : null;

        var announcementDate =
            planAnnouncement.AnnouncementDate?.ToThaiDateString(includeBuddhistEra: true);

        return new PlanAnnouncementReplate(
            planAnnouncement.PlanAnnouncementNumber.Value,
            planAnnouncement.GroupEgpNumber,
            planAnnouncement.Year,
            department,
            planAnnouncement.Telephone ?? string.Empty,
            planAnnouncement.SupplyMethodInfo.Label,
            budgetTotal.ToCurrencyStringWithComma(),
            budgetText,
            planAnnouncement.Remark,
            planAnnouncement.AnnouncementTitle,
            announcementDate,
            planSelected.Count,
            approvedDate,
            publicDate,
            null,
            createPlan,
            planSelected,
            publicPlanReplace,
            acceptors);
    }

    protected async ValueTask<Results<Ok<Guid>, NotFound<string>>> PreviewPlanAnnouncementDocumentAsync(
        Domain.Plan.PlanAnnouncement planAnnouncementData,
        PlanAnnouncementDocumentType documentType,
        IFileServiceClient fileServiceClient,
        bool hasPublicPlan = false,
        bool hasAcceptors = false,
        bool hasAssignees = false,
        CancellationToken cancellationToken = default)
    {
        var response = this.MapToPlanAnnouncementReplateAsync(planAnnouncementData, hasPublicPlan, hasAcceptors, hasAssignees);

        var getLastedDraftDocumentHistory = planAnnouncementData.DocumentHistories
                                                                .Where(d =>
                                                                    d.DocumentType == documentType)
                                                                .OrderByDescending(d => d.Version)
                                                                .FirstOrDefault();

        if (getLastedDraftDocumentHistory is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลแผนที่ร่าง");
        }

        var file = await fileServiceClient.DownloadAsync(getLastedDraftDocumentHistory.FileId, cancellationToken: cancellationToken);

        if (file == null)
        {
            return TypedResults.NotFound("ไม่พบไฟล์แผนที่ร่าง");
        }

        var fileContent = OdtDocumentExtensions.ReplaceOdtDocument(file.Contents, response);

        var odt = DocumentService.DetectContentType(fileContent);
        var unixTimeOneDay = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds();
        var fileResult = await fileServiceClient.UploadFileAsync(
            fileContent,
            contentType: odt,
            expirationUnixSeconds: unixTimeOneDay,
            cancellationToken: cancellationToken);

        return TypedResults.Ok(fileResult.Id.Value);
    }
}