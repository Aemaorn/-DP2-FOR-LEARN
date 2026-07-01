namespace GHB.DP2.Application.Features.Document.Abstract;

public abstract record RejectDocumentChangesResult
{
    private RejectDocumentChangesResult()
    {
    }

    public sealed record Success(byte[] Contents) : RejectDocumentChangesResult;

    public sealed record Failure(Exception Exception) : RejectDocumentChangesResult;
}