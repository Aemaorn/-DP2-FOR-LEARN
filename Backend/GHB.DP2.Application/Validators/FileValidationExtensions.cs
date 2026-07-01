namespace GHB.DP2.Application.Validators;

using FluentValidation;
using GHB.DP2.Application.Contracts;
using Microsoft.AspNetCore.Http;

public static class FileValidationExtensions
{
    // General attachment whitelist — must match the user-facing announcement
    // (e.g. "รองรับไฟล์ที่มีนามสกุล .doc, .docx, .xls, .xlsx, .csv, .pdf, .png, .jpg, .jpeg").
    // .odt and .webp were removed: .odt is only needed for the document template flow
    // (see TemplateExtensionContentTypeMap), and .webp had no legitimate use here and has a
    // history of decoder CVEs (libwebp).
    private static readonly Dictionary<string, string[]> ExtensionContentTypeMap = new()
    {
        [".pdf"] = ["application/pdf"],
        [".jpeg"] = ["image/jpeg", "image/jpg"],
        [".jpg"] = ["image/jpeg", "image/jpg"],
        [".png"] = ["image/png"],
        [".doc"] = ["application/msword"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
        [".xls"] = ["application/vnd.ms-excel"],
        [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
        [".csv"] = ["text/csv"],
    };

    // Template uploads (ST007 / ChEditor / OnlyOffice) need .odt — kept separate from the
    // general attachment whitelist so it can't be used as a bypass for procurement attachments.
    private static readonly Dictionary<string, string[]> TemplateExtensionContentTypeMap = new()
    {
        [".odt"] = ["application/vnd.oasis.opendocument.text"],
    };

    private const string AllowedExtensionsMessage =
        "File type must be one of the following: pdf, jpeg, jpg, png, doc, docx, xls, xlsx, csv";

    private const string AllowedTemplateExtensionsMessage =
        "File type must be: odt";

    private static readonly HashSet<string> AllowedExtensions = new(ExtensionContentTypeMap.Keys);

    private static bool IsSafeFileName(string? filename) =>
        !string.IsNullOrWhiteSpace(filename)
        && !filename.Any(char.IsControl)
        && !filename.Contains('/')
        && !filename.Contains('\\');

    public static IRuleBuilderOptions<T, IHasFile> MustBeValidFile<T>(
        this IRuleBuilder<T, IHasFile> ruleBuilder,
        bool isRequired = true)
    {
        return ruleBuilder
               .ChildRules(hasFile =>
                   hasFile.RuleFor(h => h.File)
                          .RequiredFileIf(isRequired))
               .ChildRules(hasFile =>
                   hasFile.RuleFor(h => h.File!)
                          .MustBeValidFile()
                          .When(f => f.File is not null));
    }

    public static IRuleBuilderOptions<T, IHasFile> MustBeValidPdfFile<T>(
        this IRuleBuilder<T, IHasFile> ruleBuilder,
        bool isRequired = true)
    {
        return ruleBuilder
               .ChildRules(hasFile =>
                   hasFile.RuleFor(h => h.File)
                          .RequiredFileIf(isRequired))
               .ChildRules(hasFile =>
                   hasFile.RuleFor(h => h.File!)
                          .MustBeValidPdfFile()
                          .When(f => f.File is not null));
    }

    public static IRuleBuilderOptions<T, IHasFile> MustBeValidTemplateFile<T>(
        this IRuleBuilder<T, IHasFile> ruleBuilder,
        bool isRequired = true)
    {
        return ruleBuilder
               .ChildRules(hasFile =>
                   hasFile.RuleFor(h => h.File)
                          .RequiredFileIf(isRequired))
               .ChildRules(hasFile =>
                   hasFile.RuleFor(h => h.File!)
                          .MustBeValidTemplateFile()
                          .When(f => f.File is not null));
    }

    public static IRuleBuilderOptions<T, IFormFile> MustBeValidTemplateFile<T>(this IRuleBuilder<T, IFormFile> ruleBuilder)
    {
        return ruleBuilder
               .MustBeValidFileSize()
               .Must(x =>
               {
                   if (!IsSafeFileName(x.FileName))
                   {
                       return false;
                   }

                   var ext = Path.GetExtension(x.FileName)?.ToLowerInvariant();
                   return ext is not null
                       && TemplateExtensionContentTypeMap.TryGetValue(ext, out var allowedTypes)
                       && allowedTypes.Contains(x.ContentType);
               })
               .WithMessage(AllowedTemplateExtensionsMessage);
    }

    public static IRuleBuilderOptions<T, IFormFile> MustBeValidPdfFile<T>(this IRuleBuilder<T, IFormFile> ruleBuilder)
    {
        return ruleBuilder
               .MustBeValidFileSize()
               .Must(x =>
                   IsSafeFileName(x.FileName)
                   && x.ContentType is "application/pdf"
                   && Path.GetExtension(x.FileName)?.ToLowerInvariant() is ".pdf")
               .WithMessage("File type must be pdf");
    }

    public static IRuleBuilderOptions<T, IFormFile?> RequiredFileIf<T>(
        this IRuleBuilder<T, IFormFile?> ruleBuilder,
        bool isRequired = true)
    {
        if (!isRequired)
        {
            return ruleBuilder.Must(_ => true);
        }

        return ruleBuilder
               .NotNull()
               .WithMessage("File is required");
    }

    public static IRuleBuilderOptions<T, IFormFile> MustBeValidFile<T>(this IRuleBuilder<T, IFormFile> ruleBuilder)
    {
        return ruleBuilder
               .MustBeValidFileSize()
               .Must(x =>
               {
                   if (!IsSafeFileName(x.FileName))
                   {
                       return false;
                   }

                   var ext = Path.GetExtension(x.FileName)?.ToLowerInvariant();
                   return ext is not null
                       && ExtensionContentTypeMap.TryGetValue(ext, out var allowedTypes)
                       && allowedTypes.Contains(x.ContentType);
               })
               .WithMessage(AllowedExtensionsMessage);
    }

    public static IRuleBuilderOptions<T, string> MustBeValidFileExtension<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
               .Must(filename =>
                   IsSafeFileName(filename)
                   && AllowedExtensions.Contains(
                       Path.GetExtension(filename).ToLowerInvariant()))
               .WithMessage(AllowedExtensionsMessage);
    }

    public static IRuleBuilderOptions<T, IFormFile> MustBeValidFileSize<T>(this IRuleBuilder<T, IFormFile> ruleBuilder)
    {
        return ruleBuilder
               .Must(x => x.Length > 0)
               .WithMessage("File size must be greater than 0")
               .Must(x => x.Length < 10 * 1024 * 1024)
               .WithMessage("File size must be less than 10 MB");
    }

    public static void AddAttachmentsRules<T>(this Validator<T> validator)
        where T : IHasAttachmentsWithId
    {
        validator.RuleFor(r => r.Attachments)
            .NotNull()
            .WithMessage("ต้องระบุไฟล์แนบอย่างน้อย 1 ไฟล์");

        validator.RuleForEach(x => x.Attachments)
            .ChildRules(attachment =>
            {
                attachment.RuleFor(x => x.DocumentTypeCode)
                          .NotEmpty()
                          .WithMessage("ต้องระบุประเภทเอกสาร");

                attachment
                    .RuleForEach(x => x.FileAttachments)
                    .ChildRules(file => file.AddSequenceFileAttachmentRule());
            });
    }

    public static void AddSequenceFileAttachmentRule<T>(this InlineValidator<T> file)
        where T : IHasSequenceFileAttachment
    {
        file.RuleFor(x => x.FileId)
            .NotEmpty()
            .WithMessage("ต้องระบุไฟล์ที่แนบ");

        file.RuleFor(x => x.Sequence)
            .GreaterThan(0)
            .WithMessage("ลำดับไฟล์แนบต้องมากกว่าศูนย์");

        file.RuleFor(fa => fa.FileName).MustBeValidFileExtension();
    }
}