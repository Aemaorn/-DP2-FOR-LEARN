namespace GHB.DP2.Application.Features.Procurement.Pw184;

using GHB.DP2.Application.Features.Procurement.Pw184.Abstract;
using GHB.DP2.Domain.Procurement.Pw184;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record GetPw184ByIdRequest(Guid Id);

public class GetPw184ByIdEndpoint : Pw184EndpointBase<GetPw184ByIdRequest, Results<Ok<GetPw184Response>, NotFound<string>>>
{
    public GetPw184ByIdEndpoint(ILogger<GetPw184ByIdEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Pw184")
             .WithName("GetPw184ById")
             .Produces<Ok<GetPw184Response>>()
             .Produces<NotFound>());
        this.Get("Pw184/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok<GetPw184Response>, NotFound<string>>> HandleRequestAsync(
        GetPw184ByIdRequest req,
        CancellationToken ct)
    {
        var entity = await this.GetPw184ById(Pw184Id.From(req.Id), ct);

        return TypedResults.Ok(this.MapToResponse(entity));
    }
}
