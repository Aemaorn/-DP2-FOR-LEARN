namespace GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPurchaseOrderApprovalEntrepreneursId
{
    public static PPurchaseOrderApprovalEntrepreneursId New() => From(Guid.CreateVersion7());
}

public partial class PPurchaseOrderApprovalEntrepreneurs : AuditableEntity<PPurchaseOrderApprovalEntrepreneursId>, IHasSoftDelete
{
    public override PPurchaseOrderApprovalEntrepreneursId Id { get; init; }

    public bool EmailSend { get; private set; }

    public int Sequence { get; private set; }

    public virtual PPurchaseOrderApproval PPurchaseOrderApproval { get; init; }

    public virtual SuVendor Vendor { get; private set; }

    public static PPurchaseOrderApprovalEntrepreneurs Create(
        PPurchaseOrderApproval purchaseOrderApproval,
        SuVendor vendor,
        int sequence,
        bool emailSend)
    {
        return new PPurchaseOrderApprovalEntrepreneurs
        {
            Id = PPurchaseOrderApprovalEntrepreneursId.New(),
            Vendor = vendor,
            Sequence = sequence,
            EmailSend = emailSend,
            PPurchaseOrderApproval = purchaseOrderApproval,
        };
    }

    public PPurchaseOrderApprovalEntrepreneurs Update(
    int sequence,
    bool emailSend)
    {
        this.Sequence = sequence;
        this.EmailSend = emailSend;

        return this;
    }
}
