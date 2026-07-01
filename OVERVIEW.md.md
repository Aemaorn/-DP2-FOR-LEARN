

To resolve the error you're encountering with the `SuDocumentTemplate` entity when running `dotnet ef migrations add InitialCreate`, the key issue lies in how the `OwnsOne` method is used in your `OnModelCreating` configuration. Specifically, the `OwnsOne` call is currently misconfigured and may be conflicting with other configurations.

---

### ✅ **Step-by-Step Fix**

#### 1. **Correct the `OwnsOne` Configuration**

In your current `OnModelCreating` method, you have:

```csharp
b.OwnsOne("GHB.DP2.Domain.SystemUtility.BudgetRange", "BudgetForDocument", b1 =>
{
    // ... properties
});
```

**Issue**: You're passing a string (`"GHB.DP2.Domain.SystemUtility.BudgetRange"`) where a `Type` object is expected. This is **invalid** in EF Core.

**Fix**: Use the actual type and ensure it's in scope (or fully qualified). Replace the above with:

```csharp
b.OwnsOne<BudgetRange>("BudgetForDocument", b1 =>
{
    b1.Property<decimal>("Max")
      .HasColumnType("numeric")
      .HasColumnName("BudgetMax");

    b1.Property<decimal>("Min")
      .HasColumnType("numeric")
      .HasColumnName("BudgetMin");

    b1.HasKey("SuDocumentTemplateId");
});
```

> 💡 **Note**: If `BudgetRange` is in a different namespace, you can use the fully qualified name: `typeof(GHB.DP2.Domain.SystemUtility.BudgetRange)`.

---

#### 2. **Ensure `BudgetForDocument` is Properly Declared in the Entity**

Your `SuDocumentTemplate` class must contain a property named `BudgetForDocument` of type `BudgetRange`. For example:

```csharp
public class SuDocumentTemplate
{
    public Guid SuDocumentTemplateId { get; set; }
    public BudgetRange BudgetForDocument { get; set; } = new BudgetRange();
    // ... other properties
}
```

Make sure `BudgetRange` is a **value type** (i.e., not a full-fledged entity with its own primary key), as it is being used as an **owned type**.

---

#### 3. **Remove Redundant or Conflicting `OwnsOne` Calls**

You also have another `OwnsOne` call for the `AuditInfo` property in the same `SuDocumentTemplate` configuration:

```csharp
b.OwnsOne("GHB.DP2.Domain.Common.AuditInfo", "AuditInfo", b1 =>
{
    // ... audit info configuration
});
```

This is likely causing **duplicate configuration** and confusion for EF Core. You should **remove** this block if the `AuditInfo` is already being configured elsewhere or if it's not intended to be an owned type.

If `AuditInfo` is a separate entity (not an owned type), it should be configured using `HasOne` or `HasMany`, not `OwnsOne`.

---

### ✅ Final Configuration for `SuDocumentTemplate`

Here's the corrected version of the relevant part of your `OnModelCreating` method:

```csharp
modelBuilder.Entity<SuDocumentTemplate>(b =>
{
    b.HasOne("GHB.DP2.Domain.SystemUtility.SuParameter", "SupplyMethodCodeInfo")
     .WithMany()
     .HasForeignKey("SupplyMethodCode")
     .HasPrincipalKey("Code");

    b.OwnsOne<BudgetRange>("BudgetForDocument", b1 =>
    {
        b1.Property<decimal>("Max")
          .HasColumnType("numeric")
          .HasColumnName("BudgetMax");

        b1.Property<decimal>("Min")
          .HasColumnType("numeric")
          .HasColumnName("BudgetMin");

        b1.HasKey("SuDocumentTemplateId");
    });

    // Ensure AuditInfo is not being configured here if it's already handled
});
```

---

### 📌 Summary

- **Use the actual type** in `OwnsOne`, not a string.
- **Ensure the property (`BudgetForDocument`)** is declared in your entity.
- **Avoid duplicate configuration** for `AuditInfo` or other properties.
- If `BudgetRange` is a **value type**, it's correct to use `OwnsOne`. If it's a full entity, consider using a navigation property instead.

After applying these changes, run:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

This should resolve the error and properly configure your model.