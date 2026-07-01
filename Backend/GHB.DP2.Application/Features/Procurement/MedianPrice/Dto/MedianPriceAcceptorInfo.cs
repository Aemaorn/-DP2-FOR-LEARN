namespace GHB.DP2.Application.Features.Procurement.MedianPrice.Dto;

using System.ComponentModel;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Domain.Common;

public record MedianPriceAcceptorInfo(
    Guid? Id,
    AcceptorType AcceptorType,
    Guid UserId,
    string? CommitteePositionsCode,
    bool? IsUnableToPerformDuties,
    int Sequence,
    string? Remark)
    : AcceptorRequest(Id, AcceptorType, UserId, Sequence, CommitteePositionsCode, IsUnableToPerformDuties, Remark)
{
    public class Validator : Validator<MedianPriceAcceptorInfo>
    {
        public Validator()
        {
            this.RuleFor(x => x.AcceptorType)
                .IsInEnum()
                .NotNull()
                .WithMessage("ประเภทผู้รับรองต้องมีค่าที่ถูกต้อง")
                .Must(x =>
                    x is AcceptorType.Approver or
                        AcceptorType.DepartmentDirectorAgree or
                        AcceptorType.MedianPriceCommittee)
                .WithMessage("ประเภทผู้รับรองต้องเป็น Approver, DepartmentDirectorAgree หรือ MedianPriceCommittee");

            this.RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("รหัสผู้ใช้งานไม่สามารถเป็นค่าว่างได้");

            this.RuleFor(x => x.Sequence)
                .NotEmpty()
                .WithMessage("ลำดับไม่สามารถเป็นค่าว่างได้");

            this.RuleFor(x => x.CommitteePositionsCode)
                .NotEmpty()
                .NotNull()
                .When(x => x.AcceptorType == AcceptorType.MedianPriceCommittee)
                .WithMessage("รหัสตำแหน่งคณะกรรมการไม่สามารถเป็นค่าว่างหรือเป็นค่า null ได้เมื่อประเภทผู้รับรองเป็นคณะกรรมการ");

            this.RuleFor(x => x.IsUnableToPerformDuties)
                .NotNull()
                .When(x => x.AcceptorType == AcceptorType.MedianPriceCommittee)
                .WithMessage("ต้องระบุสถานะการไม่สามารถปฏิบัติหน้าที่ได้เมื่อประเภทผู้รับรองเป็นคณะกรรมการ");
        }
    }
}

public record MedianPriceAcceptorResponseInfo(
    [property: Description("รหัสผู้อนุมัติ")]
    Guid Id,
    [property: Description("ประเภทผู้อนุมัติ")]
    AcceptorType AcceptorType,
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("ชื่อ-สกุล")]
    string FullName,
    [property: Description("ตำแหน่ง")]
    string PositionName,
    [property: Description("ชื่อหน่วยงาน")]
    string DepartmentName,
    [property: Description("สถานะการอนุมัติ")]
    AcceptorStatus Status,
    [property: Description("หมายเหตุ")]
    string? Remark,
    [property: Description("วันที่ดำเนินการ")]
    DateTimeOffset? ActionAt,
    [property: Description("รหัสตำแหน่งคณะกรรมการ")]
    string? CommitteePositionsCode,
    [property: Description("ชื่อตำแหน่งคณะกรรมการ")]
    string? CommitteePositionName,
    [property: Description("ไม่สามารถปฏิบัติหน้าที่ได้")]
    bool? IsUnableToPerformDuties,
    [property: Description("เป็นผู้อนุมัติปัจจุบัน")]
    bool IsCurrent,
    [property: Description("รหัสหน่วยงาน")]
    string? DepartmentCode,
    [property: Description("รหัสผู้ใช้งานผู้ปฏิบัติหน้าที่แทน")]
    Guid? DelegateeUserId);