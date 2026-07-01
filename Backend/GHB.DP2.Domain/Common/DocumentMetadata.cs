namespace GHB.DP2.Domain.Common;

using Codehard.FileService.Contracts.ValueObjects;
using LanguageExt;

public enum DocumentState
{
    Open,
    Finalized,
}

public partial class DocumentMetadata : AuditableEntity<FileId>
{
    public override FileId Id { get; init; }

    public DocumentState State { get; private set; }

    public Unit Finalize()
    {
        this.State = DocumentState.Finalized;

        return unit;
    }

    public static DocumentMetadata Create(FileId documentId)
    {
        return new DocumentMetadata()
        {
            Id = documentId,
            State = DocumentState.Open,
        };
    }
}