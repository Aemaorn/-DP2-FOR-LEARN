namespace GHB.DP2.Application.EventHandlers.SenEmail;

using GHB.DP2.Application.EventHandlers.SenEmail.Templateds;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Domain.SystemUtility.Event;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.Email;
using GreatFriends.ThaiBahtText;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class SendEmailHandler : IEventHandler<SendEmailEvent>
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger logger;

    public SendEmailHandler(
        ILogger<SendEmailHandler> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        this.logger = logger;
        this.serviceScopeFactory = serviceScopeFactory;
    }

    public async Task HandleAsync(SendEmailEvent eventModel, CancellationToken ct)
    {
        this.logger.LogInformation(
            "Handling SendEmailEvent for NotificationId: {NotificationId}",
            eventModel.Id);

        await using var scope = this.serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Dp2DbContext>();

        var notification =
            await dbContext.SuNotifications
                           .Include(n => n.User)
                           .SingleOrDefaultAsync(
                               n => n.Id == eventModel.Id,
                               cancellationToken: ct);

        if (notification == null)
        {
            this.logger.LogWarning(
                "Notification with Id: {NotificationId} not found.",
                eventModel.Id);

            return;
        }

        var sendEmailAsync = notification.Program switch
        {
            NotificationProgram.Plan => this.SendEmailPlanAsync(notification, ct),
            NotificationProgram.Procurement => this.SendEmailAsync(notification, ct),
            NotificationProgram.ContractAgreement => this.SendEmailAsync(notification, ct),
            NotificationProgram.ContractManagement => this.SendEmailAsync(notification, ct),
            NotificationProgram.PlanAnnouncement => this.SendEmailPlanAnnouncementAsync(notification, ct),
            NotificationProgram.BranchSpaceRent => this.SendEmailAsync(notification, ct),
            _ => Task.CompletedTask,
        };

        await sendEmailAsync;

        this.logger.LogInformation(
            "Fetching notification with Id: {NotificationId}",
            eventModel.Id);
    }

    private async Task SendEmailAsync(SuNotification notification, CancellationToken ct)
    {
        await using var scope = this.serviceScopeFactory.CreateAsyncScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var notificationTemplate =
            new NotificationTemplate
            {
                Subject = notification.Title,
                Message = notification.Message,
                Name = notification.User.Employee.View?.FullName ?? notification.User.Employee.FullName,
                MessageClick = notification.LinkButtonText ?? "View Details",
                Url = notification.LinkUrl ?? string.Empty,
            };

        await emailService.SendHtmlAsync(
            notification.User.Employee.Email,
            notificationTemplate.Subject,
            notificationTemplate.TransformText(),
            ct);

        this.logger.LogInformation(
            "Sending email to UserId: {UserId} with Title: {Title}",
            notification.UserId,
            notification.Title);
    }

    private async Task SendEmailPlanAsync(SuNotification notification, CancellationToken ct)
    {
        await using var scope = this.serviceScopeFactory.CreateAsyncScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Dp2DbContext>();

        if (notification.ReferenceId is null)
        {
            this.logger.LogError(
                "Notification with Id: {NotificationId} has no ReferenceId.",
                notification.Id);

            return;
        }

        var planId = PlanId.From(notification.ReferenceId.Value);

        var plan = await dbContext.Plans
                                  .Include(p => p.SupplyMethod)
                                  .SingleOrDefaultAsync(
                                      p => p.Id == planId,
                                      cancellationToken: ct);

        if (plan == null)
        {
            this.logger.LogError(
                "Plan with Id: {PlanId} not found for NotificationId: {NotificationId}.",
                planId,
                notification.Id);

            return;
        }

        var notificationTemplate =
            new PlanNotificationTemplate
            {
                Subject = notification.Title,
                Description = notification.Message,
                PlanNumber = plan.PlanNumber.Value,
                SupplyMethodValue = plan.SupplyMethod.Label,
                PlanName = plan.Name,
                Budget = plan.Budget.ThaiBahtText(),
                Status = plan.Status.GetDescription(),
                Remark = plan.Remark ?? string.Empty,
                MessageClick = notification.LinkButtonText ?? "View Details",
                Url = notification.LinkUrl ?? string.Empty,
                Dear = notification.User.Employee.View?.FullName ?? notification.User.Employee.FullName,
            };

        await emailService.SendHtmlAsync(
            notification.User.Employee.Email,
            notificationTemplate.Subject,
            notificationTemplate.TransformText(),
            ct);

        this.logger.LogInformation(
            "Sending plan email to UserId: {UserId} with Title: {Title}",
            notification.UserId,
            notification.Title);
    }

    private async Task SendEmailPlanAnnouncementAsync(SuNotification notification, CancellationToken ct)
    {
        await using var scope = this.serviceScopeFactory.CreateAsyncScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Dp2DbContext>();

        if (notification.ReferenceId is null)
        {
            this.logger.LogError(
                "Notification with Id: {NotificationId} has no ReferenceId.",
                notification.Id);

            return;
        }

        var planAnnouncementId = PlanAnnouncementId.From(notification.ReferenceId.Value);

        var planAnnouncement = await dbContext.PlanAnnouncements
                                              .Include(planAnnouncement => planAnnouncement.SupplyMethodInfo)
                                              .Include(planAnnouncement => planAnnouncement.AnnouncementSelectedInformations)
                                              .ThenInclude(planAnnouncementSelected => planAnnouncementSelected.Plan)
                                              .SingleOrDefaultAsync(
                                                  p => p.Id == planAnnouncementId,
                                                  cancellationToken: ct);

        if (planAnnouncement == null)
        {
            this.logger.LogError(
                "planAnnouncement with Id: {PlanId} not found for NotificationId: {NotificationId}.",
                planAnnouncementId,
                notification.Id);

            return;
        }

        var notificationTemplate =
            new PlanAnnouncementTemplate
            {
                Subject = notification.Title,
                Description = notification.Message,
                PlanNumber = planAnnouncement.PlanAnnouncementNumber.Value,
                SupplyMethodValue = planAnnouncement.SupplyMethodInfo.Label,
                PlanName = planAnnouncement.AnnouncementTitle ?? string.Empty,
                Budget = planAnnouncement.AnnouncementSelectedInformations.Sum(x => x.Plan.Budget).ThaiBahtText(),
                Status = planAnnouncement.Status.GetDescription(),
                MessageClick = notification.LinkButtonText ?? "View Details",
                Url = notification.LinkUrl ?? string.Empty,
                Dear = notification.User.Employee.View?.FullName ?? notification.User.Employee.FullName,
                BudgetYear = planAnnouncement.Year.ToString(),
                PlanCount = planAnnouncement.AnnouncementSelectedInformations.Count.ToString(),
            };

        await emailService.SendHtmlAsync(
            notification.User.Employee.Email,
            notificationTemplate.Subject,
            notificationTemplate.TransformText(),
            ct);

        this.logger.LogInformation(
            "Sending plan email to UserId: {UserId} with Title: {Title}",
            notification.UserId,
            notification.Title);
    }
}