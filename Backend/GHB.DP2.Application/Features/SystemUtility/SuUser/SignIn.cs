namespace GHB.DP2.Application.Features.SystemUtility.SuUser;

using FastEndpoints.Security;
using GHB.DP2.Application.EventHandlers.SuAuditLog;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Services.Token;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.ActiveDirectory;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using LanguageExt;

public record SignInCommand(
    string Username,
    string Password);

public class SignInCommandValidator : Validator<SignInCommand>
{
    public SignInCommandValidator()
    {
        this.RuleFor(user => user.Username)
            .NotNull()
            .NotEmpty();

        this.RuleFor(user => user.Password)
                    .NotEmpty();
    }
}

public class SignIn : Endpoint<SignInCommand, TokenResponse>
{
    private readonly Dp2DbContext dbContext;
    private readonly IActiveDirectoryService activeDirectoryService;

    public SignIn(
        Dp2DbContext dbContext,
        IActiveDirectoryService activeDirectoryService)
    {
        this.dbContext = dbContext;
        this.activeDirectoryService = activeDirectoryService;
    }

    public override void Configure()
    {
        this.Options(b => b.WithTags("SuUser"));
        this.Post("/user/signin");
        this.AllowFormData(true);
        this.AllowAnonymous();
    }

    private const int MaxFailedAttempts = 5;

    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public override async Task HandleAsync(SignInCommand req, CancellationToken ct)
    {
        var ipAddress = this.HttpContext.TryGetIpAddress();

        var user =
            await this.dbContext.SuUsers
                      .AsNoTracking()
                      .Include(suUser => suUser.Employee)
                      .Where(r => r.IsActive)
                      .Select(s => new
                      {
                          s.Id,
                          FullName = s.Employee.FirstName + " " + s.Employee.LastName,
                          s.Employee.Email,
                      })
                      .WhereIfTrue(
                          req.Username.Contains("@"),
                          u => u.Email.ToUpper() == req.Username.ToUpper())
                      .WhereIfTrue(
                          !req.Username.Contains("@"),
                          u => u.Email.ToUpper().StartsWith(req.Username.ToUpper() + "@"))
                      .AsSplitQuery()
                      .SingleOrDefaultAsync(ct);

        if (user is null)
        {
            await this.activeDirectoryService.PerformDummyValidationAsync(ct);

            await new SaveAuditLogEvent(
                    $"เข้าสู่ระบบล้มเหลว - ไม่พบผู้ใช้งาน: {Truncate(req.Username, 100)}",
                    "เข้าสู่ระบบ",
                    Option<Guid>.None,
                    ipAddress?.ToString() ?? string.Empty)
                .PublishAsync(Mode.WaitForNone, cancellation: ct);

            await this.SendStringAsync("ไม่พบผู้ใช้งานในระบบ", StatusCodes.Status401Unauthorized, cancellation: ct);

            return;
        }

        var userEntity = await this.dbContext.SuUsers
            .Where(u => u.Id == user.Id)
            .SingleAsync(ct);

        if (userEntity.IsLockedOut())
        {
            await this.activeDirectoryService.PerformDummyValidationAsync(ct);

            await new SaveAuditLogEvent(
                    $"เข้าสู่ระบบล้มเหลว - บัญชีถูกล็อค: {Truncate(req.Username, 100)}",
                    "เข้าสู่ระบบ",
                    user.Id.Value,
                    ipAddress?.ToString() ?? string.Empty)
                .PublishAsync(Mode.WaitForNone, cancellation: ct);

            await this.SendStringAsync("บัญชีผู้ใช้งานถูกล็อค กรุณาติดต่อผู้ดูแลระบบ", StatusCodes.Status401Unauthorized, cancellation: ct);

            return;
        }

        var isValid = await this.activeDirectoryService.ValidateAsync(
            user.Email,
            req.Password,
            ct);

        if (!isValid)
        {
            userEntity.RecordFailedLogin(MaxFailedAttempts, LockoutDuration);
            await this.dbContext.SaveChangesAsync(ct);

            var auditMessage = userEntity.IsLockedOut()
                ? $"เข้าสู่ระบบล้มเหลว - บัญชีถูกล็อค หลังจากพยายามเข้าสู่ระบบล้มเหลว {userEntity.FailedLoginAttempts} ครั้ง: {Truncate(req.Username, 100)}"
                : $"เข้าสู่ระบบล้มเหลว - รหัสผ่านไม่ถูกต้อง: {Truncate(req.Username, 100)}";

            await new SaveAuditLogEvent(
                    auditMessage,
                    "เข้าสู่ระบบ",
                    user.Id.Value,
                    ipAddress?.ToString() ?? string.Empty)
                .PublishAsync(Mode.WaitForNone, cancellation: ct);

            var errorMessage = userEntity.IsLockedOut()
                ? $"บัญชีผู้ใช้งานถูกล็อค หลังจากพยายามเข้าสู่ระบบล้มเหลว {userEntity.FailedLoginAttempts} ครั้ง"
                : "ชื่อผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง";

            await this.SendStringAsync(errorMessage, StatusCodes.Status401Unauthorized, cancellation: ct);

            return;
        }

        userEntity.RecordSuccessfulLogin();
        await this.dbContext.SaveChangesAsync(ct);

        await new SaveAuditLogEvent(
                "เข้าสู่ระบบ",
                "เข้าสู่ระบบ",
                user.Id.Value,
                ipAddress?.ToString() ?? string.Empty)
            .PublishAsync(Mode.WaitForNone, cancellation: ct);

        this.Response = await this.CreateTokenWith<TokenService>(
            user.Id.Value.ToString(),
            p => p.SetClaims(
                user.Id.Value,
                user.FullName));
    }

    private static string Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) ? string.Empty
        : value.Length <= maxLength ? value : value[..maxLength];
}