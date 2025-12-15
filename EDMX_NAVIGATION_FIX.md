# Fix: EDMX Navigation Property Issue

## 🐛 Lỗi

```
warehouse_transfers không chứa định nghĩa cho warehouse_transfer_details
```

## 🔍 Nguyên Nhân

Bảng `warehouse_transfer_details` được tạo bằng SQL script **SAU KHI** EDMX model đã được generate. Entity Framework không tự động detect bảng mới này.

## ✅ Giải Pháp Tạm Thời (Đã Apply)

**Thay vì dùng navigation property:**
```csharp
// ❌ LỖI - navigation property không tồn tại
var transfer = db.warehouse_transfers
    .Include(t => t.warehouse_transfer_details)
    .FirstOrDefault(t => t.id == id);
```

**Load thủ công qua ViewBag:**
```csharp
// ✅ OK - load manual
var transfer = db.warehouse_transfers
    .Include(t => t.warehouse)
    .Include(t => t.branch)
    .FirstOrDefault(t => t.id == id);

var details = db.warehouse_transfer_details
    .Where(d => d.transfer_id == id)
    .ToList();

ViewBag.TransferDetails = details;
```

**Trong View:**
```razor
@{
    var details = ViewBag.TransferDetails as List<warehouse_transfer_details>;
}
@if (details != null && details.Any())
{
    foreach (var detail in details)
    {
        // Render
    }
}
```

## 🔧 Giải Pháp Lâu Dài: Update EDMX

### Cách 1: Update Model from Database (Recommended)

**Bước 1:** Mở `Perw.edmx` (double-click file)

**Bước 2:** Right-click vào diagram → **Update Model from Database**

**Bước 3:** Click tab **Add**

**Bước 4:** Expand **Tables** → Check `warehouse_transfer_details`

**Bước 5:** Click **Finish**

**Kết quả:**
- Entity `warehouse_transfer_details` được tạo
- Navigation property `warehouse_transfers.warehouse_transfer_details` được tạo tự động
- Foreign key relationship được map

**Bước 6:** Save EDMX (Ctrl+S)

**Bước 7:** Build solution

### Cách 2: Manually Add Entity (Advanced)

Nếu Update Model from Database không hoạt động:

**1. Tạo file entity thủ công:**

```csharp
// warehouse_transfer_details.cs
namespace DoAnLTWHQT
{
    using System;
    using System.Collections.Generic;
    
    public partial class warehouse_transfer_details
    {
        public long id { get; set; }
        public long transfer_id { get; set; }
        public long product_variant_id { get; set; }
        public int quantity { get; set; }
        public string notes { get; set; }
        public Nullable<System.DateTime> created_at { get; set; }
        public Nullable<System.DateTime> updated_at { get; set; }
        public Nullable<System.DateTime> deleted_at { get; set; }
    
        public virtual warehouse_transfers warehouse_transfers { get; set; }
        public virtual product_variants product_variants { get; set; }
    }
}
```

**2. Update DbContext (Perw.Context.cs):**

```csharp
public partial class Entities : DbContext
{
    // Existing DbSets...
    
    public virtual DbSet<warehouse_transfer_details> warehouse_transfer_details { get; set; }
}
```

**3. Update warehouse_transfers entity:**

```csharp
public partial class warehouse_transfers
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public warehouse_transfers()
    {
        this.warehouse_transfer_details = new HashSet<warehouse_transfer_details>();
    }
    
    // Existing properties...
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
    public virtual ICollection<warehouse_transfer_details> warehouse_transfer_details { get; set; }
}
```

## ✨ Sau Khi Update EDMX

**Controller có thể dùng Include bình thường:**

```csharp
public ActionResult Details(long id)
{
    var transfer = db.warehouse_transfers
        .Include(t => t.warehouse)
        .Include(t => t.branch)
        .Include(t => t.warehouse_transfer_details)  // ✅ Giờ hoạt động!
        .FirstOrDefault(t => t.id == id);

    return View(transfer);
}
```

**View dùng navigation property:**

```razor
@foreach (var detail in Model.warehouse_transfer_details)
{
    @detail.product_variants.name
}
```

## 📊 Comparison

| Cách | Ưu điểm | Nhược điểm |
|------|---------|------------|
| **Manual Load (Current)** | Nhanh, không cần regenerate EDMX | Phải viết query thủ công, ViewBag overhead |
| **Update EDMX** | Type-safe, IntelliSense support, cleaner code | Phải regenerate khi schema thay đổi |

## 🎯 Khuyến Nghị

**Cho Development:** Dùng manual load để test nhanh

**Cho Production:** Update EDMX để code clean và type-safe

## 🔍 Verify After EDMX Update

```csharp
// Check navigation property exists
var db = new Entities();
var test = db.warehouse_transfers.FirstOrDefault();
if (test != null)
{
    var details = test.warehouse_transfer_details; // Should compile
    Console.WriteLine($"Details count: {details.Count()}");
}
```

---

**Status:** ✅ Temporary fix applied, production fix pending EDMX update
