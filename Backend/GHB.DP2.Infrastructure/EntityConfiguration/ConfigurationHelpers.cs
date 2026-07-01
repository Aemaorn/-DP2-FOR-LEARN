namespace GHB.DP2.Infrastructure.EntityConfiguration;

using System.Linq.Expressions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public static class ConfigurationHelpers
{
    public static EntityTypeBuilder<TEntity> HasSoftDelete<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IHasSoftDelete
    {
        builder.Property(e => e.IsDeleted)
               .IsRequired()
               .HasDefaultValue(false);

        builder.HasQueryFilter(e => !e.IsDeleted);

        return builder;
    }

    public static void OwnsAuditInfo<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditableEntity
    {
        builder.OwnsOne(e => e.AuditInfo, a =>
        {
            a.WithOwner();
            a.Property(e => e.CreatedBy)
             .HasColumnName(nameof(AuditInfo.CreatedBy))
             .IsRequired();

            a.Property(e => e.CreatedAt)
             .HasColumnName(nameof(AuditInfo.CreatedAt))
             .IsRequired();

            a.Property(e => e.CreatedByName)
             .HasColumnName(nameof(AuditInfo.CreatedByName))
             .IsRequired();

            a.Property(e => e.LastModifiedBy)
             .HasColumnName(nameof(AuditInfo.LastModifiedBy));

            a.Property(e => e.LastModifiedAt)
             .HasColumnName(nameof(AuditInfo.LastModifiedAt));

            a.Property(e => e.LastModifiedByName)
             .HasColumnName(nameof(AuditInfo.LastModifiedByName));
        });
    }

    public static void AcceptorInfo<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IHasAcceptor
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(e => e.Type)
               .HasConversion(new EnumToStringConverter<AcceptorType>())
               .IsRequired();

        builder.Property(e => e.UserId)
               .IsRequired();

        builder.Property(e => e.EmployeeCode)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(e => e.FullName)
               .IsRequired();

        builder.Property(e => e.BusinessUnitName)
               .IsRequired();

        builder.Property(e => e.PositionName)
               .IsRequired();

        builder.Property(e => e.DelegateeId)
               .HasConversion<DelegateeId.EfCoreValueConverter, DelegateeId.EfCoreValueComparer>();

        builder.Property(e => e.Sequence)
               .IsRequired();

        builder.Property(e => e.Status)
               .HasConversion(new EnumToStringConverter<AcceptorStatus>())
               .IsRequired();

        builder.Property(e => e.ActionAt);

        builder.Property(e => e.Remark);

        builder.Property(e => e.IsCurrent);

        builder.Property(e => e.SendToAcceptorId)
               .HasConversion<UserId.EfCoreValueConverter, UserId.EfCoreValueComparer>();

        builder.HasOne(s => s.User)
               .WithMany()
               .HasForeignKey(e => e.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.SendToAcceptor)
               .WithMany()
               .HasForeignKey(e => e.SendToAcceptorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Delegatee)
               .WithMany()
               .HasForeignKey(e => e.DelegateeId)
               .OnDelete(DeleteBehavior.Restrict);
    }

    public static void AssigneeInfo<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IHasAssignee
    {
        builder.Property(a => a.Group)
               .HasConversion(new EnumToStringConverter<AssigneeGroup>())
               .IsRequired();

        builder.Property(a => a.Type)
               .HasConversion(new EnumToStringConverter<AssigneeType>())
               .IsRequired();

        builder.Property(a => a.UserId)
               .IsRequired();

        builder.Property(a => a.EmployeeCode)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(a => a.FullName)
               .IsRequired();

        builder.Property(a => a.PositionName)
               .IsRequired();

        builder.Property(a => a.BusinessUnitName)
               .IsRequired();

        builder.Property(a => a.Sequence)
               .IsRequired();

        builder.Property(e => e.DelegateeId);

        builder.Property(a => a.Status)
               .HasConversion(new EnumToStringConverter<AssigneeStatus>())
               .IsRequired();

        builder.Property(e => e.ActionAt);

        builder.Property(e => e.Remark);

        builder.Property(e => e.SendToAcceptorId)
               .HasConversion<UserId.EfCoreValueConverter, UserId.EfCoreValueComparer>();

        builder.HasOne(s => s.User)
               .WithMany()
               .HasForeignKey(e => e.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.SendToAcceptor)
               .WithMany()
               .HasForeignKey(e => e.SendToAcceptorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Delegatee)
               .WithMany()
               .HasForeignKey(e => e.DelegateeId)
               .OnDelete(DeleteBehavior.Restrict);
    }

    public static void OwnDocumentHistory<TEntity, TDocumentHistory>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, IEnumerable<TDocumentHistory>>> navigationExpression,
        Action<OwnedNavigationBuilder<TEntity, TDocumentHistory>> buildAction)
        where TEntity : class
        where TDocumentHistory : class, IDocumentHistory
    {
        ArgumentNullException.ThrowIfNull(navigationExpression);

        builder.OwnsMany(navigationExpression!, b =>
        {
            buildAction.Invoke(b);

            b.Property(e => e.FileId)
             .IsRequired();

            b.Property(e => e.Version)
             .IsRequired();

            b.Property(e => e.CreatedAt)
             .IsRequired();

            b.Property(e => e.CreatedBy)
             .IsRequired();

            b.Property(e => e.CreatedByName)
             .IsRequired();

            b.Property(e => e.IsReplaced)
             .IsRequired()
             .HasDefaultValue(false);

            b.Property(e => e.Remark);
        });
    }

    public static void HasActivityInfo<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IHasActivityInfo
    {
        // Ignore the Activities property to prevent EF Core from trying to map it as a column
        builder.Ignore(p => p.Activities);
    }
}