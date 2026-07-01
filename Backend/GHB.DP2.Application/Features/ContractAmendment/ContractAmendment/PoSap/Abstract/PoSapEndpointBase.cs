namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoSap.Abstract;

using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoSap;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract class PoSapEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;

    protected PoSapEndpointBase(
        ILogger logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    protected Dp2DbContext DbContext => this.dbContext;

    protected async Task UpsertAcceptorsAsync(
        CamContractAmendmentPoSap entity,
        AcceptorRequest[] requests,
        CancellationToken cancellationToken = default)
    {
        var userIds =
            requests.Select(r => r.UserId)
                    .ToArray();

        var users = await this.ValidateUsersAsync(userIds, cancellationToken);

        // Remove acceptors that are not in the request
        _ = entity.Acceptors
                  .ExceptBy(
                      userIds,
                      a => a.User.Id.Value)
                  .Map(entity.RemoveAcceptor)
                  .ToHashSet();

        // Update existing acceptors and add new ones
        _ = entity.Acceptors
                  .Join(
                      requests,
                      a => a.User.Id.Value,
                      r => r.UserId,
                      (acceptor, request) =>
                      {
                          var user = users.First(u => u.Id.Value == request.UserId);
                          acceptor.SetSequence(request.Sequence)
                                  .SetType(request.AcceptorType)
                                  .SetUser(
                                      user.Id,
                                      user.EmployeeCode,
                                      user.Employee?.View?.FullName ?? string.Empty,
                                      user.Employee?.View?.FullPositionName ?? string.Empty,
                                      user.Employee?.View?.BusinessUnitName ?? string.Empty);

                          return acceptor;
                      })
                  .ToHashSet();

        // Add new acceptors that are not already in the entity
        var usersInEntity =
            entity.Acceptors
                  .Map(a => a.User.Id.Value)
                  .ToArray();
        _ = requests
            .ExceptBy(
                usersInEntity,
                r => r.UserId)
            .Join(
                users,
                r => r.UserId,
                u => u.Id.Value,
                (request, user) => new { request, user })
            .Map(result => CamContractAmendmentPoSapAcceptor.Create(
                result.request.AcceptorType,
                result.user,
                result.request.Sequence)).Iter(s => entity.AddAcceptor(s));
    }

    protected static async Task SendNotificationAsync(CamContractAmendmentPoSap entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.ContractAmendment)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Empty, "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private async Task<SuUser[]> ValidateUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Map(UserId.From).ToArray();

        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => ids.Contains(u.Id))
                              .ToArrayAsync(cancellationToken);

        var missingIds = ids.Except(users.Map(u => u.Id)).ToArray();

        if (missingIds.Length > 0)
        {
            this.ThrowError($"User with ID {string.Join(", ", missingIds)} not found.", StatusCodes.Status404NotFound);
        }

        return users;
    }
}