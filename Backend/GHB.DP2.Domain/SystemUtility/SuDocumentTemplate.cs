namespace GHB.DP2.Domain.SystemUtility;

using System.Text.Json;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct SuDocumentTemplateId
{
    public static SuDocumentTemplateId New() => From(Guid.CreateVersion7());
}

public class BudgetRange
{
    public decimal Min { get; private set; }

    public decimal? Max { get; private set; }

    public BudgetRange SetRange(decimal min, decimal? max)
    {
        this.Min = min;
        this.Max = max;

        return this;
    }

    public static BudgetRange Default => new()
    {
        Min = 0m,
        Max = default,
    };
}

public partial class SuDocumentTemplate : AuditableEntity<SuDocumentTemplateId>, IHasSoftDelete, IDisposable
{
    public override SuDocumentTemplateId Id { get; init; }

    public ParameterCode? SupplyMethodCode { get; private set; }

    public BudgetRange BudgetForDocument { get; private set; }

    public string Group { get; private set; }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public string? PreviewPfdFileName { get; private set; }

    public FileId PreviewPfdFileId { get; private set; }

    public FileId FileId { get; private set; }

    public bool IsActive { get; private set; }

    public bool? IsCancel { get; private set; }

    public bool? IsChange { get; private set; }

    /// <summary>
    /// จพ. ให้ความเห็น
    /// </summary>
    public bool? IsJorPorComment =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.IsJorPorComment), out var property) == true
            ? property.GetBoolean()
            : null;

    public bool? IsJorPor =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.IsJorPor), out var property) == true
            ? property.GetBoolean()
            : null;

    /// <summary>
    /// มีค่าปรับ
    /// </summary>
    public bool? IsFine =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.IsFine), out var property) == true
            ? property.GetBoolean()
            : null;

    /// <summary>
    /// ประเภทสัญญา
    /// </summary>
    public string? ContractTemplateCode =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.ContractTemplateCode), out var property) == true
            ? property.GetString()
            : null;

    /// <summary>
    /// ประกาศผู้ชนะ
    /// </summary>
    public bool? IsWinnerAnnouncement =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.IsWinnerAnnouncement), out var property) == true
            ? property.GetBoolean()
            : null;

    /// <summary>
    /// รายงานผลการพิจารณาและขออนุมัติ
    /// </summary>
    public bool? IsEvaluationReport =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.IsEvaluationReport), out var property) == true
            ? property.GetBoolean()
            : null;

    /// <summary>
    /// คำสั่งแต่งตั้ง
    /// </summary>
    public bool? IsAppointmentOrdered =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.IsAppointmentOrdered), out var property) == true
            ? property.GetBoolean()
            : null;

    /// <summary>
    /// ขออนุมัติ
    /// </summary>
    public bool? IsApproval =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.IsApproval), out var property) == true
            ? property.GetBoolean()
            : null;

    /// <summary>
    /// ระหว่างปีงบประมาณ
    /// </summary>
    public bool? IsInYear =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.IsInYear), out var property) == true
            ? property.GetBoolean()
            : null;

    /// <summary>
    /// เผยแพร่
    /// </summary>
    public bool? IsPublished =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.IsPublished), out var property) == true
            ? property.GetBoolean()
            : null;

    /// <summary>
    /// ประเภทวิธีการจัดหา
    /// </summary>
    public string? SupplyMethodTypeCode =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.SupplyMethodTypeCode), out var property) == true
            ? property.GetString()
            : null;

    /// <summary>
    /// มีการรับประกัน
    /// </summary>
    public bool? HasGuarantee =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.HasGuarantee), out var property) == true
            ? property.GetBoolean()
            : null;

    /// <summary>
    /// เป็นความลับ
    /// </summary>
    public bool? IsConfidential =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.IsConfidential), out var property) == true
            ? property.GetBoolean()
            : null;

    public bool? IsPDPA =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.IsPDPA), out var property) == true
            ? property.GetBoolean()
            : null;

    /// <summary>
    /// ประเภทการเช่า
    /// </summary>
    public string? PrincipleApprovalTemplateCode =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.PrincipleApprovalTemplateCode), out var property) == true
            ? property.GetString()
            : null;

    /// <summary>
    /// ประเภทการเช่า
    /// </summary>
    public string? PrincipleApprovalRentalTemplateCode =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.PrincipleApprovalRentalTemplateCode), out var property) == true
            ? property.GetString()
            : null;

    /// <summary>
    /// เป็นความลับ
    /// </summary>
    public bool? IsWinnerPrincipleApprovalRental =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.IsWinnerPrincipleApprovalRental), out var property) == true
            ? property.GetBoolean()
            : null;

    public string? ContractAmendmentDocumentType =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.ContractAmendmentDocumentType), out var property) == true
            ? property.GetString()
            : null;

    public string? Quarter =>
        this.AdditionalInfo?.RootElement
            .TryGetProperty(nameof(this.Quarter), out var property) == true
            ? property.GetString()
            : null;

    public JsonDocument? AdditionalInfo { get; private set; }

    public virtual SuParameter? SupplyMethodCodeInfo { get; init; }

    private static IDictionary<string, object> JsonDocumentToDictionary(JsonDocument jsonDocument)
    {
        return jsonDocument.RootElement
                           .EnumerateObject()
                           .ToDictionary(
                               prop => prop.Name,
                               prop => GetJsonElementValue(prop.Value));
    }

    private static object GetJsonElementValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => element.GetRawText(),
        };
    }

    public static SuDocumentTemplate Create(
        string group,
        string code,
        string name,
        bool isActive = true)
    {
        return new SuDocumentTemplate
        {
            Id = SuDocumentTemplateId.New(),
            Group = group,
            Code = code,
            Name = name,
            IsActive = isActive,
            BudgetForDocument = BudgetRange.Default,
        };
    }

    public Unit Update(
        string group,
        string name,
        bool isActive = true)
    {
        this.Group = group;
        this.Name = name;
        this.IsActive = isActive;

        return unit;
    }

    public SuDocumentTemplate SetBudgetForDocument(decimal budgetForDocument, decimal? maxBudgetForDocument = null)
    {
        if (maxBudgetForDocument is not null && maxBudgetForDocument < budgetForDocument)
        {
            throw new ArgumentException("Max budget must be greater than or equal to the budget for document.");
        }

        this.BudgetForDocument.SetRange(budgetForDocument, maxBudgetForDocument);

        return this;
    }

    public SuDocumentTemplate SetSupplyMethodCode(ParameterCode? supplyMethodCodeInfo)
    {
        this.SupplyMethodCode = supplyMethodCodeInfo;

        return this;
    }

    public SuDocumentTemplate SetIsCancel(bool? isCancel)
    {
        this.IsCancel = isCancel;

        return this;
    }

    public SuDocumentTemplate SetIsChange(bool? isChange)
    {
        this.IsChange = isChange;

        return this;
    }

    public SuDocumentTemplate SetContractTemplateCode(string? contractTemplateCode)
    {
        var additionalInfo = this.AdditionalInfo != null
            ? JsonDocumentToDictionary(this.AdditionalInfo)
            : new Dictionary<string, object>();

        if (string.IsNullOrEmpty(contractTemplateCode))
        {
            additionalInfo.Remove(nameof(this.ContractTemplateCode));
        }
        else
        {
            additionalInfo[nameof(this.ContractTemplateCode)] = contractTemplateCode;
        }

        this.AdditionalInfo?.Dispose();
        this.AdditionalInfo = additionalInfo.Count > 0
            ? JsonDocument.Parse(JsonSerializer.Serialize(additionalInfo))
            : null;

        return this;
    }

    public SuDocumentTemplate SetIsFine(bool? isFine)
    {
        var additionalInfo = this.AdditionalInfo != null
            ? JsonDocumentToDictionary(this.AdditionalInfo)
            : new Dictionary<string, object>();

        if (isFine is null)
        {
            additionalInfo.Remove(nameof(this.IsFine));
        }
        else
        {
            additionalInfo[nameof(this.IsFine)] = isFine;
        }

        this.AdditionalInfo?.Dispose();
        this.AdditionalInfo = additionalInfo.Count > 0
            ? JsonDocument.Parse(JsonSerializer.Serialize(additionalInfo))
            : null;

        return this;
    }

    public SuDocumentTemplate SetIsJorPorComment(bool? isJorPorComment)
    {
        var additionalInfo = this.AdditionalInfo != null
            ? JsonDocumentToDictionary(this.AdditionalInfo)
            : new Dictionary<string, object>();

        if (isJorPorComment is null)
        {
            additionalInfo.Remove(nameof(this.IsJorPorComment));
        }
        else
        {
            additionalInfo[nameof(this.IsJorPorComment)] = isJorPorComment;
        }

        this.AdditionalInfo?.Dispose();
        this.AdditionalInfo = additionalInfo.Count > 0
            ? JsonDocument.Parse(JsonSerializer.Serialize(additionalInfo))
            : null;

        return this;
    }

    public SuDocumentTemplate SetIsWinnerAnnouncement(bool? isWinnerAnnouncement)
    {
        var additionalInfo = this.AdditionalInfo != null
            ? JsonDocumentToDictionary(this.AdditionalInfo)
            : new Dictionary<string, object>();

        if (isWinnerAnnouncement is null)
        {
            additionalInfo.Remove(nameof(this.IsWinnerAnnouncement));
        }
        else
        {
            additionalInfo[nameof(this.IsWinnerAnnouncement)] = isWinnerAnnouncement;
        }

        this.AdditionalInfo?.Dispose();
        this.AdditionalInfo = additionalInfo.Count > 0
            ? JsonDocument.Parse(JsonSerializer.Serialize(additionalInfo))
            : null;

        return this;
    }

    public SuDocumentTemplate SetIsEvaluationReport(bool? isEvaluationReport)
    {
        var additionalInfo = this.AdditionalInfo != null
            ? JsonDocumentToDictionary(this.AdditionalInfo)
            : new Dictionary<string, object>();

        if (isEvaluationReport is null)
        {
            additionalInfo.Remove(nameof(this.IsEvaluationReport));
        }
        else
        {
            additionalInfo[nameof(this.IsEvaluationReport)] = isEvaluationReport;
        }

        this.AdditionalInfo?.Dispose();
        this.AdditionalInfo = additionalInfo.Count > 0
            ? JsonDocument.Parse(JsonSerializer.Serialize(additionalInfo))
            : null;

        return this;
    }

    public SuDocumentTemplate SetIsAppointmentOrdered(bool? isAppointmentOrdered)
    {
        var additionalInfo = this.AdditionalInfo != null
            ? JsonDocumentToDictionary(this.AdditionalInfo)
            : new Dictionary<string, object>();

        if (isAppointmentOrdered is null)
        {
            additionalInfo.Remove(nameof(this.IsAppointmentOrdered));
        }
        else
        {
            additionalInfo[nameof(this.IsAppointmentOrdered)] = isAppointmentOrdered;
        }

        this.AdditionalInfo?.Dispose();
        this.AdditionalInfo = additionalInfo.Count > 0
            ? JsonDocument.Parse(JsonSerializer.Serialize(additionalInfo))
            : null;

        return this;
    }

    public SuDocumentTemplate SetIsApproval(bool? isApproval)
    {
        var additionalInfo = this.AdditionalInfo != null
            ? JsonDocumentToDictionary(this.AdditionalInfo)
            : new Dictionary<string, object>();

        if (isApproval is null)
        {
            additionalInfo.Remove(nameof(this.IsApproval));
        }
        else
        {
            additionalInfo[nameof(this.IsApproval)] = isApproval;
        }

        this.AdditionalInfo?.Dispose();
        this.AdditionalInfo = additionalInfo.Count > 0
            ? JsonDocument.Parse(JsonSerializer.Serialize(additionalInfo))
            : null;

        return this;
    }

    public SuDocumentTemplate SetIsInYear(bool? isInYear)
    {
        var additionalInfo = this.AdditionalInfo != null
            ? JsonDocumentToDictionary(this.AdditionalInfo)
            : new Dictionary<string, object>();

        if (isInYear is null)
        {
            additionalInfo.Remove(nameof(this.IsInYear));
        }
        else
        {
            additionalInfo[nameof(this.IsInYear)] = isInYear;
        }

        this.AdditionalInfo?.Dispose();
        this.AdditionalInfo = additionalInfo.Count > 0
            ? JsonDocument.Parse(JsonSerializer.Serialize(additionalInfo))
            : null;

        return this;
    }

    public SuDocumentTemplate SetIsPublished(bool? isPublished)
    {
        var additionalInfo = this.AdditionalInfo != null
            ? JsonDocumentToDictionary(this.AdditionalInfo)
            : new Dictionary<string, object>();

        if (isPublished is null)
        {
            additionalInfo.Remove(nameof(this.IsPublished));
        }
        else
        {
            additionalInfo[nameof(this.IsPublished)] = isPublished;
        }

        this.AdditionalInfo?.Dispose();
        this.AdditionalInfo = additionalInfo.Count > 0
            ? JsonDocument.Parse(JsonSerializer.Serialize(additionalInfo))
            : null;

        return this;
    }

    public SuDocumentTemplate SetSupplyMethodTypeCode(string? supplyMethodTypeCode)
    {
        var additionalInfo = this.AdditionalInfo != null
            ? JsonDocumentToDictionary(this.AdditionalInfo)
            : new Dictionary<string, object>();

        if (string.IsNullOrEmpty(supplyMethodTypeCode))
        {
            additionalInfo.Remove(nameof(this.SupplyMethodTypeCode));
        }
        else
        {
            additionalInfo[nameof(this.SupplyMethodTypeCode)] = supplyMethodTypeCode;
        }

        this.AdditionalInfo?.Dispose();
        this.AdditionalInfo = additionalInfo.Count > 0
            ? JsonDocument.Parse(JsonSerializer.Serialize(additionalInfo))
            : null;

        return this;
    }

    public SuDocumentTemplate SetHasGuarantee(bool? hasGuarantee)
    {
        var additionalInfo = this.AdditionalInfo != null
            ? JsonDocumentToDictionary(this.AdditionalInfo)
            : new Dictionary<string, object>();

        if (hasGuarantee is null)
        {
            additionalInfo.Remove(nameof(this.HasGuarantee));
        }
        else
        {
            additionalInfo[nameof(this.HasGuarantee)] = hasGuarantee;
        }

        this.AdditionalInfo?.Dispose();
        this.AdditionalInfo = additionalInfo.Count > 0
            ? JsonDocument.Parse(JsonSerializer.Serialize(additionalInfo))
            : null;

        return this;
    }

    public SuDocumentTemplate SetIsConfidential(bool? isConfidential)
    {
        var additionalInfo = this.AdditionalInfo != null
            ? JsonDocumentToDictionary(this.AdditionalInfo)
            : new Dictionary<string, object>();

        if (isConfidential is null)
        {
            additionalInfo.Remove(nameof(this.IsConfidential));
        }
        else
        {
            additionalInfo[nameof(this.IsConfidential)] = isConfidential;
        }

        this.AdditionalInfo?.Dispose();
        this.AdditionalInfo = additionalInfo.Count > 0
            ? JsonDocument.Parse(JsonSerializer.Serialize(additionalInfo))
            : null;

        return this;
    }

    public SuDocumentTemplate SetPrincipleApprovalTemplateCode(string? principleApprovalTemplateCode)
    {
        var additionalInfo = this.AdditionalInfo != null
            ? JsonDocumentToDictionary(this.AdditionalInfo)
            : new Dictionary<string, object>();

        if (string.IsNullOrEmpty(principleApprovalTemplateCode))
        {
            additionalInfo.Remove(nameof(this.PrincipleApprovalTemplateCode));
        }
        else
        {
            additionalInfo[nameof(this.PrincipleApprovalTemplateCode)] = principleApprovalTemplateCode;
        }

        this.AdditionalInfo?.Dispose();
        this.AdditionalInfo = additionalInfo.Count > 0
            ? JsonDocument.Parse(JsonSerializer.Serialize(additionalInfo))
            : null;

        return this;
    }

    public SuDocumentTemplate SetContractAmendmentDocumentType(string? contractAmendmentDocumentType)
    {
        var additionalInfo = this.AdditionalInfo != null
            ? JsonDocumentToDictionary(this.AdditionalInfo)
            : new Dictionary<string, object>();

        if (string.IsNullOrEmpty(contractAmendmentDocumentType))
        {
            additionalInfo.Remove(nameof(this.ContractAmendmentDocumentType));
        }
        else
        {
            additionalInfo[nameof(this.ContractAmendmentDocumentType)] = contractAmendmentDocumentType;
        }

        this.AdditionalInfo?.Dispose();
        this.AdditionalInfo = additionalInfo.Count > 0
            ? JsonDocument.Parse(JsonSerializer.Serialize(additionalInfo))
            : null;

        return this;
    }

    public SuDocumentTemplate SetPrincipleApprovalRentalTemplateCode(
        bool? isWinnerPrincipleApprovalRental,
        string? principleApprovalRentalTemplateCode)
    {
        var additionalInfo = this.AdditionalInfo != null
            ? JsonDocumentToDictionary(this.AdditionalInfo)
            : new Dictionary<string, object>();

        if (string.IsNullOrEmpty(principleApprovalRentalTemplateCode))
        {
            additionalInfo.Remove(nameof(this.PrincipleApprovalRentalTemplateCode));
        }
        else
        {
            additionalInfo[nameof(this.PrincipleApprovalRentalTemplateCode)] = principleApprovalRentalTemplateCode;
        }

        if (isWinnerPrincipleApprovalRental is null)
        {
            additionalInfo.Remove(nameof(this.IsWinnerPrincipleApprovalRental));
        }
        else
        {
            additionalInfo[nameof(this.IsWinnerPrincipleApprovalRental)] = isWinnerPrincipleApprovalRental;
        }

        this.AdditionalInfo?.Dispose();
        this.AdditionalInfo = additionalInfo.Count > 0
            ? JsonDocument.Parse(JsonSerializer.Serialize(additionalInfo))
            : null;

        return this;
    }

    public Unit UpdatePreviewPfdFile(FileId previewPfdFileId, string previewPfdFileName)
    {
        this.PreviewPfdFileName = previewPfdFileName;
        this.PreviewPfdFileId = previewPfdFileId;

        return unit;
    }

    public Unit UpdateFileId(FileId fileId)
    {
        this.FileId = fileId;

        return unit;
    }

    private bool disposed = false;

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                this.AdditionalInfo?.Dispose();
            }

            this.disposed = true;
        }
    }
}