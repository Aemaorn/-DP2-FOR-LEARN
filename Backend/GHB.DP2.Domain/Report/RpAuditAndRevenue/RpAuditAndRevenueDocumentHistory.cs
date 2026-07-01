namespace GHB.DP2.Domain.Report.RpAuditAndRevenue;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct RpAuditAndRevenueDocumentHistoryId
{
    public static RpAuditAndRevenueDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum RpAuditAndRevenueDocumentType
{
    /// <summary>
    /// รายงาน สตง. และหนังสือถึงสรรพากร
    /// </summary>
    AuditReport,

    /// <summary>
    /// รายงาน สตง. ถึงผู้ว่าการตรวจเงินแผ่นดิน
    /// </summary>
    AuditGeneralReport,

    /// <summary>
    /// รายงานหนังสือถึงสรรพากรถึงอธิบดีกรมสรรพากร
    /// </summary>
    RevenueReport,
}

public class RpAuditAndRevenueDocumentHistory : DocumentHistory<RpAuditAndRevenueDocumentHistoryId>
{
    public override RpAuditAndRevenueDocumentHistoryId Id { get; init; }

    public RpAuditAndRevenueDocumentType DocumentType { get; init; }

    public RpAuditAndRevenueStatus StatusState { get; init; }

    public static RpAuditAndRevenueDocumentHistory Create(
        RpAuditAndRevenueDocumentType documentType,
        RpAuditAndRevenueStatus statusState,
        string version,
        FileId fileId,
        bool isReplace)
    {
        return new RpAuditAndRevenueDocumentHistory
        {
            Id = RpAuditAndRevenueDocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace,
        };
    }
}