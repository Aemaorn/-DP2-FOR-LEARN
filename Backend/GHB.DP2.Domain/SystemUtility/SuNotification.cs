namespace GHB.DP2.Domain.SystemUtility;

using Codehard.Common.DomainModel;
using GHB.DP2.Domain.SystemUtility.Event;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct NotificationId
{
    public static NotificationId New() => From(Guid.CreateVersion7());
}

public enum NotificationProgram
{
    /// <summary>
    /// ไม่มีโปรแกรม
    /// </summary>
    None = 0,

    /// <summary>
    /// แผนงาน (Plan)
    /// </summary>
    Plan = 1,

    /// <summary>
    /// สัญญา (Contract Agreement)
    /// </summary>
    ContractAgreement = 2,

    /// <summary>
    /// การจัดซื้อจัดจ้าง (Procurement)
    /// </summary>
    Procurement = 3,

    /// <summary>
    /// ระบบสาธารณูปโภค (System Utility)
    /// </summary>
    SystemUtility = 4,

    /// <summary>
    /// การจัดการผู้ใช้ (User Management)
    /// </summary>
    UserManagement = 5,

    /// <summary>
    /// การจัดการสัญญา (Contract Management)
    /// </summary>
    ContractManagement = 6,

    ContractAmendment = 7,

    /// <summary>
    /// เบิกจ่าย (สำหรับบัญชี)
    /// </summary>
    ExpenseDisbursement = 8,

    PlanAnnouncement = 9,

    BranchSpaceRent = 10,

    Report = 11,
}

public class SuNotification : Entity<NotificationId>
{
    public override NotificationId Id { get; init; }

    public UserId UserId { get; init; }

    public string Title { get; init; }

    public string Message { get; init; }

    public IDictionary<string, string>? AdditionalData { get; private set; }

    public virtual SuUser User { get; init; }

    public Guid? ReferenceId
    {
        get
        {
            if (this.AdditionalData != null &&
                this.AdditionalData.TryGetValue(nameof(this.ReferenceId), out var value) &&
                Guid.TryParse(value, out var referenceId))
            {
                return referenceId;
            }

            return null;
        }
    }

    public NotificationProgram Program
    {
        get
        {
            if (this.AdditionalData != null &&
                this.AdditionalData.TryGetValue(nameof(this.Program), out var value) &&
                Enum.TryParse<NotificationProgram>(value, out var program))
            {
                return program;
            }

            return NotificationProgram.None;
        }
    }

    public string? LinkUrl
    {
        get
        {
            if (this.AdditionalData != null && this.AdditionalData.TryGetValue(nameof(this.LinkUrl), out var value))
            {
                return value;
            }

            return null;
        }
    }

    public string? LinkButtonText
    {
        get
        {
            if (this.AdditionalData != null && this.AdditionalData.TryGetValue(nameof(this.LinkButtonText), out var value))
            {
                return value;
            }

            return null;
        }
    }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ReadAt { get; private set; }

    public SuNotification MarkRead()
    {
        this.ReadAt = DateTimeOffset.UtcNow;

        return this;
    }

    public SuNotification SetReferenceId(Guid referenceId)
    {
        this.AdditionalData ??= new Dictionary<string, string>();

        if (this.AdditionalData.TryGetValue(nameof(this.ReferenceId), out _))
        {
            this.AdditionalData[nameof(this.ReferenceId)] = referenceId.ToString();
        }
        else
        {
            this.AdditionalData.Add(nameof(this.ReferenceId), referenceId.ToString());
        }

        return this;
    }

    public SuNotification SetProgram(NotificationProgram program)
    {
        this.AdditionalData ??= new Dictionary<string, string>();

        if (this.AdditionalData.TryGetValue(nameof(this.Program), out _))
        {
            this.AdditionalData[nameof(this.Program)] = program.ToString();
        }
        else
        {
            this.AdditionalData.Add(nameof(this.Program), program.ToString());
        }

        return this;
    }

    public SuNotification SetLink(string link, string buttonText)
    {
        this.AdditionalData ??= new Dictionary<string, string>();

        if (this.AdditionalData.TryGetValue(nameof(this.LinkUrl), out _))
        {
            this.AdditionalData[nameof(this.LinkUrl)] = link;
        }
        else
        {
            this.AdditionalData.Add(nameof(this.LinkUrl), link);
        }

        if (this.AdditionalData.TryGetValue(nameof(this.LinkButtonText), out _))
        {
            this.AdditionalData[nameof(this.LinkButtonText)] = buttonText;
        }
        else
        {
            this.AdditionalData.Add(nameof(this.LinkButtonText), buttonText);
        }

        return this;
    }

    public static SuNotification Create(
        UserId userId,
        string title,
        string message)
    {
        var notification = new SuNotification
        {
            Id = NotificationId.New(),
            UserId = userId,
            Title = title,
            Message = message,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Add event sends email
        notification.AddDomainEvent(SendEmailEvent.Create(notification.Id));

        return notification;
    }
}