namespace GHB.DP2.Infrastructure.Services.ChEditor;

public interface IChEditorService
{
    Task<Stream> ConvertToPdf(Stream document, CancellationToken cancellationToken = default);
}

public class ChEditorService : IChEditorService
{
    private readonly HttpClient httpClient;

    public ChEditorService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public Task<Stream> ConvertToPdf(Stream document, CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent();

        var streamContent = new StreamContent(document);
        content.Add(streamContent, "data", "document.docx");

        content.Add(new StringContent("pdf"), "format");
        content.Add(new StringContent("PDF/A-2b"), "PDFVer");

        return this.httpClient.PostAsync("/cool/convert-to", content, cancellationToken)
                   .ContinueWith(
                       t =>
                       {
                           t.Result.EnsureSuccessStatusCode();

                           return t.Result.Content.ReadAsStreamAsync(cancellationToken);
                       },
                       cancellationToken).Unwrap();
    }
}