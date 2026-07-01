namespace GHB.DP2.Application.Dtos;

using FluentValidation;
using GHB.DP2.Application.Features.Operations.Dto;
using GHB.DP2.Domain.Common;

public record AcceptorRequest(
    Guid? Id,
    AcceptorType AcceptorType,
    Guid UserId,
    int Sequence,
    string? CommitteePositionsCode = default,
    bool? IsUnableToPerformDuties = default,
    string? Remark = default);

public class AcceptorRequestValidator : Validator<AcceptorRequest>
{
    public AcceptorRequestValidator()
    {
        this.RuleFor(r => r.AcceptorType)
            .IsInEnum()
            .WithMessage("ไม่พบประเภทผู้อนุมัติในระบบ");

        this.RuleFor(r => r.UserId)
            .NotEmpty()
            .WithMessage("ผู้ใช้จะต้องไม่เป็นค่าว่าง");

        this.RuleFor(x => x.Sequence)
            .GreaterThan(0)
            .WithMessage("ลำดับของผผู้เห็นชอบ/อนุมัติต้องมากกว่า 0");
    }
}

public record AcceptorResponseBase<T>(
    T Id,
    AcceptorType AcceptorType,
    Guid UserId,
    int Sequence,
    string FullName,
    string PositionName,
    string DepartmentName,
    AcceptorStatus Status,
    string? Remark = default,
    DateTimeOffset? ActionAt = default,
    string? CommitteePositionsCode = default,
    string? CommitteePositionName = default,
    bool? IsUnableToPerformDuties = default,
    string? DepartmentCode = default,
    Guid? DelegateId = default,
    bool IsCurrent = default,
    Guid? DelegateeUserId = default);

public record AcceptorResponse(
    Guid Id,
    AcceptorType AcceptorType,
    Guid UserId,
    int Sequence,
    string FullName,
    string PositionName,
    string DepartmentName,
    AcceptorStatus Status,
    string? Remark = default,
    DateTimeOffset? ActionAt = default,
    string? CommitteePositionsCode = default,
    string? CommitteePositionName = default,
    bool? IsUnableToPerformDuties = default,
    string? DepartmentCode = default,
    Guid? DelegateId = default,
    bool IsCurrent = default,
    Guid? DelegateeUserId = default) : AcceptorResponseBase<Guid>(
        Id, AcceptorType, UserId, Sequence, FullName, PositionName, DepartmentName,
        Status, Remark, ActionAt, CommitteePositionsCode, CommitteePositionName,
        IsUnableToPerformDuties, DepartmentCode, DelegateId, IsCurrent, DelegateeUserId);

public record AcceptorNoIdResponse(
    Guid? Id,
    AcceptorType AcceptorType,
    Guid UserId,
    int Sequence,
    string FullName,
    string PositionName,
    string DepartmentName,
    AcceptorStatus Status,
    string? Remark = default,
    DateTimeOffset? ActionAt = default,
    string? CommitteePositionsCode = default,
    string? CommitteePositionName = default,
    bool? IsUnableToPerformDuties = default,
    string? DepartmentCode = default,
    Guid? DelegateId = default,
    bool IsCurrent = default,
    Guid? DelegateeUserId = default) : AcceptorResponseBase<Guid?>(
        Id, AcceptorType, UserId, Sequence, FullName, PositionName, DepartmentName,
        Status, Remark, ActionAt, CommitteePositionsCode, CommitteePositionName,
        IsUnableToPerformDuties, DepartmentCode, DelegateId, IsCurrent, DelegateeUserId);

public static class OperationExtensions
{
    public static IEnumerable<AcceptorResponse> ToAcceptorResponse(
        this IEnumerable<OperationInfo> operations,
        AcceptorType acceptorType = AcceptorType.Approver)
    {
        return operations.Select((op, index) => new AcceptorResponse(
            Id: Guid.NewGuid(), // Assuming Id is generated or set elsewhere
            AcceptorType: acceptorType, // Replace it with actual logic to determine type
            UserId: op.UserId.Value,
            Sequence: index + 1, // Replace it with actual sequence logic
            FullName: op.FullName,
            PositionName: op.FullPositionName,
            DepartmentName: string.Empty, // Replace it with actual department name logic
            Status: AcceptorStatus.Pending, // Replace it with actual status logic
            Remark: null,
            ActionAt: null,
            CommitteePositionsCode: null,
            CommitteePositionName: null,
            IsUnableToPerformDuties: false,
            DepartmentCode: string.Empty, // Replace it with actual department code logic
            DelegateId: null,
            IsCurrent: true));
    }
}