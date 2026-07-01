namespace GHB.DP2.Application.Features.Procurement.MedianPrice.Dto;

using FluentValidation;
using GHB.DP2.Domain.Common;

public record MedianPriceAssigneeInfo(
    Guid? Id,
    AssigneeGroup AssigneeGroup,
    AssigneeType AssigneeType,
    Guid UserId,
    int Sequence)
{
    public class Validator : Validator<MedianPriceAssigneeInfo>
    {
        public Validator()
        {
            this.RuleFor(x => x.AssigneeType)
                .IsInEnum()
                .NotNull()
                .WithMessage("ประเภทผู้มอบหมายต้องมีค่าที่ถูกต้อง");

            this.RuleFor(r => r.AssigneeGroup)
                .IsInEnum()
                .NotNull()
                .WithMessage("กลุ่มผู้มอบหมายต้องมีค่าที่ถูกต้อง");

            this.RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("รหัสผู้ใช้งานไม่สามารถเป็นค่าว่างได้");

            this.RuleFor(x => x.Sequence)
                .NotEmpty()
                .WithMessage("ลำดับไม่สามารถเป็นค่าว่างได้");
        }
    }
}