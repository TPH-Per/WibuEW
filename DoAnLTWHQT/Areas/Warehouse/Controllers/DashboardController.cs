using Ltwhqt.ViewModels.Shared;
using Ltwhqt.ViewModels.Warehouse;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Warehouse.Controllers
{
    public class DashboardController : WarehouseBaseController
    {
        public ActionResult Index()
        {
            var now = DateTimeOffset.Now;
            var vm = new WarehouseDashboardViewModel
            {
                WarehouseName = "Kho trung tâm",
                Statistics = new List<StatisticCardViewModel>
                {
                    new StatisticCardViewModel { Label = "T?n kho kh? d?ng", Value = "42.180 sp", SubLabel = "Tang 3% so v?i hôm qua", Icon = "bi-box-seam", Context = "light", Trend = "+3%" },
                    new StatisticCardViewModel { Label = "Ðon xu?t trong ngày", Value = "68", SubLabel = "Chi nhánh yêu c?u", Icon = "bi-arrow-up-right-square", Context = "primary", Trend = "+5%" },
                    new StatisticCardViewModel { Label = "Phi?u nh?p NCC", Value = "12", SubLabel = "Ch? ki?m d?nh", Icon = "bi-truck", Context = "success", Trend = "3 dang ch?" },
                    new StatisticCardViewModel { Label = "SKU c?nh báo", Value = "18", SubLabel = "Du?i m?c reorder", Icon = "bi-exclamation-triangle", Context = "danger", Trend = "C?n x? lý" }
                },
                RecentTransactions = new List<InventoryTransactionViewModel>
                {
                    new InventoryTransactionViewModel { Id = 1, Variant = "Sneaker Aurora / M", Type = "outbound", Quantity = 24, Reference = "Transfer #245", HappenedAt = now.AddMinutes(-25), Warehouse = "Kho trung tâm", PerformedBy = User?.Identity?.Name ?? "system" },
                    new InventoryTransactionViewModel { Id = 2, Variant = "Áo khoác Varsity / L", Type = "inbound", Quantity = 60, Reference = "Shipment #1120", HappenedAt = now.AddHours(-2), Warehouse = "Kho trung tâm", PerformedBy = "warehouse_bot" },
                    new InventoryTransactionViewModel { Id = 3, Variant = "Balo Transit / Ðen", Type = "adjustment", Quantity = -3, Reference = "Damage check", HappenedAt = now.AddHours(-5), Warehouse = "Kho trung tâm", PerformedBy = "kho.thang" }
                }
            };

            return View(vm);
        }
    }
}
