namespace GHB.DP2.Application.CommandHandler;

using System.Globalization;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class NotificationWorkGuaranteeReturnCommand : ICommand;

public class NotificationWorkGuaranteeReturnHandler
    : ICommandHandler<NotificationWorkGuaranteeReturnCommand>
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<NotificationWorkGuaranteeReturnHandler> logger;

    public NotificationWorkGuaranteeReturnHandler(
        ILogger<NotificationWorkGuaranteeReturnHandler> logger,
        IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
    }

    public async Task ExecuteAsync(
        NotificationWorkGuaranteeReturnCommand command,
        CancellationToken ct)
    {
        this.logger.LogInformation("Starting NotificationWorkGuaranteeReturn.");

        await using var scope = this.serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Dp2DbContext>();
        var emailServiceFactory = scope.ServiceProvider.GetRequiredService<IEmailServiceFactory>();

        var today = DateTimeOffset.UtcNow.Date;
        var deadlineThreshold = today.AddDays(30);

        var completedDeliveryRefIds = await dbContext.CmDeliveryAcceptances
            .Where(da => da.Status == CmDeliveryAcceptanceStatus.Completed)
            .Where(da => da.SourceType == SourceType.ContractDraftVendor && da.RefId.HasValue)
            .Select(da => da.RefId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var completedVendorIds = completedDeliveryRefIds
            .Select(ContractDraftVendorId.From)
            .ToList();

        var vendors = await dbContext.CaContractDraftVendors
            .Include(v => v.DraftTermsConditions)
                .ThenInclude(tc => tc.Warranty)
            .Include(v => v.CmContractGuaranteeReturns)
            .Include(v => v.ContractDraft)
            .Include(v => v.Vendor)
            .Where(v => completedVendorIds.Contains(v.Id))
            .Where(v => !v.CmContractGuaranteeReturns.Any())
            .Where(v => v.DraftTermsConditions.Guarantee.IsSubmitted == true)
            .ToListAsync(ct);

        this.logger.LogInformation(
            "Found {Count} vendors with completed delivery and no guarantee return.",
            vendors.Count);

        var vendorsToNotify = vendors.Where(v =>
        {
            var hasWarranty = v.DraftTermsConditions.Warranty.HasWarranty == true;

            var deadlineDate = hasWarranty
                ? v.DraftTermsConditions.Warranty.WarrantyEndDate
                : v.ContractEndDate;

            return deadlineDate.HasValue
                   && deadlineDate.Value.Date >= today
                   && deadlineDate.Value.Date <= deadlineThreshold;
        }).ToList();

        this.logger.LogInformation(
            "Filtered to {Count} vendors within 30-day deadline window.",
            vendorsToNotify.Count);

        foreach (var vendor in vendorsToNotify)
        {
            var procurementId = vendor.ContractDraft.ProcurementId;

            var committees = await dbContext.PJp005Committees
                .Include(c => c.User)
                    .ThenInclude(u => u.Employee)
                .Where(c => c.PJp005.ProcurementId == procurementId)
                .Where(c => c.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                .OrderBy(c => c.Sequence)
                .ToListAsync(ct);

            if (committees.Count == 0)
            {
                this.logger.LogWarning(
                    "No inspection committee found for vendor {VendorId}, procurement {ProcurementId}.",
                    vendor.Id,
                    procurementId);

                continue;
            }

            var hasWarranty = vendor.DraftTermsConditions.Warranty.HasWarranty == true;

            var deadlineDate = hasWarranty
                ? vendor.DraftTermsConditions.Warranty.WarrantyEndDate!.Value
                : vendor.ContractEndDate!.Value;

            var contractRef = vendor.ContractNumber;

            foreach (var member in committees)
            {
                _ = SendBellNotificationAsync(vendor, member);
                await SendEmailAsync(emailServiceFactory, vendor, member, deadlineDate, contractRef, ct);
            }
        }

        this.logger.LogInformation("Completed NotificationWorkGuaranteeReturn.");
    }

    private static async Task SendBellNotificationAsync(
        Domain.ContractAgreement.CaContractDraft.CaContractDraftVendor vendor,
        PJp005Committee member)
    {
        await Notification
            .Crate(
                member.SuUserId,
                NotificationConstant.GuaranteeReturnReminder.Title,
                string.Format(
                    NotificationConstant.GuaranteeReturnReminder.Message,
                    ProgramConstant.ContractGuaranteeReturn.Name,
                    vendor.PoNumber ?? vendor.ContractNumber),
                NotificationProgram.ContractManagement)
            .SetReferenceId(vendor.Id.Value)
            .SetLinkUrl(
                string.Format(
                    ProgramConstant.ContractGuaranteeReturn.Url,
                    vendor.Id,
                    string.Empty),
                "ดูรายละเอียด")
            .PublishAsync(CancellationToken.None);
    }

    private static async Task SendEmailAsync(
        IEmailServiceFactory emailServiceFactory,
        Domain.ContractAgreement.CaContractDraft.CaContractDraftVendor vendor,
        PJp005Committee member,
        DateTimeOffset deadlineDate,
        string contractRef,
        CancellationToken ct)
    {
        var email = member.User?.Employee?.Email;

        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var thaiCulture = new CultureInfo("th-TH");
        var signedDateText = vendor.ContractSignedDate?.ToString("d MMMM yyyy", thaiCulture) ?? "-";
        var deadlineDateText = deadlineDate.ToString("d MMMM yyyy", thaiCulture);
        var vendorName = vendor.Vendor?.EstablishmentName ?? "-";

        var body = $"""
            <div style="font-family: 'TH SarabunPSK', 'Sarabun', sans-serif; font-size: 16px; color: #333; line-height: 1.8;">
                <p>เรียน คณะกรรมการตรวจรับ</p>

                <p style="text-indent: 2em;">
                    ตามที่บริษัท {vendorName} จำกัด ดำเนินการทำสัญญา {vendor.ContractName}
                    สัญญาเลขที่ {contractRef} ลงวันที่ {signedDateText}
                    ซึ่งสัญญาดังกล่าวอยู่ในความดูแลของคณะกรรมการตรวจรับและจะครบกำหนดพ้นภาระผูกพันตามสัญญาในวันที่ {deadlineDateText} นั้น
                </p>

                <p style="text-indent: 2em;">
                    ส่วนบริหารสัญญา ฝ่ายจัดหาและการพัสดุ จึงขอความอนุเคราะห์ให้ท่านช่วยตรวจสอบว่าสามารถคืนหลักประกันสัญญาให้แก่
                    บริษัทฯ คู่สัญญาได้หรือไม่ โดยพิจารณาสาระสำคัญ
                </p>

                <ol style="margin-left: 1em;">
                    <li>บริษัทฯ ได้มีการปฏิบัติตามข้อกำหนดเงื่อนไขในสัญญาครบถ้วนหรือไม่</li>
                    <li>มีความเสียหายเกิดขึ้นจากการปฏิบัติงานตามสัญญาหรือไม่</li>
                    <li>มีความชำรุดบกพร่องของสิ่งของตามสัญญานี้ ซึ่งจะต้องเรียกร้องให้บริษัทฯ แก้ไขหรือชดใช้หรือไม่</li>
                    <li><u>พร้อมได้มีการตรวจรับและเบิกจ่ายเงินงวดสุดท้ายเรียบร้อยแล้ว</u></li>
                </ol>

                <p>
                    เพื่อส่วนบริหารสัญญาจะได้ดำเนินการขออนุมัติคืนหลักประกันให้กับบริษัทฯตามระเบียบ
                    กระทรวงการคลังว่าด้วยการจัดซื้อจัดจ้างและการบริหารพัสดุภาครัฐ พ.ศ. 2560 ต่อไป
                </p>

                <br/>
                <p style="margin-bottom: 0;">ส่วนบริหารสัญญา ฝ่ายจัดหาและการพัสดุ</p>
                <p style="margin-top: 0; margin-bottom: 0; padding-left: 1em;">ธนาคารอาคารสงเคราะห์</p>
                <p style="margin-top: 0; padding-left: 1em;">โทร.02-2022105 , 02-2022109</p>
            </div>
            """;

        await emailServiceFactory.Create()
            .To(email, member.FullName)
            .Subject($"แจ้งเตือนคืนหลักประกันสัญญา - {vendor.ContractName}")
            .Html(body)
            .SendAsync(ct);
    }
}
