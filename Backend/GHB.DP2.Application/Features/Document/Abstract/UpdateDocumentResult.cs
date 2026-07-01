namespace GHB.DP2.Application.Features.Document.Abstract;

public abstract record UpdateDocumentResult
{
    private UpdateDocumentResult()
    {
    }

    public sealed record Success : UpdateDocumentResult;

    public sealed record Failed(string? Reason) : UpdateDocumentResult;
}