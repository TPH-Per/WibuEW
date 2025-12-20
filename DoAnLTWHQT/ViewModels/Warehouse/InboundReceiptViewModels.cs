using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Ltwhqt.ViewModels.Warehouse
{
    /// <summary>
    /// ViewModel cho danh sách phiếu nhập kho
    /// </summary>
    public class InboundReceiptViewModel
    {
        public long Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string SupplierName { get; set; } = string.Empty;

        public string WarehouseName { get; set; } = string.Empty;

        public string Status { get; set; } = "pending";

        public DateTime ReceivedAt { get; set; }

        public decimal TotalAmount { get; set; }

        public string Notes { get; set; } = string.Empty;

        public int ItemCount { get; set; }

        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// ViewModel cho form tạo/sửa phiếu nhập kho
    /// </summary>
    public class InboundReceiptFormViewModel
    {
        public long Id { get; set; }

        [Display(Name = "Mã phiếu nhập")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp")]
        [Display(Name = "Nhà cung cấp")]
        public long? SupplierId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn kho")]
        [Display(Name = "Kho")]
        public long? WarehouseId { get; set; }

        [Display(Name = "Ngày nhận hàng")]
        public DateTime? ReceivedAt { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "pending";

        [Display(Name = "Ghi chú")]
        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;

        // Dropdown options
        public IEnumerable<SelectListItem> SupplierOptions { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> WarehouseOptions { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> ProductVariantOptions { get; set; } = new List<SelectListItem>();

        // Chi tiết sản phẩm (JSON string từ form)
        public string DetailsJson { get; set; } = "[]";

        // Hiển thị chi tiết đã có (cho Edit)
        public List<InboundReceiptDetailItemViewModel> ExistingDetails { get; set; } = new List<InboundReceiptDetailItemViewModel>();
    }

    /// <summary>
    /// ViewModel cho chi tiết phiếu nhập kho
    /// </summary>
    public class InboundReceiptDetailViewModel
    {
        public long Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string SupplierName { get; set; } = string.Empty;

        public string WarehouseName { get; set; } = string.Empty;

        public string Status { get; set; } = "pending";

        public DateTime ReceivedAt { get; set; }

        public decimal TotalAmount { get; set; }

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public List<InboundReceiptDetailItemViewModel> Details { get; set; } = new List<InboundReceiptDetailItemViewModel>();
    }

    /// <summary>
    /// ViewModel cho từng dòng sản phẩm trong phiếu nhập
    /// </summary>
    public class InboundReceiptDetailItemViewModel
    {
        public long ProductVariantId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string VariantName { get; set; } = string.Empty;

        public string Sku { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá nhập phải lớn hơn 0")]
        public decimal InputPrice { get; set; }

        public decimal LineTotal => Quantity * InputPrice;
    }
}
