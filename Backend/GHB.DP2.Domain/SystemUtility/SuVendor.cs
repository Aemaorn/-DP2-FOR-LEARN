namespace GHB.DP2.Domain.SystemUtility;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using LanguageExt;
using System.ComponentModel.DataAnnotations;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct SuVendorId
{
    public static SuVendorId New() => From(Guid.CreateVersion7());
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct SuVendorAttachmentId
{
    public static SuVendorAttachmentId New() => From(Guid.CreateVersion7());
}

public enum SuVendorNationality
{
    TH,
    Foreign,
}

public enum SuVendorType
{
    [Display(Name = "บุคคลธรรมดา")]
    Individual,

    [Display(Name = "นิติบุคคล")]
    JuristicPerson,

    [Display(Name = "กิจการร่วมค้า (Consortium)")]
    Consortium,

    [Display(Name = "กิจการร่วมค้า (Joint Venture)")]
    JointVenture,
}

public record Address(
    string? HouseNumber,
    string? RoomNumber,
    string? Floor,
    string? VillageName,
    string? Moo,
    string? Allay,
    string? Road,
    string? RawProvinceCode,
    string? RawDistrictCode,
    string? RawSubDistrictCode,
    string? PostalCode);

public record VendorProfile(
    ParameterCode EntrepreneurType,
    string TaxpayerIdentificationNo,
    string EstablishmentName);

public record ContactInfo(
    string? Tel,
    string? Fax,
    string Email);

public record SapInfo(
    string SapVendorNumber,
    string SapBranchNumber);

public partial class SuVendor : AuditableEntity<SuVendorId>, IHasSoftDelete
{
    private List<SuVendorAttachment> attachments = new();

    public override SuVendorId Id { get; init; }

    public SuVendorNationality Nationality { get; private set; }

    public SuVendorType Type { get; private set; }

    public ParameterCode EntrepreneurType { get; private set; }

    public string TaxpayerIdentificationNo { get; private set; }

    public string EstablishmentName { get; private set; }

    public string PlaceName { get; private set; }

    public string? HouseNumber { get; private set; }

    public string? RoomNumber { get; private set; }

    public string? Floor { get; private set; }

    public string? VillageName { get; private set; }

    public string? Moo { get; private set; }

    public string? Allay { get; private set; }

    public string AddressLine =>
        $"{this.HouseNumber ?? string.Empty} {this.RoomNumber ?? string.Empty} {this.Floor ?? string.Empty} {this.VillageName ?? string.Empty} {this.Moo ?? string.Empty} {this.Allay ?? string.Empty}".Trim();

    public string? Road { get; private set; }

    public string? RawProvinceCode { get; private set; }

    public string? RawDistrictCode { get; private set; }

    public string? RawSubDistrictCode { get; private set; }

    public string? PostalCode { get; private set; }

    public string? Tel { get; private set; }

    public string? Fax { get; private set; }

    public string SapVendorNumber { get; private set; }

    public string SapBranchNumber { get; private set; }

    public string Email { get; private set; }

    public virtual IReadOnlyCollection<SuVendorAttachment> Attachments => this.attachments;

    public virtual IReadOnlyCollection<SuVendorCheckHistory> VendorCheck { get; set; }

    public virtual SuParameter EntrepreneurTypeInfo { get; init; }

    public static SuVendor Create(
        SuVendorNationality nationality,
        SuVendorType type,
        VendorProfile vendorProfile,
        string placeName,
        Address address,
        ContactInfo contactInfo,
        SapInfo sapInfo)
    {
        return new SuVendor
        {
            Id = SuVendorId.New(),
            Nationality = nationality,
            Type = type,
            EntrepreneurType = vendorProfile.EntrepreneurType,
            TaxpayerIdentificationNo = vendorProfile.TaxpayerIdentificationNo,
            EstablishmentName = vendorProfile.EstablishmentName,
            PlaceName = placeName,
            HouseNumber = address.HouseNumber,
            RoomNumber = address.RoomNumber,
            Floor = address.Floor,
            VillageName = address.VillageName,
            Moo = address.Moo,
            Allay = address.Allay,
            Road = address.Road,
            RawProvinceCode = address.RawProvinceCode,
            RawDistrictCode = address.RawDistrictCode,
            RawSubDistrictCode = address.RawSubDistrictCode,
            PostalCode = address.PostalCode,
            Tel = contactInfo.Tel,
            Fax = contactInfo.Fax,
            SapVendorNumber = sapInfo.SapVendorNumber,
            SapBranchNumber = sapInfo.SapBranchNumber,
            Email = contactInfo.Email,
        };
    }

    public Unit Update(
        SuVendorNationality nationality,
        SuVendorType type,
        VendorProfile vendorProfile,
        string placeName,
        Address address,
        ContactInfo contactInfo,
        SapInfo sapInfo)
    {
        this.Nationality = nationality;
        this.Type = type;
        this.EntrepreneurType = vendorProfile.EntrepreneurType;
        this.TaxpayerIdentificationNo = vendorProfile.TaxpayerIdentificationNo;
        this.EstablishmentName = vendorProfile.EstablishmentName;
        this.PlaceName = placeName;
        this.HouseNumber = address.HouseNumber;
        this.RoomNumber = address.RoomNumber;
        this.Floor = address.Floor;
        this.VillageName = address.VillageName;
        this.Moo = address.Moo;
        this.Allay = address.Allay;
        this.Road = address.Road;
        this.RawProvinceCode = address.RawProvinceCode;
        this.RawDistrictCode = address.RawDistrictCode;
        this.RawSubDistrictCode = address.RawSubDistrictCode;
        this.PostalCode = address.PostalCode;
        this.Tel = contactInfo.Tel;
        this.Fax = contactInfo.Fax;
        this.SapVendorNumber = sapInfo.SapVendorNumber;
        this.SapBranchNumber = sapInfo.SapBranchNumber;
        this.Email = contactInfo.Email;

        return unit;
    }

    public Unit AddAttachment(string fileName, int sequence, FileId fileId, bool isPrivate = false)
    {
        if (this.attachments.Any(x => x.Sequence == sequence))
        {
            throw new InvalidOperationException($"Attachment with Sequence {sequence} already exists.");
        }

        var attachment = SuVendorAttachment.Create(
            this,
            fileName,
            sequence,
            isPrivate,
            fileId);

        this.attachments.Add(attachment);

        return unit;
    }

    public Unit RemoveAttachmentById(Guid attachmentId)
    {
        var attach = this.attachments
                         .Where(w => w.Id != SuVendorAttachmentId.From(attachmentId))
                         .OrderBy(o => o.Sequence)
                         .ToList();

        this.attachments = attach;

        for (int i = 0; i < this.attachments.Count; i++)
        {
            this.attachments[i].Sequence = i + 1;
        }

        return unit;
    }
}

public class SuVendorAttachment : AuditableEntity<SuVendorAttachmentId>
{
    public override SuVendorAttachmentId Id { get; init; }

    public SuVendorId VendorId { get; init; }

    public FileId FileId { get; init; }

    public string FileName { get; set; }

    public bool IsPrivate { get; set; }

    public int Sequence { get; set; }

    public virtual SuVendor Vendor { get; private set; }

    internal static SuVendorAttachment Create(
        SuVendor vendor,
        string fileName,
        int sequence,
        bool isPrivate,
        FileId fileId)
    {
        return new SuVendorAttachment()
        {
            Id = SuVendorAttachmentId.New(),
            Vendor = vendor,
            VendorId = vendor.Id,
            FileName = fileName,
            Sequence = sequence,
            IsPrivate = isPrivate,
            FileId = fileId,
        };
    }

    public Unit Update(int sequence, bool isPrivate)
    {
        this.IsPrivate = isPrivate;
        this.Sequence = sequence;

        return unit;
    }

    public Unit SetSequence(int seq)
    {
        this.Sequence = seq;

        return unit;
    }
}