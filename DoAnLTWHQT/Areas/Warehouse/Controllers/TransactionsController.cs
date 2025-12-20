using System.Collections.Generic;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Warehouse;

namespace DoAnLTWHQT.Areas.Warehouse.Controllers
{
    public class TransactionsController : WarehouseBaseController
    {
        public ActionResult Index(string type = "all")
        {
            var transactions = BuildTransactions();
            if (!string.Equals(type, "all", System.StringComparison.OrdinalIgnoreCase))
            {
                transactions = transactions.FindAll(t => string.Equals(t.Type, type, System.StringComparison.OrdinalIgnoreCase));
            }

            ViewBag.TypeFilter = type;
            return View(transactions);
        }

        private static List<WarehouseTransactionViewModel> BuildTransactions()
        {
            return new List<WarehouseTransactionViewModel>
            {
                new WarehouseTransactionViewModel { Id = 1, Type = "inbound", Variant = "Aurora / 39", Quantity = 60, Reference = "Shipment #1002", OccurredAt = System.DateTimeOffset.UtcNow.AddDays(-1), PerformedBy = "warehouse.thuy" },
                new WarehouseTransactionViewModel { Id = 2, Type = "outbound", Variant = "Aurora / 39", Quantity = 24, Reference = "Transfer #8001", OccurredAt = System.DateTimeOffset.UtcNow.AddHours(-4), PerformedBy = "warehouse.bot" },
                new WarehouseTransactionViewModel { Id = 3, Type = "adjustment", Variant = "Varsity / L", Quantity = -2, Reference = "Damage report", OccurredAt = System.DateTimeOffset.UtcNow.AddHours(-8), PerformedBy = "warehouse.thuy", Notes = "Rách vải" }
            };
        }
    }
}
