using System;
using System.Collections.Generic;
using Ltwhqt.ViewModels.Shared;

namespace Ltwhqt.ViewModels.Warehouse
{
    public class WarehouseShipmentViewModel
    {
        public long Id { get; set; }

        public string Supplier { get; set; } = string.Empty;

        public string Warehouse { get; set; } = string.Empty;

        public string Variant { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string Status { get; set; } = "pending";

        public DateTimeOffset ExpectedAt { get; set; }

        public DateTimeOffset? ReceivedAt { get; set; }

        public string Notes { get; set; } = string.Empty;
    }

    public class WarehouseShipmentFormViewModel
    {
        public IEnumerable<SelectOptionViewModel> SupplierOptions { get; set; } = new List<SelectOptionViewModel>();

        public IEnumerable<SelectOptionViewModel> VariantOptions { get; set; } = new List<SelectOptionViewModel>();

        public IEnumerable<SelectOptionViewModel> WarehouseOptions { get; set; } = new List<SelectOptionViewModel>();

        public DateTimeOffset ExpectedAt { get; set; } = DateTimeOffset.Now;

        public int Quantity { get; set; } = 10;
    }

    public class WarehouseInventoryItemViewModel
    {
        public long VariantId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string VariantName { get; set; } = string.Empty;

        public string Sku { get; set; } = string.Empty;

        public int QuantityOnHand { get; set; }

        public int QuantityReserved { get; set; }

        public int ReorderLevel { get; set; }

        public bool IsLowStock => QuantityOnHand <= ReorderLevel;
    }

    public class WarehouseInventoryDetailViewModel
    {
        public WarehouseInventoryItemViewModel Summary { get; set; } = new WarehouseInventoryItemViewModel();

        public IList<WarehouseTransactionViewModel> Transactions { get; set; } = new List<WarehouseTransactionViewModel>();

        public IList<WarehouseReservationViewModel> Reservations { get; set; } = new List<WarehouseReservationViewModel>();
    }

    public class WarehouseAdjustmentViewModel
    {
        public long VariantId { get; set; }

        public string Variant { get; set; } = string.Empty;

        public int CurrentQuantity { get; set; }

        public int Adjustment { get; set; }

        public string Reason { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        public string CreatedBy { get; set; } = string.Empty;
    }

    public class WarehouseTransactionViewModel
    {
        public long Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Variant { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string Reference { get; set; } = string.Empty;

        public DateTimeOffset OccurredAt { get; set; }

        public string PerformedBy { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;
    }

    public class WarehouseReservationViewModel
    {
        public long OrderId { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public string Channel { get; set; } = string.Empty;

        public string Variant { get; set; } = string.Empty;

        public int ReservedQuantity { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTimeOffset ReservedAt { get; set; }
    }
}
