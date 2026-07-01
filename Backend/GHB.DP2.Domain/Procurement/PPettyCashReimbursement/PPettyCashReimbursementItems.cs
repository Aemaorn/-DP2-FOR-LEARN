namespace GHB.DP2.Domain.Procurement.PPettyCashReimbursement;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPettyCash;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPettyCashReimbursementItemId
{
    public static PPettyCashReimbursementItemId New() => From(Guid.CreateVersion7());
}

public class PPettyCashReimbursementItems : AuditableEntity<PPettyCashReimbursementItemId>
{
    public override PPettyCashReimbursementItemId Id { get; init; }

    public int Sequence { get; set; }

    public virtual PPettyCashReimbursement PettyCashReimbursement { get; init; }

    public virtual PPettyCashGLAccount PettyCashGlAccount { get; set; }

    public static PPettyCashReimbursementItems Create()
    {
        return new PPettyCashReimbursementItems
        {
            Id = PPettyCashReimbursementItemId.New(),
        };
    }

    public PPettyCashReimbursementItems SetValue(
        int sequence,
        PPettyCashGLAccount pettyCashGlAccount)
    {
        this.Sequence = sequence;
        this.PettyCashGlAccount = pettyCashGlAccount;

        return this;
    }
}