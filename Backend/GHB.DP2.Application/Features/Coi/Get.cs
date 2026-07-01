namespace GHB.DP2.Application.Features.Coi;

using System.Text;
using FluentValidation;
using GHB.DP2.Domain.Common;
using GHB.DP2.Infrastructure.Services.Coi;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record SearchCoiRequest(
    string? Name,
    string? Ssn)
{
    public class Validator : Validator<SearchCoiRequest>
    {
        public Validator()
        {
            this.RuleFor(x => x.Name)
                .NotEmpty()
                .When(x => string.IsNullOrWhiteSpace(x.Ssn))
                .WithMessage("กรุณาระบุ ชื่อ หรือ เลขบัตรประชาชน อย่างน้อยหนึ่งช่อง");

            this.RuleFor(x => x.Ssn)
                .NotEmpty()
                .When(x => string.IsNullOrWhiteSpace(x.Name))
                .WithMessage("กรุณาระบุ ชื่อ หรือ เลขบัตรประชาชน อย่างน้อยหนึ่งช่อง");
        }
    }
}

public record SearchCoiResult(
    QualificationResult Result,
    string Remark);

public class GetByNameEndpoint : EndpointBase<SearchCoiRequest, Results<Ok<SearchCoiResult>, NotFound<string>>>
{
    private readonly ICoiService coiService;

    public GetByNameEndpoint(
        ILogger<GetByNameEndpoint> logger,
        ICoiService coiService)
        : base(logger)
    {
        this.coiService = coiService;
    }

    public override void Configure()
    {
        this.Get("coi/search");
        this.Description(x => x
                              .WithTags("COI")
                              .AllowAnonymous()
                              .Produces<SearchCoiResult>()
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status401Unauthorized)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok<SearchCoiResult>, NotFound<string>>>
        HandleRequestAsync(
            SearchCoiRequest req,
            CancellationToken ct)
    {
        try
        {
            var resultAsync =
                (req.Name.IsNull(), req.Ssn.IsNull()) switch
                {
                    (false, false) => this.coiService.GetCoiByNameSsnAsync(req.Name!, req.Ssn!, ct),
                    (false, true) => this.coiService.GetCoiByNameAsync(req.Name!, ct),
                    (true, false) => this.coiService.GetCoiBySsnAsync(req.Ssn!, ct),
                    _ => throw new InvalidOperationException("ระบุ ชื่อ หรือ เลขบัตรประชาชน อย่างน้อยหนึ่งช่อง"),
                };

            var result = await resultAsync;

            var coiInfos = result.ToArray();

            if (!coiInfos.Any())
            {
                return TypedResults.Ok(
                    new SearchCoiResult(
                        QualificationResult.Pass,
                        "ไม่พบข้อมูล COI"));
            }

            var remarkString = new StringBuilder();

            foreach (var coiInfo in coiInfos)
            {
                remarkString.AppendLine($"พบข้อมูล COI: {coiInfo.EmployeeName}, ตำแหน่ง: {coiInfo.PositionName}, แผนก: {coiInfo.DivisionName}, มี ความสัมพันธ์: {coiInfo.RelationName}");
            }

            var response =
                new SearchCoiResult(
                    QualificationResult.Fail,
                    remarkString.ToString());

            return TypedResults.Ok(response);
        }
        catch (Exception)
        {
            return TypedResults.Ok(
                new SearchCoiResult(
                    QualificationResult.UnKnow,
                    "ตรวจสอบไม่สำเร็จ"));
        }
    }
}