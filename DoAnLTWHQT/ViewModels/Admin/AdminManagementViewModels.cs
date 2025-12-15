using System;
using System.Collections.Generic;
using Ltwhqt.ViewModels.Shared;

namespace Ltwhqt.ViewModels.Admin
{
    public class UserListItemViewModel
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Status { get; set; } = "active";

        public string WarehouseName { get; set; } = string.Empty;

        public string BranchName { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
    }

    public class UserFormViewModel
    {
        public long? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Role { get; set; } = "client";

        public long? WarehouseId { get; set; }

        public long? BranchId { get; set; }

        public string Status { get; set; } = "active";

        public IEnumerable<SelectOptionViewModel> RoleOptions { get; set; } = new List<SelectOptionViewModel>();

        public IEnumerable<SelectOptionViewModel> WarehouseOptions { get; set; } = new List<SelectOptionViewModel>();

        public IEnumerable<SelectOptionViewModel> BranchOptions { get; set; } = new List<SelectOptionViewModel>();
    }

    public class CategoryListItemViewModel
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;
    }

    public class CategoryFormViewModel
    {
        public long? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;
    }

    public class ProductManagementViewModel
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Supplier { get; set; } = string.Empty;

        public string Status { get; set; } = "draft";

        public int VariantCount { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public IList<ProductVariantManagementViewModel> Variants { get; set; } = new List<ProductVariantManagementViewModel>();
    }

    public class ProductFormViewModel
    {
        public long? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public long CategoryId { get; set; }

        public long SupplierId { get; set; }

        public string Status { get; set; } = "draft";

        public IEnumerable<System.Web.Mvc.SelectListItem> CategoryOptions { get; set; } = new List<System.Web.Mvc.SelectListItem>();

        public IEnumerable<System.Web.Mvc.SelectListItem> SupplierOptions { get; set; } = new List<System.Web.Mvc.SelectListItem>();

        public IEnumerable<System.Web.Mvc.SelectListItem> StatusOptions { get; set; } = new List<System.Web.Mvc.SelectListItem>();
    }

    public class ProductVariantManagementViewModel
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Sku { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public decimal? OriginalPrice { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public int QuantityOnHand { get; set; }
    }

    public class ProductVariantFormViewModel
    {
        public long? Id { get; set; }

        public long ProductId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Sku { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public decimal? OriginalPrice { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public System.Web.HttpPostedFileBase ImageFile { get; set; }
    }

    public class ProductReviewManagementViewModel
    {
        public long ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public int Rating { get; set; }

        public string Comment { get; set; } = string.Empty;

        public string Status { get; set; } = "pending";

        public bool IsApproved { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }

    public class SupplierViewModel
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string ContactInfo { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
    }

    public class SupplierFormViewModel
    {
        public long? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string ContactInfo { get; set; } = string.Empty;
    }

    public class InventorySnapshotViewModel
    {
        public long VariantId { get; set; }

        public string VariantName { get; set; } = string.Empty;

        public string WarehouseName { get; set; } = string.Empty;

        public int QuantityOnHand { get; set; }

        public int QuantityReserved { get; set; }

        public int ReorderLevel { get; set; }

        public bool IsWarning => QuantityOnHand <= ReorderLevel;
    }

    public class InventoryAdjustmentViewModel
    {
        public long VariantId { get; set; }

        public string VariantName { get; set; } = string.Empty;

        public int CurrentQuantity { get; set; }

        public int Adjustment { get; set; }

        public string Reason { get; set; } = string.Empty;
    }

    public class SupplierShipmentViewModel
    {
        public long Id { get; set; }

        public string Supplier { get; set; } = string.Empty;

        public string Warehouse { get; set; } = string.Empty;

        public string Variant { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string Status { get; set; } = "pending";

        public DateTimeOffset ReceivedAt { get; set; }
    }

    public class WarehouseTransferViewModel
    {
        public long Id { get; set; }

        public string FromWarehouse { get; set; } = string.Empty;

        public string ToBranch { get; set; } = string.Empty;

        public string Variant { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string Status { get; set; } = "pending";

        public DateTimeOffset CreatedAt { get; set; }
    }

    public class BranchViewModel
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Manager { get; set; } = string.Empty;

        public string SourceWarehouse { get; set; } = string.Empty;
    }

    public class BranchFormViewModel
    {
        public long? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public long? ManagerUserId { get; set; }

        public long? WarehouseId { get; set; }

        public IEnumerable<SelectOptionViewModel> ManagerOptions { get; set; } = new List<SelectOptionViewModel>();

        public IEnumerable<SelectOptionViewModel> WarehouseOptions { get; set; } = new List<SelectOptionViewModel>();
    }

    public class BranchInventoryViewModel
    {
        public string BranchName { get; set; } = string.Empty;

        public string Variant { get; set; } = string.Empty;

        public int QuantityOnHand { get; set; }

        public int QuantityReserved { get; set; }
    }

    public class OrderManagementViewModel
    {
        public long Id { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public string Branch { get; set; } = string.Empty;

        public string Customer { get; set; } = string.Empty;

        public string Status { get; set; } = "pending";

        public decimal TotalAmount { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public IList<OrderLineViewModel> Lines { get; set; } = new List<OrderLineViewModel>();

        public PaymentManagementViewModel Payment { get; set; }
    }

    public class OrderLineViewModel
    {
        public string ProductName { get; set; } = string.Empty;

        public string VariantName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }
    }

    public class PaymentManagementViewModel
    {
        public long Id { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Status { get; set; } = "pending";

        public bool IsDeposit { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }

    public class DiscountManagementViewModel
    {
        public long Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Type { get; set; } = "percent";

        public decimal Value { get; set; }

        public decimal? MinOrderAmount { get; set; }

        public int MaxUses { get; set; }

        public int UsedCount { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset StartAt { get; set; }

        public DateTimeOffset EndAt { get; set; }
    }

    public class DiscountFormViewModel
    {
        public long? Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Type { get; set; } = "percent";

        public decimal Value { get; set; }

        public decimal? MinOrderAmount { get; set; }

        public int? MaxUses { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTimeOffset? StartAt { get; set; }

        public DateTimeOffset? EndAt { get; set; }

        public IEnumerable<SelectOptionViewModel> TypeOptions { get; set; } = new List<SelectOptionViewModel>();
    }

    public class ReportIndicatorViewModel
    {
        public string Label { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public string Trend { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel cho báo cáo tổng tồn kho từ sp_BaoCaoTongTonKhoDonGian
    /// </summary>
    public class TotalInventoryReportViewModel
    {
        public string TenKho { get; set; } = string.Empty;
        public string LoaiKho { get; set; } = string.Empty;
        public int TongSoLuongTon { get; set; }
        public int HangDatTruoc { get; set; }
        
        public bool IsWarehouse => LoaiKho == "Kho Tổng";
        public int SoLuongKhaDung => TongSoLuongTon - HangDatTruoc;
    }
}
