using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Branch;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    public class InventoryController : BranchBaseController
    {
        public ActionResult Index(string filter = "all")
        {
            var items = BuildInventory();
            if (string.Equals(filter, "low", System.StringComparison.OrdinalIgnoreCase))
            {
                items = items.Where(i => i.IsLowStock).ToList();
            }

            ViewBag.Filter = filter;
            return View(items);
        }

        public ActionResult Detail(long variantId)
        {
            var summary = BuildInventory().FirstOrDefault(i => i.VariantId == variantId) ?? BuildInventory().First();
            var vm = new BranchInventoryDetailViewModel
            {
                Summary = summary,
                RecentTransfers = new List<BranchTransferViewModel>
                {
                    new BranchTransferViewModel { Id = 9101, FromWarehouse = "Kho trung tâm", Branch = "Chi nhánh Q1", Variant = summary.VariantName, Quantity = 12, Status = "received", CreatedAt = System.DateTimeOffset.UtcNow.AddDays(-2) }
                }
            };
            return View(vm);
        }

        public ActionResult Adjust(long variantId)
        {
            var item = BuildInventory().FirstOrDefault(i => i.VariantId == variantId) ?? BuildInventory().First();
            return View(new BranchAdjustmentViewModel { VariantId = variantId, Variant = item.VariantName });
        }

        private static List<BranchInventoryItemViewModel> BuildInventory()
        {
            return new List<BranchInventoryItemViewModel>
            {
                new BranchInventoryItemViewModel { VariantId = 101, ProductName = "Sneaker Aurora", VariantName = "Aurora / 39", Sku = "AUR-39", QuantityOnHand = 18, QuantityReserved = 5, ReorderLevel = 8 },
                new BranchInventoryItemViewModel { VariantId = 102, ProductName = "Sneaker Aurora", VariantName = "Aurora / 40", Sku = "AUR-40", QuantityOnHand = 6, QuantityReserved = 3, ReorderLevel = 5 },
                new BranchInventoryItemViewModel { VariantId = 202, ProductName = "Varsity Jacket", VariantName = "Varsity / L", Sku = "VAR-L", QuantityOnHand = 3, QuantityReserved = 1, ReorderLevel = 4 }
            };
        }
    }
}
