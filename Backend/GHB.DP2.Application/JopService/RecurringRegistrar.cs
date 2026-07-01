namespace GHB.DP2.Application.JopService;

using System.Linq.Expressions;
using System.Reflection;
using Hangfire;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class RecurringRegistrar
{
    private readonly IRecurringJobManager recurringJobManager;
    private readonly JobStorage jobStorage;
    private readonly IOptionsMonitor<List<RecurringJobDef>> defs;
    private readonly TimeZoneInfo timeZoneInfo;
    private readonly ILogger<RecurringJobDef> logger;

    public RecurringRegistrar(
        IRecurringJobManager recurringJobManager,
        JobStorage jobStorage,
        IOptionsMonitor<List<RecurringJobDef>> defs,
        TimeZoneHelper timeZoneHelper,
        ILogger<RecurringJobDef> logger)
    {
        this.recurringJobManager = recurringJobManager;
        this.jobStorage = jobStorage;
        this.defs = defs;
        this.timeZoneInfo = timeZoneHelper.AsiaBangkok;
        this.logger = logger;
    }

    public void SyncAtStartup() => this.SafeSync();

    private void SafeSync()
    {
        try
        {
            this.Sync();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to sync recurring jobs");
        }
    }

    private void Sync()
    {
        var wanted =
            this.defs.CurrentValue
                .ToHashSet();

        using var conn = this.jobStorage.GetConnection();
        var existing =
            conn.GetRecurringJobs()
                .Select(j => j.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var def in wanted.Where(d => d.Enabled))
        {
            var expr = BuildCallExpr(def);

            var options = new RecurringJobOptions
            {
                TimeZone = this.timeZoneInfo,
            };

            var queue = string.IsNullOrWhiteSpace(def.Queue)
                ? EnqueuedState.DefaultQueue
                : def.Queue;

            this.recurringJobManager.AddOrUpdate(
                recurringJobId: def.Id,
                queue: queue,
                methodCall: expr,
                cronExpression: def.Cron,
                options: options);

            this.logger.LogInformation("Upserted recurring job {Id} -> {Method} ({Cron})", def.Id, def.Method, def.Cron);

            existing.Remove(def.Id);
        }

        var disabledIds = wanted.Where(d => !d.Enabled).Select(d => d.Id);

        foreach (var id in existing.Concat(disabledIds).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            this.recurringJobManager.RemoveIfExists(id);

            this.logger.LogInformation("Removed recurring job {Id}", id);
        }
    }

    private static Expression<Func<MaintenanceJobs, Task>> BuildCallExpr(RecurringJobDef def)
    {
        var type = typeof(MaintenanceJobs);
        var argsCfg = def.Args ?? [];

        var method = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .FirstOrDefault(m => m.Name == def.Method && m.GetParameters().Length == argsCfg.Length)
                     ?? throw new InvalidOperationException(
                         $"Method {type.FullName}.{def.Method} with {argsCfg.Length} parameter(s) not found.");

        var parms = method.GetParameters();
        var convertedArgs = new Expression[parms.Length];

        for (var i = 0; i < parms.Length; i++)
        {
            var val = i < argsCfg.Length ? argsCfg[i] : null;
            val = ProcessSpecialValues(val);

            convertedArgs[i] = CreateParameterExpression(val, parms[i], def.Method, i);
        }

        var j = Expression.Parameter(type, "j");
        var call = Expression.Call(j, method, convertedArgs);

        return method.ReturnType != typeof(Task)
            ? throw new InvalidOperationException($"Method {type.FullName}.{def.Method} must return Task.")
            : Expression.Lambda<Func<MaintenanceJobs, Task>>(call, j);
    }

    private static object? ProcessSpecialValues(object? val)
    {
        return val is string s && s.Equals("{{now}}", StringComparison.OrdinalIgnoreCase)
            ? DateTimeOffset.Now
            : val;
    }

    private static Expression CreateParameterExpression(object? val, ParameterInfo param, string methodName, int paramIndex)
    {
        if (val == null)
        {
            return CreateNullParameterExpression(param, methodName, paramIndex);
        }

        if (param.ParameterType.IsInstanceOfType(val))
        {
            return Expression.Constant(val, param.ParameterType);
        }

        return CreateConvertedParameterExpression(val, param, methodName, paramIndex);
    }

    private static Expression CreateNullParameterExpression(ParameterInfo param, string methodName, int paramIndex)
    {
        if (param.HasDefaultValue)
        {
            return Expression.Constant(param.DefaultValue, param.ParameterType);
        }

        var isNonNullableValueType = param.ParameterType.IsValueType &&
                                     Nullable.GetUnderlyingType(param.ParameterType) == null;

        if (isNonNullableValueType)
        {
            throw new InvalidOperationException(
                $"Parameter {paramIndex} ({param.Name}) of type {param.ParameterType.Name} " +
                $"for method {methodName} is required but no argument was provided.");
        }

        return Expression.Constant(null, param.ParameterType);
    }

    private static Expression CreateConvertedParameterExpression(object val, ParameterInfo param, string methodName, int paramIndex)
    {
        try
        {
            var targetType = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;
            var convertedVal = Convert.ChangeType(val, targetType);

            return Expression.Constant(convertedVal, param.ParameterType);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Cannot convert argument {paramIndex} (value: '{val}', type: {val.GetType().Name}) " +
                $"to parameter type {param.ParameterType.Name} for method {methodName}.",
                ex);
        }
    }
}