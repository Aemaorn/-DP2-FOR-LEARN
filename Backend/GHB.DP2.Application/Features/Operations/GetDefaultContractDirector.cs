namespace GHB.DP2.Application.Features.Operations;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Operations.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetDefaultContractDirector : EndpointBase<Results<Ok<AssigneeResponse[]>, NotFound<string>>>
{
    private readonly Dp2DbContext dp2DbContext;

    public GetDefaultContractDirector(
        ILogger<GetDefaultContractDirector> logger,
        Dp2DbContext dp2DbContext)
        : base(logger)
    {
        this.dp2DbContext = dp2DbContext;
    }

    public override void Configure()
    {
        this.Options(x =>
            x.WithTags("Operations")
             .WithName("GetDefaultContractDirector")
             .Produces<Ok<OperationInfo>>()
             .Produces<NotFound<string>>()
             .WithDescription("Get the default Contract director information"));
        this.Get("/operations/default-contract-director");
    }

    protected override async ValueTask<Results<Ok<AssigneeResponse[]>, NotFound<string>>> HandleRequestAsync(CancellationToken ct)
    {
        var contractManager = await this.dp2DbContext.RawEmployeePositions
                                        .Include(p => p.Employee)
                                        .ThenInclude(e => e.View)
                                        .Where(p =>
                                            p.BusinessUnitId == BusinessUnitId.From(JorPor.DefaultSectionHead.BusinessUnitId) &&
                                            p.Position.Name == JorPor.DefaultSectionHead.PositionName)
                                        .SelectMany(p => p.Employee.Users)
                                        .FirstOrDefaultAsync(ct);

        if (contractManager?.Employee?.View is null)
        {
            this.ThrowError(
                $"ไม่พบข้อมูลผู้รับผิดชอบสัญญา",
                StatusCodes.Status404NotFound);
        }

        AssigneeResponse[] assigneeResponses =
        [
            new(
                null,
                AssigneeGroup.Contract,
                AssigneeType.Director,
                contractManager.Id.Value,
                1,
                contractManager.Employee.View.FullName,
                contractManager.Employee.View.FullPositionName,
                contractManager.Employee.View.BusinessUnitName,
                AssigneeStatus.Draft),
        ];

        return TypedResults.Ok(assigneeResponses);
    }
}