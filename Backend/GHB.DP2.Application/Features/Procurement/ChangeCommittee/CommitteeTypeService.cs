namespace GHB.DP2.Application.Features.Procurement.ChangeCommittee;

using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;

public record CommitteeGroupTypeServiceDto(
    string SourceType,
    Guid SourceId,
    string CommitteeGroupType);

public interface ICommitteeGroupTypeServiceService
{
    Task<IEnumerable<CommitteeGroupTypeServiceDto>> GetCommitteeGroupTypeByProcurementId(Guid procurementId, CancellationToken cancellationToken);
}

[RegisterService<ICommitteeGroupTypeServiceService>(LifeTime.Scoped)]
public class CommitteeGroupTypeServiceService : ICommitteeGroupTypeServiceService
{
    private readonly Dp2DbContext dbContext;

    public CommitteeGroupTypeServiceService(Dp2DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    private readonly string originalQuery = @"
                    SELECT 
                        'Appoint' AS ""SourceType"",
                        pa.""Id"" AS ""SourceId"",
                        'TOR' AS ""CommitteeGroupType""
                    FROM ""Procurement"".""PpAppoint"" pa
                    WHERE pa.""ProcurementId"" = @ProcurementId
                        AND EXISTS (
                            SELECT 1 FROM ""Procurement"".""PpAppointTorDraftCommittee"" patdc
                            WHERE patdc.""PpAppointId"" = pa.""Id""
                        )

                    UNION ALL

                    SELECT 
                        'Appoint' AS ""SourceType"",
                        pa.""Id"" AS ""SourceId"",
                        'MedianPrice' AS ""CommitteeGroupType""
                    FROM ""Procurement"".""PpAppoint"" pa
                    WHERE pa.""ProcurementId"" = @ProcurementId
                        AND EXISTS (
                            SELECT 1 FROM ""Procurement"".""PpAppointMedianPriceCommittee"" pampc
                            WHERE pampc.""PpAppointId"" = pa.""Id""
                        )

                    UNION ALL

                    SELECT DISTINCT * FROM (
                        SELECT
                            CASE
                                WHEN p.""Id"" IS NOT NULL THEN 'Jp005'
                                ELSE 'PurchaseRequisition'
                            END AS ""SourceType"",
                            COALESCE(p.""Id"", ppr.""Id"") AS ""SourceId"",
                            COALESCE(pc.""GroupType"", pprc.""GroupType"") AS ""CommitteeGroupType""
                        FROM ""Procurement"".""PpPurchaseRequisitionCommittee"" pprc
                        INNER JOIN ""Procurement"".""PpPurchaseRequisition"" ppr
                            ON pprc.""PpPurchaseRequisitionId"" = ppr.""Id""
                        LEFT JOIN ""Procurement"".""PJp005"" p
                            ON p.""PpPurchaseRequisitionId"" = ppr.""Id""
                        LEFT JOIN ""Procurement"".""PJp005Committee"" pc
                            ON p.""Id"" = pc.""PJp005Id""
                        WHERE ppr.""ProcurementId"" = @ProcurementId
                    ) a

                    UNION ALL

                    SELECT DISTINCT
                        'PrincipleApproval' AS ""SourceType"",
                        pa.""Id"" AS ""SourceId"",
                        pac.""GroupType"" AS ""CommitteeGroupType""
                    FROM ""Procurement"".""PPrincipleApproval"" pa
                    INNER JOIN ""Procurement"".""PPrincipleApprovalCommittee"" pac
                        ON pac.""PPrincipleApprovalId"" = pa.""Id""
                    WHERE pa.""ProcurementId"" = @ProcurementId
                        AND pac.""IsDeleted"" = false

                    UNION ALL

                    SELECT DISTINCT
                        'PurchaseOrderApproval' AS ""SourceType"",
                        poa.""Id"" AS ""SourceId"",
                        poac.""GroupType"" AS ""CommitteeGroupType""
                    FROM ""Procurement"".""PPurchaseOrderApproval"" poa
                    INNER JOIN ""Procurement"".""PPurchaseOrderApprovalCommittee"" poac
                        ON poac.""PurchaseOrderApprovalId"" = poa.""Id""
                    WHERE poa.""ProcurementId"" = @ProcurementId
                        AND poa.""IsDeleted"" = false
                ";

    public async Task<IEnumerable<CommitteeGroupTypeServiceDto>> GetCommitteeGroupTypeByProcurementId(
        Guid procurementId,
        CancellationToken cancellationToken)
    {
        var sqlParams = new[]
        {
            new Npgsql.NpgsqlParameter("@ProcurementId", procurementId),
        };

        var allCommittees = await this.dbContext
                                       .Database
                                       .SqlQueryRaw<CommitteeGroupTypeServiceDto>(this.originalQuery, sqlParams)
                                       .ToListAsync(cancellationToken);

        return allCommittees;
    }
}