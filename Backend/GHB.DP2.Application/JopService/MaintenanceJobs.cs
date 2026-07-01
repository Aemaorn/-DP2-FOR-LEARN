namespace GHB.DP2.Application.JopService;

using GHB.DP2.Application.CommandHandler;
using Hangfire;
using Microsoft.Extensions.Logging;

public class MaintenanceJobs
{
    private readonly ILogger<MaintenanceJobs> logger;

    public MaintenanceJobs(ILogger<MaintenanceJobs> logger)
    {
        this.logger = logger;
    }

    [Queue("default")]
    [AutomaticRetry(Attempts = 0)]
    public async Task SyncErmEmployee()
    {
        this.logger.LogInformation("Starting SyncErmEmployee job...");

        await new SyncErmEmployeeCommand().ExecuteAsync(CancellationToken.None);

        this.logger.LogInformation("Completed SyncErmEmployee job.");
    }

    [Queue("default")]
    [AutomaticRetry(Attempts = 0)]
    public async Task SyncRawEmployee()
    {
        this.logger.LogInformation("Starting SyncRawEmployee job...");

        await new SyncRawEmployeeCommand().ExecuteAsync(CancellationToken.None);

        this.logger.LogInformation("Completed SyncRawEmployee job.");
    }

    [Queue("default")]
    [AutomaticRetry(Attempts = 0)]
    public async Task NotificationWorkGuaranteeReturn()
    {
        this.logger.LogInformation("Starting NotificationWorkGuaranteeReturn job...");

        await new NotificationWorkGuaranteeReturnCommand().ExecuteAsync(CancellationToken.None);

        this.logger.LogInformation("Completed NotificationWorkGuaranteeReturn job.");
    }
}