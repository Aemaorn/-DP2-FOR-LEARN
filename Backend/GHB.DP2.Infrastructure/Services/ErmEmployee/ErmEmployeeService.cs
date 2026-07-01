namespace GHB.DP2.Infrastructure.Services.ErmEmployee;

using System.Net.Http.Json;

public interface IErmEmployeeService
{
    Task<IEnumerable<ErmEmployeeResult>> GetErmEmployeesAsync(CancellationToken cancellationToken = default);
}

public class ErmEmployeeService : IErmEmployeeService
{
    private readonly HttpClient httpClient;

    public ErmEmployeeService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<IEnumerable<ErmEmployeeResult>> GetErmEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var response =
            await this.httpClient.PostAsync(
                "/GHBHRInternalAPI/ERM/All",
                null,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new Exception($"Failed to retrieve ERM employee information. Status code: {response.StatusCode}, Message: {message}");
        }

        var ermEmployees = await response.Content.ReadFromJsonAsync<IEnumerable<ErmEmployeeResult>>(cancellationToken: cancellationToken);

        return ermEmployees ?? [];
    }
}