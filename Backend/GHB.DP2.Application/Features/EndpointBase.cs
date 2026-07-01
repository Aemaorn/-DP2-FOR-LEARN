namespace GHB.DP2.Application.Features;

using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public abstract partial class EndpointBase<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly ILogger logger;

    protected EndpointBase(ILogger logger)
    {
        this.logger = logger;
    }

    public override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        var handlerType = this.GetType().FullName;

        this.logger.LogDebug(
            "Processing request for {HandlerType} with {Summary}",
            handlerType,
            this.Definition.EndpointSummary?.Summary);

        try
        {
            var result = await this.HandleRequestAsync(
                req,
                ct);

            await this.SendResultAsync(result);

            var isNonSuccessResponse = result is IStatusCodeHttpResult { StatusCode: >= 400 };

            if (isNonSuccessResponse)
            {
                this.logger.LogDebug(
                    "Request return with non-success response {Response}",
                    result);
            }

            this.logger.LogDebug("Request processed");
        }
        catch (Exception ex)
        {
            this.logger.LogError(
                ex,
                "Error processing request for {HandlerType}",
                handlerType);

            throw;
        }
    }

    protected abstract ValueTask<TResponse> HandleRequestAsync(TRequest req, CancellationToken ct);
}

public abstract partial class TransactionalEndpointBase<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly ILogger logger;
    private readonly Dp2DbContext dbContext;

    protected TransactionalEndpointBase(ILogger logger, Dp2DbContext dbContext)
    {
        this.logger = logger;
        this.dbContext = dbContext;
    }

    public override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        var handlerType = this.GetType().FullName;

        this.logger.LogDebug(
            "Processing request for {HandlerType} with {Summary}",
            handlerType,
            this.Definition.EndpointSummary?.Summary);

        // Start a transaction
        await using var transaction = await this.dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            var result = await this.HandleRequestAsync(
                req,
                ct);

            await this.SendResultAsync(result);

            var isNonSuccessResponse = result is IStatusCodeHttpResult { StatusCode: >= 400 };

            if (isNonSuccessResponse)
            {
                this.logger.LogDebug(
                    "Request return with non-success response {Response}",
                    result);
            }

            // Commit the transaction
            await transaction.CommitAsync(ct);

            this.logger.LogDebug("Request processed");
        }
        catch (Exception ex)
        {
            this.logger.LogError(
                ex,
                "Error processing request for {HandlerType}",
                handlerType);

            // Roll back the transaction
            await transaction.RollbackAsync(ct);

            throw;
        }
    }

    protected abstract ValueTask<TResponse> HandleRequestAsync(TRequest req, CancellationToken ct);
}

public abstract partial class EndpointBase<TResponse> : EndpointWithoutRequest<TResponse>
    where TResponse : IResult
{
    private readonly ILogger logger;

    protected EndpointBase(ILogger logger)
    {
        this.logger = logger;
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var handlerType = this.GetType().FullName;

        this.logger.LogDebug(
            "Processing request for {HandlerType} with {Summary}",
            handlerType,
            this.Definition.EndpointSummary?.Summary);

        try
        {
            var result = await this.HandleRequestAsync(ct);

            await this.SendResultAsync(result);

            var isNonSuccessResponse = result is IStatusCodeHttpResult { StatusCode: >= 400 };

            if (isNonSuccessResponse)
            {
                this.logger.LogDebug(
                    "Request return with non-success response {Response}",
                    result);
            }

            this.logger.LogDebug("Request processed");
        }
        catch (Exception ex)
        {
            this.logger.LogError(
                ex,
                "Error processing request for {HandlerType}",
                handlerType);

            throw;
        }
    }

    protected abstract ValueTask<TResponse> HandleRequestAsync(CancellationToken ct);
}