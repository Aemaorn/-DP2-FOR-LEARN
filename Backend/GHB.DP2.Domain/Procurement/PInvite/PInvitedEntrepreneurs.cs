namespace GHB.DP2.Domain.Procurement.PInvite;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PInvitedEntrepreneursId
{
    public static PInvitedEntrepreneursId New() => From(Guid.CreateVersion7());
}

public enum EntrepreneursCheckType
{
    Coi,
    Watchlist,
    Egp,
}

public partial class PInvitedEntrepreneurs :
    AuditableEntity<PInvitedEntrepreneursId>,
    IHasSoftDelete
{
    public override PInvitedEntrepreneursId Id { get; init; }

    public bool EmailSend { get; private set; }

    public int Sequence { get; private set; }

    public bool WatchlistResult { get; private set; }

    public string? WatchlistResultRemark { get; private set; }

    public DateTimeOffset? WatchlistResultAt { get; private set; }

    public bool CoiResult { get; private set; }

    public string? CoiResultRemark { get; private set; }

    public DateTimeOffset? CoiResultAt { get; private set; }

    public bool EgpResult { get; private set; }

    public string? EgpResultRemark { get; private set; }

    public DateTimeOffset? EgpResultAt { get; private set; }

    public string? Email { get; private set; }

    public string? EmailTemplate { get; private set; }

    public virtual PInvite Invite { get; private set; }

    public virtual SuVendor Vendor { get; private set; }

    public virtual IReadOnlyCollection<PInvitedEntrepreneurShareholders> InvitedEntrepreneurShareholders { get; private set; }

    public virtual IReadOnlyCollection<PInvitedEntrepreneurChecker> InvitedEntrepreneurCheckers { get; private set; }

    public virtual IReadOnlyCollection<PInvitedEntrepreneursAttachments> Attachments { get; private set; }

    public virtual IReadOnlyCollection<PInvitedEntrepreneursDocumentHistory> DocumentHistories { get; private set; }

    public PInvitedEntrepreneursDocumentHistory? LastedDocument =>
        this.DocumentHistories
            .OrderVersions()
            .FirstOrDefault();

    public PInvitedEntrepreneursDocumentHistory? LastedDraftDocument =>
        this.DocumentHistories
            .Where(x => x.StatusState == PInviteStatus.Draft)
            .Where(x => !x.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public PInvitedEntrepreneursDocumentHistory? LastedWaitingApprovalDocument =>
        this.DocumentHistories
            .Where(x => x.StatusState == PInviteStatus.WaitingApproval)
            .OrderVersions()
            .FirstOrDefault();

    public PInvitedEntrepreneurs SetWatchlist(bool result, string? remark, DateTimeOffset? at)
    {
        this.WatchlistResult = result;
        this.WatchlistResultRemark = remark;
        this.WatchlistResultAt = at;

        return this;
    }

    public PInvitedEntrepreneurs SetCoi(bool result, string? remark, DateTimeOffset? at)
    {
        this.CoiResult = result;
        this.CoiResultRemark = remark;
        this.CoiResultAt = at;

        return this;
    }

    public PInvitedEntrepreneurs SetEgp(bool result, string? remark, DateTimeOffset? at)
    {
        this.EgpResult = result;
        this.EgpResultRemark = remark;
        this.EgpResultAt = at;

        return this;
    }

    public PInvitedEntrepreneurs MarkEmailAsSent()
    {
        this.EmailSend = true;

        return this;
    }

    public static PInvitedEntrepreneurs Create(
        PInvite invite,
        SuVendor vendor,
        int sequence,
        bool emailSend)
    {
        return new PInvitedEntrepreneurs
        {
            Id = PInvitedEntrepreneursId.New(),
            Invite = invite,
            Vendor = vendor,
            Sequence = sequence,
            EmailSend = emailSend,
            InvitedEntrepreneurCheckers = [],
            InvitedEntrepreneurShareholders = [],
            Attachments = [],
            DocumentHistories = [],
        };
    }

    public PInvitedEntrepreneurs Update(
        int sequence,
        bool emailSend)
    {
        this.Sequence = sequence;
        this.EmailSend = emailSend;

        return this;
    }

    public PInvitedEntrepreneurs SetSequence(
        int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public PInvitedEntrepreneurs AddInvitedEntrepreneurShareholderList(List<PInvitedEntrepreneurShareholders> pInvitedEntrepreneurShareholder)
    {
        this.InvitedEntrepreneurShareholders = pInvitedEntrepreneurShareholder;

        return this;
    }

    public PInvitedEntrepreneurs AddInvitedEntrepreneurShareholder(PInvitedEntrepreneurShareholders pInvitedEntrepreneurShareholder)
    {
        var shareholdersList = this.InvitedEntrepreneurShareholders?.ToList() ?? new List<PInvitedEntrepreneurShareholders>();
        shareholdersList.Add(pInvitedEntrepreneurShareholder);

        this.InvitedEntrepreneurShareholders = shareholdersList;

        return this;
    }

    public PInvitedEntrepreneurs UpdatePInviteEntrepreneursShareholder(PInvitedEntrepreneurShareholders shareholders)
    {
        var invitedEntrepreneursShareholdersList = this.InvitedEntrepreneurShareholders?.ToList() ?? new List<PInvitedEntrepreneurShareholders>();
        var idx = invitedEntrepreneursShareholdersList.FindIndex(a => a.Id == shareholders.Id);

        if (idx >= 0)
        {
            invitedEntrepreneursShareholdersList[idx] = shareholders;
            this.InvitedEntrepreneurShareholders = invitedEntrepreneursShareholdersList;
        }

        return this;
    }

    public PInvitedEntrepreneurs RemovePInviteEntrepreneursShareholder(PInvitedEntrepreneurShareholdersId shareholdersId)
    {
        var entrepreneurShareholdersList = this.InvitedEntrepreneurShareholders?.ToList() ?? new List<PInvitedEntrepreneurShareholders>();
        entrepreneurShareholdersList.RemoveAll(a => a.Id == shareholdersId);
        this.InvitedEntrepreneurShareholders = entrepreneurShareholdersList;

        return this;
    }

    public PInvitedEntrepreneurs AddChecker(
        QualificationType checkType,
        QualificationResult result,
        DateTimeOffset resultAt,
        string? remark)
    {
        var checkerList = this.InvitedEntrepreneurCheckers?.ToList() ?? [];

        var checkerExists = resultAt == checkerList.Where(x => x.CheckType == checkType).MaxBy(x => x.ResultAt)?.ResultAt;

        if (checkerExists)
        {
            return this;
        }

        var checker =
            (PInvitedEntrepreneurChecker)PInvitedEntrepreneurChecker
                .Create(
                    checkType,
                    result,
                    resultAt,
                    remark);

        checkerList.Add(checker);

        this.InvitedEntrepreneurCheckers = checkerList;

        return this;
    }

    public PInvitedEntrepreneurs RemoveAttachment(PInvitedEntrepreneursAttachments attachment)
    {
        var list = this.Attachments.ToHashSet();
        list.Remove(attachment);
        this.Attachments = list;

        return this;
    }

    public PInvitedEntrepreneurs AddAttachment(PInvitedEntrepreneursAttachments attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment), "Attachment cannot be null.");
        }

        if (this.Attachments.Contains(attachment))
        {
            throw new InvalidOperationException("Attachment already exists in the plan.");
        }

        var attachments = this.Attachments.ToHashSet();

        attachments.Add(attachment);

        this.Attachments = attachments;

        return this;
    }

    public PInvitedEntrepreneurs SetSendMailInfo(string email, string emailTemplate)
    {
        this.Email = email;
        this.EmailTemplate = emailTemplate;

        return this;
    }

    public Unit AddDocumentHistory(PInvitedEntrepreneursDocumentHistory documentHistory)
    {
        var histories = this.DocumentHistories?.ToList() ?? [];

        histories.Add(documentHistory);

        this.DocumentHistories = histories;

        return Unit.Default;
    }

    public Unit MarkDocumentAsReplaced(PInvitedEntrepreneursDocumentHistory documentHistory)
    {
        var histories = this.DocumentHistories?.ToList() ?? [];

        var existingHistory = histories.FirstOrDefault(h => h.Id == documentHistory.Id);

        if (existingHistory != null)
        {
            var index = histories.IndexOf(existingHistory);
            histories[index] = PInvitedEntrepreneursDocumentHistory.Create(
                this.Id,
                existingHistory.StatusState,
                existingHistory.Version,
                existingHistory.FileId,
                isReplace: true);
        }

        this.DocumentHistories = histories;

        return Unit.Default;
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PInvitedEntrepreneursAttachmentId
{
    public static PInvitedEntrepreneursAttachmentId New() => From(Guid.CreateVersion7());
}

public class PInvitedEntrepreneursAttachments : AuditableEntity<PInvitedEntrepreneursAttachmentId>
{
    public override PInvitedEntrepreneursAttachmentId Id { get; init; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public int Sequence { get; private set; }

    public static PInvitedEntrepreneursAttachments Create(
        FileId fileId,
        string fileName,
        int sequence)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        return new PInvitedEntrepreneursAttachments
        {
            Id = PInvitedEntrepreneursAttachmentId.New(),
            Sequence = sequence,
            FileId = fileId,
            FileName = fileName,
        };
    }

    public PInvitedEntrepreneursAttachments SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public PInvitedEntrepreneursAttachments Clone()
    {
        return new PInvitedEntrepreneursAttachments
        {
            Id = PInvitedEntrepreneursAttachmentId.New(),
            Sequence = this.Sequence,
            FileId = this.FileId,
            FileName = this.FileName,
        };
    }
}