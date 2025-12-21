using System;
using System.Collections.Generic;

namespace Ltwhqt.ViewModels.Branch
{
    /// <summary>
    /// ViewModel cho danh sách phiếu yêu cầu nhập hàng của Branch
    /// </summary>
    public class TransferRequestListViewModel
    {
        public long Id { get; set; }
        public long FromWarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public long ToBranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public DateTime TransferDate { get; set; }
        public string Status { get; set; } = "Requested";
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalAmount { get; set; }

        public string StatusBadgeClass
        {
            get
            {
                switch (Status?.ToLower())
                {
                    case "requested": return "bg-warning text-dark";
                    case "approved": return "bg-info";
                    case "processing": return "bg-primary";
                    case "completed": return "bg-success";
                    case "rejected": return "bg-danger";
                    case "cancelled": return "bg-secondary";
                    default: return "bg-secondary";
                }
            }
        }

        public string StatusDisplayName
        {
            get
            {
                switch (Status?.ToLower())
                {
                    case "requested": return "Chờ duyệt";
                    case "approved": return "Đã duyệt";
                    case "processing": return "Đang xử lý";
                    case "completed": return "Hoàn thành";
                    case "rejected": return "Từ chối";
                    case "cancelled": return "Đã hủy";
                    default: return Status;
                }
            }
        }
    }

    /// <summary>
    /// ViewModel cho chi tiết phiếu yêu cầu
    /// </summary>
    public class TransferRequestDetailViewModel
    {
        public long Id { get; set; }
        public long TransferId { get; set; }
        public long ProductVariantId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string VariantName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal LineTotal { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel cho trang chi tiết phiếu yêu cầu
    /// </summary>
    public class TransferRequestFullViewModel
    {
        public TransferRequestListViewModel Header { get; set; }
        public List<TransferRequestDetailViewModel> Details { get; set; } = new List<TransferRequestDetailViewModel>();
    }

    /// <summary>
    /// ViewModel cho form tạo yêu cầu nhập hàng
    /// </summary>
    public class CreateTransferRequestViewModel
    {
        public long FromWarehouseId { get; set; }
        public long ToBranchId { get; set; }
        public string Notes { get; set; } = string.Empty;

        public List<System.Web.Mvc.SelectListItem> WarehouseOptions { get; set; } = new List<System.Web.Mvc.SelectListItem>();
    }

    /// <summary>
    /// ViewModel cho form thêm sản phẩm vào phiếu
    /// </summary>
    public class AddTransferDetailViewModel
    {
        public long TransferId { get; set; }
        public long ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; } = string.Empty;

        public List<System.Web.Mvc.SelectListItem> ProductVariantOptions { get; set; } = new List<System.Web.Mvc.SelectListItem>();
    }
}
