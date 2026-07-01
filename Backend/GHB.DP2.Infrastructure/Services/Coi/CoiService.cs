namespace GHB.DP2.Infrastructure.Services.Coi;

using System.Net.Http.Json;
using GHB.DP2.Infrastructure.Configurations;

public interface ICoiService
{
    Task<IEnumerable<CoiInfo>> GetCoiAllAsync(
        CancellationToken cancellationToken);

    Task<IEnumerable<CoiInfo>> GetCoiBySsnAsync(
        string ssn,
        CancellationToken cancellationToken);

    Task<IEnumerable<CoiInfo>> GetCoiByNameAsync(
        string name,
        CancellationToken cancellationToken);

    Task<IEnumerable<CoiInfo>> GetCoiByNameSsnAsync(
        string name,
        string ssn,
        CancellationToken cancellationToken);
}

public class CoiService : ICoiService
{
    private readonly HttpClient httpClient;
    private readonly CoiConfiguration configuration;

    public CoiService(HttpClient httpClient, CoiConfiguration configuration)
    {
        this.httpClient = httpClient;
        this.configuration = configuration;
    }

    public async Task<IEnumerable<CoiInfo>> GetCoiAllAsync(CancellationToken cancellationToken)
    {
        var response =
            await this.httpClient.PostAsync(
                this.configuration.GetAllEndpoint,
                null,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new CoiException($"Failed to retrieve COI information. Status code: {response.StatusCode}, Message: {message}");
        }

        var coiInfos = await response.Content.ReadFromJsonAsync<CoiResult>(cancellationToken: cancellationToken);

        return coiInfos?.Data ?? [];
    }

    public async Task<IEnumerable<CoiInfo>> GetCoiBySsnAsync(string ssn, CancellationToken cancellationToken)
    {
        var response =
            await this.httpClient.PostAsJsonAsync(
                this.configuration.GetBySsnEndpoint,
                new { ssn },
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new CoiException($"Failed to retrieve COI information for SSN {ssn}. Status code: {response.StatusCode}, Message: {message}");
        }

        var coiInfos = await response.Content.ReadFromJsonAsync<CoiResult>(cancellationToken: cancellationToken);

        return coiInfos?.Data ?? [];
    }

    public async Task<IEnumerable<CoiInfo>> GetCoiByNameAsync(string name, CancellationToken cancellationToken)
    {
        var response =
            await this.httpClient.PostAsJsonAsync(
                this.configuration.GetByNameEndpoint,
                new { name },
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new CoiException($"Failed to retrieve COI information for Name {name}. Status code: {response.StatusCode}, Message: {message}");
        }

        var coiInfos = await response.Content.ReadFromJsonAsync<CoiResult>(cancellationToken: cancellationToken);

        return coiInfos?.Data ?? [];
    }

    public async Task<IEnumerable<CoiInfo>> GetCoiByNameSsnAsync(string name, string ssn, CancellationToken cancellationToken)
    {
        var response =
            await this.httpClient.PostAsJsonAsync(
                this.configuration.GetByNameSsnEndpoint,
                new { name, ssn },
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new CoiException($"Failed to retrieve COI information for Name {name} and SSN {ssn}. Status code: {response.StatusCode}, Message: {message}");
        }

        var coiInfos = await response.Content.ReadFromJsonAsync<CoiResult>(cancellationToken: cancellationToken);

        return coiInfos?.Data ?? [];
    }
}