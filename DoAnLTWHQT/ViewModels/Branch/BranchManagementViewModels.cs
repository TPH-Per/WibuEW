using System;
using System.Collections.Generic;
using Ltwhqt.ViewModels.Shared;

namespace Ltwhqt.ViewModels.Branch
{
    public class BranchTransferViewModel
    {
        public long Id { get; set; }

        public string FromWarehouse { get; set; } = string.Empty;

        public string Branch { get; set; } = string.Empty;

        public string Variant { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string Status { get; set; } = "pending";

        public DateTimeOffset CreatedAt { get; set; }

        public string Notes { get; set; } = string.Empty;
    }

    public class BranchInventoryItemViewModel
    {
        public long VariantId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string VariantName { get; set; } = string.Empty;

        public string Sku { get; set; } = string.Empty;

        public int QuantityOnHand { get; set; }

        public int QuantityReserved { get; set; }

        public int ReorderLevel { get; set; }

        public long BranchId { get; set; }

        public string BranchName { get; set; } = string.Empty;

        public bool IsLowStock => QuantityOnHand <= ReorderLevel;

        public int AvailableQuantity => QuantityOnHand - QuantityReserved;
    }

    public class BranchInventoryDetailViewModel
    {
        public BranchInventoryItemViewModel Summary { get; set; } = new BranchInventoryItemViewModel();

        public IList<BranchTransferViewModel> RecentTransfers { get; set; } = new List<BranchTransferViewModel>();
    }

    public class BranchAdjustmentViewModel
    {
        public long VariantId { get; set; }

        public string Variant { get; set; } = string.Empty;

        public int Adjustment { get; set; }

        public string Reason { get; set; } = string.Empty;
    }

    public class BranchOrderListItemViewModel
    {
        public long Id { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public string Customer { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Channel { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }

    public class BranchOrderDetailViewModel
    {
        public BranchOrderListItemViewModel Summary { get; set; } = new BranchOrderListItemViewModel();

        public IList<BranchOrderLineViewModel> Lines { get; set; } = new List<BranchOrderLineViewModel>();

        public IList<BranchPaymentViewModel> Payments { get; set; } = new List<BranchPaymentViewModel>();

        public string ShippingAddress { get; set; } = string.Empty;
    }

    public class BranchOrderLineViewModel
    {
        public string ProductName { get; set; } = string.Empty;

        public string VariantName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }
    }

    public class BranchPaymentViewModel
    {
        public long Id { get; set; }

        public string Method { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
    }

    public class BranchPreOrderViewModel
    {
        public long Id { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public string Customer { get; set; } = string.Empty;

        public string PickupDateLabel { get; set; } = string.Empty;

        public decimal DepositAmount { get; set; }

        public string Status { get; set; } = "preorder";

        public IList<BranchOrderLineViewModel> Lines { get; set; } = new List<BranchOrderLineViewModel>();
    }

    public class BranchDiscountCheckViewModel
    {
        public string Code { get; set; } = string.Empty;

        public bool IsValid { get; set; }

        public string Message { get; set; } = string.Empty;

        public DateTimeOffset? StartAt { get; set; }

        public DateTimeOffset? EndAt { get; set; }

        public decimal? MinOrderAmount { get; set; }

        public int UsedCount { get; set; }

        public int? MaxUses { get; set; }
    }

    public class BranchReportCardViewModel
    {
        public string Title { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public string Trend { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}
