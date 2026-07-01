namespace GHB.DP2.Application.Features.SystemUtility.SuVendorShareholders;

using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;

public record SuVendorShareholdersDto(
    Guid VendorId,
    int Sequence,
    string TaxId,
    string FirstName,
    string LastName,
    bool IsDirector,
    bool? IsShareholder,
    DateTimeOffset? ModifiedAt);

public interface ISuVendorShareholdersService
{
    Task<IEnumerable<SuVendorShareholdersDto>> GetDefaultShareholdersByVendorId(Guid vendorId, CancellationToken cancellationToken);

    Task<IEnumerable<SuVendorShareholdersDto>> GetDefaultShareholders(CancellationToken cancellationToken);
}

[RegisterService<ISuVendorShareholdersService>(LifeTime.Scoped)]
public class SuVendorShareholdersService : ISuVendorShareholdersService
{
    private readonly Dp2DbContext dbContext;

    public SuVendorShareholdersService(Dp2DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    private readonly string originalQuery = @"
					WITH combined_data AS (
                        SELECT 
                            ""Id"" as  ""EntrepreneurId"",
                            ""VendorId"",
                            COALESCE(""LastModifiedAt"", ""CreatedAt"") AS ""ModifiedAt"",
                            'Invited' AS ""ProgramName""
                        FROM ""Procurement"".""PInvitedEntrepreneurs""
                        WHERE ""IsDeleted"" is false
                        UNION ALL
                        SELECT 
                            ""Id"" as  ""EntrepreneurId"",
                            ""SuVendorId"" AS ""VendorId"",
                            COALESCE(""LastModifiedAt"", ""CreatedAt"") AS ""ModifiedAt"",
                            'PurchaseOrder' AS ""ProgramName""
                        FROM ""Procurement"".""PPurchaseOrderEntrepreneur""
                        WHERE ""IsDeleted"" is false
                        UNION ALL
                        SELECT 
                            ""Id"" as  ""EntrepreneurId"",
                            ""VendorId"",
                            COALESCE(""LastModifiedAt"", ""CreatedAt"") AS ""ModifiedAt"",
                            'PrincipleApprovalRental' AS ""ProgramName""
                        FROM ""Procurement"".""PPrincipleApprovalRentalEntrepreneurs""
                        WHERE ""IsDeleted"" is false
                        UNION ALL
                        SELECT 
                            ""Id"" as ""EntrepreneurId"",
                            ""VendorId"",
                            COALESCE(""LastModifiedAt"", ""CreatedAt"") AS ""ModifiedAt"",
                            'ContractDraft' AS ""ProgramName""
                        FROM ""ContractAgreement"".""CaContractDraftVendor""
                        WHERE ""IsDeleted"" is false
                    ),
                    ranked_data AS (
                        SELECT 
                            *,
                            ROW_NUMBER() OVER (PARTITION BY ""VendorId"" ORDER BY ""ModifiedAt"" DESC) AS rn
                        FROM combined_data
                    )
                    select
	                    rd.""VendorId"",
	                    a.""Sequence"",
	                    a.""TaxId"",
	                    a.""FirstName"",
	                    a.""LastName"",
	                    a.""IsDirector"",
	                    a.""IsShareholder"",
	                    rd.""ModifiedAt""
                    from
                    (
	                    select ""InvitedEntrepreneurId"" as ""EntrepreneurId"", ""Sequence"", ""TaxId"" , ""FirstName"" , ""LastName"", ""IsDirector"", ""IsShareholder""  from ""Procurement"".""PInvitedEntrepreneurShareholders""
	                    where ""IsDeleted"" is false
	                    union all
	                    select ""PurchaseOrderEntrepreneurId"" as ""EntrepreneurId"", ""Sequence"", ""TaxId"" , ""FirstName"" , ""LastName"", ""IsDirector"", ""IsShareholder""  from  ""Procurement"".""PPurchaseOrderEntrepreneurShareholders""
	                    where ""IsDeleted"" is false
	                    union all
	                    select ""EntrepreneursId"", ""Sequence"", ""TaxId"" , ""FirstName"" , ""LastName"", ""IsDirector"", ""IsShareholder""  from  ""Procurement"".""PPrincipleApprovalRentalEntrepreneursShareholders""
	                    where ""IsDeleted"" is false
	                    union all
	                    select ""ContractInvitationVendorsId""  as ""EntrepreneurId"", ""Sequence"", ""TaxId"" , ""FirstName"" , ""LastName"", ""IsDirector"", ""IsShareholder""  from  ""ContractAgreement"".""CaContractInvitationVendorShareholders""
	                    where ""IsDeleted"" is false
	                    ) a
                    inner join ranked_data rd on rd.""EntrepreneurId"" = a.""EntrepreneurId""
                    WHERE rn = 1";

    public async Task<IEnumerable<SuVendorShareholdersDto>> GetDefaultShareholdersByVendorId(Guid vendorId, CancellationToken cancellationToken = default)
    {
        var allShareholders = await this.dbContext
                                        .Database
                                        .SqlQueryRaw<SuVendorShareholdersDto>(this.originalQuery)
                                        .Where(p => p.VendorId == vendorId)
                                        .OrderBy(p => p.VendorId)
                                        .ThenBy(p => p.Sequence)
                                        .ToListAsync(cancellationToken);

        return allShareholders;
    }

    public async Task<IEnumerable<SuVendorShareholdersDto>> GetDefaultShareholders(CancellationToken cancellationToken = default)
    {
        var allShareholders = await this.dbContext.Database.SqlQueryRaw<SuVendorShareholdersDto>(this.originalQuery).ToListAsync(cancellationToken);

        return allShareholders;
    }
}