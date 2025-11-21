using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Warehouse;

namespace DoAnLTWHQT.Areas.Warehouse.Controllers
{
    public class InventoryController : WarehouseBaseController
    {
        public ActionResult Index(string filter = "all")
        {
            var items = BuildInventory();
            if (string.Equals(filter, "low", System.StringComparison.OrdinalIgnoreCase))
            {
                items = items.Where(i => i.IsLowStock).ToList();
            }

            if (string.Equals(filter, "zero", System.StringComparison.OrdinalIgnoreCase))
            {
                items = items.Where(i => i.QuantityOnHand == 0).ToList();
            }

            ViewBag.Filter = filter;
            return View(items);
        }

        public ActionResult Detail(long variantId)
        {
            var summary = BuildInventory().FirstOrDefault(i => i.VariantId == variantId) ?? BuildInventory().First();
            var vm = new WarehouseInventoryDetailViewModel
            {
                Summary = summary,
                Transactions = BuildTransactions().Where(t => t.Variant.Contains(summary.VariantName)).ToList(),
                Reservations = BuildReservations().Where(r => r.Variant.Contains(summary.VariantName)).ToList()
            };
            return View(vm);
        }

        private static List<WarehouseInventoryItemViewModel> BuildInventory()
        {
            return new List<WarehouseInventoryItemViewModel>
            {
                new WarehouseInventoryItemViewModel { VariantId = 101, ProductName = "Sneaker Aurora", VariantName = "Aurora / 39", Sku = "AUR-39", QuantityOnHand = 48, QuantityReserved = 10, ReorderLevel = 30 },
                new WarehouseInventoryItemViewModel { VariantId = 102, ProductName = "Sneaker Aurora", VariantName = "Aurora / 40", Sku = "AUR-40", QuantityOnHand = 18, QuantityReserved = 12, ReorderLevel = 25 },
                new WarehouseInventoryItemViewModel { VariantId = 202, ProductName = "Áo khoác Varsity", VariantName = "Varsity / L", Sku = "VAR-L", QuantityOnHand = 6, QuantityReserved = 8, ReorderLevel = 15 }
            };
        }

        private static List<WarehouseTransactionViewModel> BuildTransactions()
        {
            return new List<WarehouseTransactionViewModel>
            {
                new WarehouseTransactionViewModel { Id = 1, Type = "inbound", Variant = "Aurora / 39", Quantity = 60, Reference = "Shipment #1002", OccurredAt = System.DateTimeOffset.UtcNow.AddDays(-1), PerformedBy = "warehouse.thuy" },
                new WarehouseTransactionViewModel { Id = 2, Type = "outbound", Variant = "Aurora / 39", Quantity = 24, Reference = "Transfer #701", OccurredAt = System.DateTimeOffset.UtcNow.AddHours(-4), PerformedBy = "warehouse.bot" },
                new WarehouseTransactionViewModel { Id = 3, Type = "adjustment", Variant = "Varsity / L", Quantity = -2, Reference = "Damage check", OccurredAt = System.DateTimeOffset.UtcNow.AddHours(-8), PerformedBy = "warehouse.thuy", Notes = "Hàng lỗi chỉ may" }
            };
        }

        private static List<WarehouseReservationViewModel> BuildReservations()
        {
            return new List<WarehouseReservationViewModel>
            {
                new WarehouseReservationViewModel { OrderId = 9001, OrderCode = "PO24001", Channel = "Online", Variant = "Aurora / 39", ReservedQuantity = 4, Status = "processing", ReservedAt = System.DateTimeOffset.UtcNow.AddHours(-3) },
                new WarehouseReservationViewModel { OrderId = 9004, OrderCode = "PR24005", Channel = "Pre-order", Variant = "Aurora / 39", ReservedQuantity = 6, Status = "pending", ReservedAt = System.DateTimeOffset.UtcNow.AddHours(-6) }
            };
        }
    }
}
