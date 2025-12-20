using Ltwhqt.ViewModels.Shared;
using System;
using System.Collections.Generic;

namespace Ltwhqt.ViewModels.Warehouse
{
    public class WarehouseDashboardViewModel
    {
        public string WarehouseName { get; set; } = string.Empty;

        public IList<StatisticCardViewModel> Statistics { get; set; } = new List<StatisticCardViewModel>();

        public IList<InventoryTransactionViewModel> RecentTransactions { get; set; } = new List<InventoryTransactionViewModel>();
    }

    public class InventoryTransactionViewModel
    {
        public long Id { get; set; }

        public string Variant { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string Reference { get; set; } = string.Empty;

        public DateTimeOffset HappenedAt { get; set; }

        public string Warehouse { get; set; } = string.Empty;

        public string PerformedBy { get; set; } = string.Empty;
    }
}
