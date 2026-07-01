namespace GHB.DP2.Application.EventHandlers.AnnouncementInfo;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class Section60
{
    public const string Plan = "An010";
    public const string MedianPrice = "An005";
    public const string Tor = "An006";
    public const string Invite = "An003";
    public const string Winner = "An007";
    public const string ReportPlan = "An004"; // TODO: Fix the name of this thai name is รายงานขอซื้อขอจ้าง i don't know eng name
    public const string Other = "An009";
}

public class Section80
{
    public const string Plan = "An019";
    public const string MedianPrice = "An015";
    public const string Tor = "An016";
    public const string Invite = "An013";
    public const string Winner = "An017";
    public const string ReportPlan = "An014"; // TODO: Fix the name of this thai name is รายงานขอซื้อขอจ้าง i don't know eng name
    public const string Other = "An018";
}

public class AnnouncementData
{
    public string AnnouncementName { get; init; }

    public DateTimeOffset AnnouncementDate { get; init; }

    public decimal FinancialAmount { get; init; }

    public string? Annotation { get; init; }

    public string SectionId { get; init; }

    public Stream? Document { get; init; }

    public DateTimeOffset? ExpectedDate { get; init; }

    public decimal? ReferencePrice { get; init; }

    public DateTimeOffset? PublicHearingDateStart { get; init; }

    public DateTimeOffset? PublicHearingDateEnd { get; init; }

    public string? Text5 { get; init; }

    public string? Text6 { get; init; }

    public string? Text7 { get; init; }

    public string? Text8 { get; init; }

    public bool IsDp { get; init; }

    public static AnnouncementData Create(
        string announcementName,
        DateTimeOffset announcementDate,
        decimal financialAmount,
        string? annotation,
        string sectionId,
        Stream? document)
    {
        return new AnnouncementData()
        {
            AnnouncementName = announcementName,
            AnnouncementDate = announcementDate,
            FinancialAmount = financialAmount,
            Annotation = annotation,
            SectionId = sectionId,
            Document = document,
        };
    }

    public Task PublishEvent(CancellationToken ct)
    {
        var info = new AnnouncementDataDto(
            this.AnnouncementName,
            this.AnnouncementDate,
            this.FinancialAmount,
            this.Annotation,
            this.SectionId,
            this.Document);

        return new SaveAnnouncementInfo(info)
            .PublishAsync(Mode.WaitForAll, ct);
    }
}

public record AnnouncementDataDto(
    string AnnouncementName,
    DateTimeOffset AnnouncementDate,
    decimal FinancialAmount,
    string? Annotation,
    string SectionId,
    Stream? Document,
    DateTimeOffset? ExpectedDate = default,
    decimal? ReferencePrice = default,
    DateTimeOffset? PublicHearingDateStart = default,
    DateTimeOffset? PublicHearingDateEnd = default,
    string? Text5 = default,
    string? Text6 = default,
    string? Text7 = default,
    string? Text8 = default,
    bool IsDp = false);

public record SaveAnnouncementInfo(AnnouncementDataDto Data) : IEvent;

public class SaveAnnouncementInfoHandler : IEventHandler<SaveAnnouncementInfo>
{
    private readonly IConfiguration configuration;
    private readonly ILogger<SaveAnnouncementInfoHandler> logger;
    private readonly HttpClient httpClient;

    public SaveAnnouncementInfoHandler(
        IConfiguration configuration,
        ILogger<SaveAnnouncementInfoHandler> logger,
        HttpClient httpClient)
    {
        this.configuration = configuration;
        this.logger = logger;
        this.httpClient = httpClient;
    }

    public async Task HandleAsync(SaveAnnouncementInfo eventModel, CancellationToken ct)
    {
        var ghb1ApiUrl = this.configuration["ApiUrl:Ghb1Url"];

        if (string.IsNullOrEmpty(ghb1ApiUrl))
        {
            this.logger.LogWarning("GHB1 API URL is not configured");

            return;
        }

        this.logger.LogInformation($"Send create request to: {ghb1ApiUrl} with AnnouncementName : {eventModel.Data.AnnouncementName} and sectionId : {eventModel.Data.SectionId}");

        try
        {
            using var formData = new MultipartFormDataContent();

            formData.Add(new StringContent(eventModel.Data.AnnouncementName), "AnnouncementName");
            formData.Add(new StringContent(eventModel.Data.AnnouncementDate.ToString("O")), "AnnouncementDate");
            formData.Add(new StringContent(eventModel.Data.FinancialAmount.ToString()), "FinancialAmount");
            formData.Add(new StringContent(eventModel.Data.SectionId.ToLower()), "SectionId");
            formData.Add(new StringContent(eventModel.Data.IsDp.ToString()), "IsDp");

            if (!string.IsNullOrEmpty(eventModel.Data.Annotation))
            {
                formData.Add(new StringContent(eventModel.Data.Annotation), "Annotation");
            }

            if (eventModel.Data.ExpectedDate.HasValue)
            {
                formData.Add(new StringContent(eventModel.Data.ExpectedDate.Value.ToString("O")), "ExpectedDate");
            }

            if (eventModel.Data.ReferencePrice.HasValue)
            {
                formData.Add(new StringContent(eventModel.Data.ReferencePrice.Value.ToString()), "ReferencePrice");
            }

            if (eventModel.Data.PublicHearingDateStart.HasValue)
            {
                formData.Add(new StringContent(eventModel.Data.PublicHearingDateStart.Value.ToString("O")), "PublicHearingDateStart");
            }

            if (eventModel.Data.PublicHearingDateEnd.HasValue)
            {
                formData.Add(new StringContent(eventModel.Data.PublicHearingDateEnd.Value.ToString("O")), "PublicHearingDateEnd");
            }

            if (!string.IsNullOrEmpty(eventModel.Data.Text5))
            {
                formData.Add(new StringContent(eventModel.Data.Text5), "Text5");
            }

            if (!string.IsNullOrEmpty(eventModel.Data.Text6))
            {
                formData.Add(new StringContent(eventModel.Data.Text6), "Text6");
            }

            if (!string.IsNullOrEmpty(eventModel.Data.Text7))
            {
                formData.Add(new StringContent(eventModel.Data.Text7), "Text7");
            }

            if (!string.IsNullOrEmpty(eventModel.Data.Text8))
            {
                formData.Add(new StringContent(eventModel.Data.Text8), "Text8");
            }

            if (eventModel.Data.Document != null)
            {
                formData.Add(new StreamContent(eventModel.Data.Document!), "Document", "document");
            }

            var apiPath = string.Concat(ghb1ApiUrl, "/announcementinfo/file/v2");
            var response = await this.httpClient.PostAsync(apiPath, formData, ct);

            if (response.IsSuccessStatusCode)
            {
                this.logger.LogInformation($"Successfully sent announcement data to GHB1 API. Status: {response.StatusCode}");
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync(ct);
                this.logger.LogError($"Failed to send announcement data to GHB1 API. Status: {response.StatusCode}, Response: {responseContent}");
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error occurred while sending announcement data to GHB1 API: {ex.Message}");
        }
    }
}