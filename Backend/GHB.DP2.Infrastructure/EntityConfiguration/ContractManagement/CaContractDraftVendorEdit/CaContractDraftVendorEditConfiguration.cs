namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CaContractDraftVendorEdit;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.ContractManagement;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CaContractDraftVendorEditConfiguration : EntityTypeConfigurationBase<CaContractDraftVendorEdit, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractDraftVendorEdit> builder)
    {
        builder.ToTable(nameof(CaContractDraftVendorEdit), nameof(ContractManagement));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(c => c.ContractDraftVendorId)
               .HasVogenConversion();

        builder.Property(c => c.ProcurementId);

        builder.Property(c => c.ContractManagementId)
               .HasConversion<ContractManagementId.EfCoreValueConverter, ContractManagementId.EfCoreValueComparer>()
               .IsRequired(false);

        builder.HasOne(c => c.ContractManagement)
               .WithMany()
               .HasForeignKey(c => c.ContractManagementId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

        builder.Property(c => c.ContractInvitationVendorsId)
               .HasConversion<ContractInvitationVendorsId.EfCoreValueConverter, ContractInvitationVendorsId.EfCoreValueComparer>();

        builder.Property(c => c.Status)
               .HasConversion(new EnumToStringConverter<ContractDraftVendorEditStatus>())
               .IsRequired();

        builder.Property(c => c.Email)
               .HasMaxLength(256);

        builder.Property(c => c.Title);

        builder.Property(c => c.Description);

        builder.Property(c => c.ContractName)
               .IsRequired();

        builder.Property(c => c.PoNumber)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(c => c.ContractNumber)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(c => c.Budget);

        builder.Property(c => c.ContractSignedDate);

        builder.Property(c => c.ContractEndDate);

        builder.Property(c => c.ContractDraftNumber)
               .HasVogenConversion();

        builder.Property(c => c.ContractTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(c => c.TemplateCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(c => c.TemplateText);

        builder.Property(c => c.SubTemplateCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(c => c.SubTemplateText);

        builder.Property(c => c.StartDate);

        builder.Property(c => c.EndDate);

        builder.Property(c => c.IsWorkingDayOnly);

        builder.Property(c => c.VendorAppointmentMemoDate);

        builder.Property(c => c.DocumentDate);

        builder.Property(c => c.PeriodConditionTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(c => c.ContractStatus)
               .HasConversion(new EnumToStringConverter<ContractStatus>())
               .HasDefaultValue(ContractStatus.Draft)
               .IsRequired();

        builder.OwnsOne(c => c.Buyer, buyerBuilder =>
        {
            buyerBuilder.Property(b => b.Name)
                        .HasColumnName("BuyerName");

            buyerBuilder.Property(b => b.Address)
                        .HasColumnName("BuyerAddress");

            buyerBuilder.OwnsOne(
                b => b.Province,
                provinceBuilder =>
                {
                    provinceBuilder.Property(p => p.Code)
                                   .HasColumnName("BuyerProvinceCode");

                    provinceBuilder.Property(p => p.Name)
                                   .HasColumnName("BuyerProvinceName");
                });

            buyerBuilder.OwnsOne(b => b.District, districtBuilder =>
            {
                districtBuilder.Property(d => d.Code)
                               .HasColumnName("BuyerDistrictCode");

                districtBuilder.Property(d => d.Name)
                               .HasColumnName("BuyerDistrictName");
            });

            buyerBuilder.OwnsOne(b => b.SubDistrict, subDistrictBuilder =>
            {
                subDistrictBuilder.Property(s => s.Code)
                                  .HasColumnName("BuyerSubDistrictCode");

                subDistrictBuilder.Property(s => s.Name)
                                  .HasColumnName("BuyerSubDistrictName");
            });
        });

        builder.OwnsOne(c => c.Vendor, vendorBuilder =>
        {
            vendorBuilder.Property(v => v.VendorId)
                         .HasConversion<SuVendorId.EfCoreValueConverter, SuVendorId.EfCoreValueComparer>()
                         .HasColumnName("VendorId");

            vendorBuilder.Property(v => v.StartDate)
                         .HasColumnName("VendorStartDate");

            vendorBuilder.Property(v => v.EndDate)
                         .HasColumnName("VendorEndDate");

            vendorBuilder.Property(v => v.EstablishmentName)
                         .HasColumnName("VendorEstablishmentName");

            vendorBuilder.Property(v => v.Address)
                         .HasColumnName("VendorAddress");

            vendorBuilder.Property(v => v.Road)
                         .HasColumnName("VendorRoad");

            vendorBuilder.Property(v => v.RawProvinceCode)
                         .HasColumnName("VendorRawProvinceCode");

            vendorBuilder.Property(v => v.RawDistrictCode)
                         .HasColumnName("VendorRawDistrictCode");

            vendorBuilder.Property(v => v.RawSubDistrictCode)
                         .HasColumnName("VendorRawSubDistrictCode");

            vendorBuilder.Property(v => v.VendorRegistrationPlace)
                         .HasColumnName("VendorRegistrationPlace");

            vendorBuilder.HasOne(v => v.VendorInfo)
                         .WithMany()
                         .HasForeignKey(v => v.VendorId)
                         .OnDelete(DeleteBehavior.Restrict);
        });

        builder.OwnsOne(c => c.Agreement, agreementBuilder =>
        {
            agreementBuilder.Property(a => a.Type)
                            .HasConversion(new EnumToStringConverter<AgreementType>())
                            .HasColumnName("AgreementType");

            agreementBuilder.Property(a => a.ContractItem)
                            .HasColumnName("AgreementContractItem");

            agreementBuilder.Property(a => a.IsExchangeGiver)
                            .HasColumnName("IsAgreementExchangeGiver");

            agreementBuilder.Property(a => a.WorkplaceAddress)
                            .HasColumnName("AgreementWorkplaceAddress");

            agreementBuilder.OwnsOne(a => a.WorkplaceProvince, provinceBuilder =>
            {
                provinceBuilder.Property(p => p.Code)
                               .HasColumnName("AgreementWorkplaceProvinceCode");

                provinceBuilder.Property(p => p.Name)
                               .HasColumnName("AgreementWorkplaceProvinceName");
            });

            agreementBuilder.OwnsOne(a => a.WorkplaceDistrict, districtBuilder =>
            {
                districtBuilder.Property(d => d.Code)
                               .HasColumnName("AgreementWorkplaceDistrictCode");

                districtBuilder.Property(d => d.Name)
                               .HasColumnName("AgreementWorkplaceDistrictName");
            });

            agreementBuilder.OwnsOne(a => a.WorkplaceSubDistrict, subDistrictBuilder =>
            {
                subDistrictBuilder.Property(s => s.Code)
                                  .HasColumnName("AgreementWorkplaceSubDistrictCode");

                subDistrictBuilder.Property(s => s.Name)
                                  .HasColumnName("AgreementWorkplaceSubDistrictName");
            });

            agreementBuilder.OwnsOne(a => a.RentalDuration, rentalDurationBuilder =>
            {
                rentalDurationBuilder.Property(r => r.Day)
                                     .HasColumnName("AgreementRentalDurationDay");

                rentalDurationBuilder.Property(r => r.Month)
                                     .HasColumnName("AgreementRentalDurationMonth");

                rentalDurationBuilder.Property(r => r.Year)
                                     .HasColumnName("AgreementRentalDurationYear");
            });

            agreementBuilder.Property(a => a.StartDate)
                            .HasColumnName("AgreementStartDate");

            agreementBuilder.Property(a => a.EndDate)
                            .HasColumnName("AgreementEndDate");

            agreementBuilder.Property(a => a.Brand)
                            .HasColumnName("AgreementBrand");

            agreementBuilder.Property(a => a.Model)
                            .HasColumnName("AgreementModel");

            agreementBuilder.Property(a => a.SerialNumber)
                            .HasColumnName("AgreementSerialNumber");

            agreementBuilder.Property(a => a.EngineCapacityCc)
                            .HasColumnName("AgreementEngineCapacityCc");

            agreementBuilder.Property(a => a.Quantity)
                            .HasColumnName("AgreementQuantity");

            agreementBuilder.Property(a => a.UnitCode)
                            .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                            .HasColumnName("AgreementUnitCode");

            agreementBuilder.HasOne(a => a.Unit)
                            .WithMany()
                            .HasForeignKey(a => a.UnitCode)
                            .HasPrincipalKey(p => p.Code);

            agreementBuilder.Property(a => a.VatRateTypeCode)
                            .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                            .HasColumnName("AgreementVatRateType");

            agreementBuilder.HasOne(a => a.VatRateType)
                            .WithMany()
                            .HasForeignKey(a => a.VatRateTypeCode)
                            .HasPrincipalKey(p => p.Code)
                            .OnDelete(DeleteBehavior.Restrict);

            agreementBuilder.Property(a => a.Price)
                            .HasColumnName("AgreementPrice");

            agreementBuilder.Property(a => a.VatAmount)
                            .HasColumnName("AgreementVatAmount");

            agreementBuilder.Property(a => a.TotalAmount)
                            .HasColumnName("AgreementTotalPrice");
        });

        builder.OwnsOne(c => c.Payment, paymentBuilder =>
        {
            paymentBuilder.Property(p => p.TypeCode)
                          .HasColumnName("PaymentTypeCode");

            paymentBuilder.Property(p => p.DueDays)
                          .HasColumnName("PaymentDueDays");

            paymentBuilder.Property(p => p.RedeliveryDateCode)
                          .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                          .HasColumnName("PaymentRedeliveryDate");

            paymentBuilder.HasOne(p => p.Type)
                          .WithMany()
                          .HasForeignKey(p => p.TypeCode)
                          .HasPrincipalKey(p => p.Code)
                          .OnDelete(DeleteBehavior.Restrict);

            paymentBuilder.HasOne(p => p.RedeliveryDate)
                          .WithMany()
                          .HasForeignKey(p => p.RedeliveryDateCode)
                          .HasPrincipalKey(p => p.Code)
                          .OnDelete(DeleteBehavior.Restrict);
        });

        builder.OwnsOne(c => c.Delivery, deliveryBuilder =>
        {
            deliveryBuilder.Property(d => d.Address)
                           .HasColumnName("DeliveryAddress");

            deliveryBuilder.Property(d => d.Date)
                           .HasColumnName("DeliveryDate");

            deliveryBuilder.Property(d => d.LeadTime)
                           .HasColumnName("DeliveryLeadTime");

            deliveryBuilder.Property(d => d.PeriodTypeCode)
                           .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                           .HasColumnName("DeliveryPeriodTypeCode");

            deliveryBuilder.Property(d => d.LeadOtherTimeTypeCode)
                           .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                           .HasColumnName("DeliveryLeadOtherTimeTypeCode");

            deliveryBuilder.HasOne(d => d.LeadOtherTimeType)
                           .WithMany()
                           .HasForeignKey(d => d.LeadOtherTimeTypeCode)
                           .HasPrincipalKey(p => p.Code)
                           .OnDelete(DeleteBehavior.Restrict);

            deliveryBuilder.Property(d => d.LeadOtherTime)
                           .HasColumnName("DeliveryLeadOtherTime");

            deliveryBuilder.Property(d => d.LeadTimeTypeCode)
                           .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                           .HasColumnName("DeliveryLeadTimeTypeCode");

            deliveryBuilder.HasOne(d => d.LeadTimeType)
                           .WithMany()
                           .HasForeignKey(d => d.LeadTimeTypeCode)
                           .HasPrincipalKey(p => p.Code)
                           .OnDelete(DeleteBehavior.Restrict);

            deliveryBuilder.Property(d => d.CountingConditionCode)
                           .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                           .HasColumnName("DeliveryCountingConditionCode");

            deliveryBuilder.HasOne(d => d.CountingCondition)
                           .WithMany()
                           .HasForeignKey(d => d.CountingConditionCode)
                           .HasPrincipalKey(p => p.Code)
                           .OnDelete(DeleteBehavior.Restrict);
        });

        builder.OwnsOne(c => c.Termination, terminationBuilder =>
        {
            terminationBuilder.Property(t => t.StartDate)
                              .HasColumnName("TerminationStartDate");

            terminationBuilder.Property(t => t.EndDate)
                              .HasColumnName("TerminationEndDate");

            terminationBuilder.OwnsOne(t => t.Duration, durationBuilder =>
            {
                durationBuilder.Property(d => d.Day)
                               .HasColumnName("TerminationDurationDay");

                durationBuilder.Property(d => d.Month)
                               .HasColumnName("TerminationDurationMonth");

                durationBuilder.Property(d => d.Year)
                               .HasColumnName("TerminationDurationYear");
            });

            terminationBuilder.OwnsOne(t => t.VendorProcessingTime, durationBuilder =>
            {
                durationBuilder.Property(d => d.Day)
                               .HasColumnName("VendorProcessingTimePerDay");

                durationBuilder.Property(d => d.Month)
                               .HasColumnName("VendorProcessingTimePerMonth");

                durationBuilder.Property(d => d.Year)
                               .HasColumnName("VendorProcessingTimePerYear");
            });
        });

        builder.HasOne(c => c.ContractDraftVendor)
               .WithMany()
               .HasForeignKey(c => c.ContractDraftVendorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ContractType)
               .WithMany()
               .HasForeignKey(c => c.ContractTypeCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Template)
               .WithMany()
               .HasForeignKey(c => c.TemplateCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.SubTemplate)
               .WithMany()
               .HasForeignKey(c => c.SubTemplateCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.PeriodConditionType)
               .WithMany()
               .HasForeignKey(c => c.PeriodConditionTypeCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.DraftEquipmentRental)
               .WithOne(e => e.CaContractDraftVendorEdit)
               .HasForeignKey<CaContractDraftEditEquipmentRental>(c => c.ContractDraftVendorEditId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.DraftTermsConditions)
               .WithOne(t => t.CaContractDraftVendorEdit)
               .HasForeignKey<CaContractDraftEditTermsConditions>(c => c.ContractDraftVendorEditId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Attachments)
               .WithOne(a => a.CaContractDraftVendorEdit)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Shareholders)
               .WithOne(pd => pd.CaContractDraftVendorEdit)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Checkers)
               .WithOne(c => c.CaContractDraftVendorEdit)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.CheckerAttachment)
               .WithOne()
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Assignees)
               .WithOne(a => a.CaContractDraftVendorEdit)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Components)
               .WithOne(a => a.CaContractDraftVendorEdit)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(c => c.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(CaContractDraftEditVendorDocumentHistory), nameof(ContractManagement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<CaContractDraftEditVendorDocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<ContractDraftVendorEditStatus>());
        });

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
        builder.HasActivityInfo();
    }
}
