namespace GHB.DP2.Infrastructure.EntityConfiguration.Raws;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Raws;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RawErmEmployeeConfiguration : EntityTypeConfigurationBase<RawErmEmployee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<RawErmEmployee> builder)
    {
        builder.ToTable(nameof(RawErmEmployee), nameof(Raws));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("id")
               .HasVogenConversion()
               .IsRequired();

        builder.Property(e => e.Title)
               .HasColumnName("title")
               .IsRequired();

        builder.Property(e => e.EmployeeCode)
               .HasColumnName("emp_code")
               .IsRequired();

        builder.Property(e => e.FirstName)
               .HasColumnName("fname_t")
               .IsRequired();

        builder.Property(e => e.LastName)
               .HasColumnName("lname_t")
               .IsRequired();

        builder.Property(e => e.CitizenCardId)
               .HasColumnName("idcard")
               .IsRequired();

        builder.Property(e => e.BirthDate)
               .HasColumnName("birthdate");

        builder.Property(e => e.Grade)
               .HasColumnName("grade")
               .IsRequired();

        builder.Property(e => e.EmployeeType)
               .HasColumnName("emp_type")
               .IsRequired();

        builder.Property(e => e.PositionId)
               .HasColumnName("position_id")
               .IsRequired();

        builder.Property(e => e.PositionName)
               .HasColumnName("position_name")
               .IsRequired();

        builder.Property(e => e.ActingPosition)
               .HasColumnName("acting_position")
               .IsRequired();

        builder.Property(e => e.ManagerEmpId)
               .HasColumnName("manager_emp_id")
               .IsRequired();

        builder.Property(e => e.Email)
               .HasColumnName("email")
               .IsRequired();

        builder.Property(e => e.OrganizationLevel)
               .HasColumnName("orglevel")
               .IsRequired();

        builder.OwnsOne(e => e.OrganizationLevel1, o => o.ConfigureOrganizationLevel("1"));
        builder.OwnsOne(e => e.OrganizationLevel2, o => o.ConfigureOrganizationLevel("2"));
        builder.OwnsOne(e => e.OrganizationLevel3, o => o.ConfigureOrganizationLevel("3"));
        builder.OwnsOne(e => e.OrganizationLevel4, o => o.ConfigureOrganizationLevel("4"));
        builder.OwnsOne(e => e.OrganizationLevel5, o => o.ConfigureOrganizationLevel("5"));
        builder.OwnsOne(e => e.OrganizationLevel6, o => o.ConfigureOrganizationLevel("6"));
        builder.OwnsOne(e => e.OrganizationLevel7, o => o.ConfigureOrganizationLevel("7"));
        builder.OwnsOne(e => e.OrganizationLevel8, o => o.ConfigureOrganizationLevel("8"));
        builder.OwnsOne(e => e.OrganizationLevel9, o => o.ConfigureOrganizationLevel("9"));

        builder.Property(e => e.StartDate)
               .HasColumnName("start_date");
        builder.Property(e => e.StopDate)
               .HasColumnName("stop_date");
        builder.Property(e => e.LastAction)
               .HasColumnName("last_action");
        builder.Property(e => e.DataDate)
               .HasColumnName("data_date");
    }
}

public static class RawErmEmployeeConfigurationHelpers
{
    public static void ConfigureOrganizationLevel(
        this OwnedNavigationBuilder<RawErmEmployee, OrganizationLevel> builder,
        string prefix)
    {
        builder.Property(o => o.Id)
               .HasColumnName($"objid{prefix}");
        builder.Property(o => o.SolId)
               .HasColumnName($"org_unit_solid{prefix}");
        builder.Property(o => o.Name)
               .HasColumnName($"org_unit_name{prefix}");
        builder.Property(o => o.ShortName)
               .HasColumnName($"short{prefix}");
    }
}