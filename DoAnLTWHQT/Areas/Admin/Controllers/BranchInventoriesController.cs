using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class BranchInventoriesController : AdminBaseController
    {
        private readonly perwEntities _db = new perwEntities();

        // GET: Admin/BranchInventories
        public ActionResult Index(long? branchId = null)
        {
            var query = _db.branch_inventories
                .Include(bi => bi.branch)
                .Include(bi => bi.product_variants)
                .Include(bi => bi.product_variants.product)
                .AsQueryable();

            // Filter by branch if specified
            if (branchId.HasValue)
            {
                query = query.Where(bi => bi.branch_id == branchId.Value);
            }

            var inventories = query
                .OrderBy(bi => bi.branch.name)
                .ThenBy(bi => bi.product_variants.product.name)
                .Select(bi => new BranchInventoryViewModel
                {
                    BranchName = bi.branch.name ?? "N/A",
                    Variant = (bi.product_variants.product.name ?? "N/A") + " / " + (bi.product_variants.name ?? "N/A"),
                    QuantityOnHand = bi.quantity_on_hand,
                    QuantityReserved = bi.quantity_reserved
                })
                .ToList();

            // Pass branch list for filter dropdown
            ViewBag.Branches = _db.branches
                .OrderBy(b => b.name)
                .Select(b => new { b.id, b.name })
                .ToList();

            ViewBag.SelectedBranchId = branchId;

            return View(inventories);
        }

        // GET: Admin/BranchInventories/Details/5
        public ActionResult Details(long id)
        {
            var branch = _db.branches
                .Include(b => b.branch_inventories)
                .Include(b => b.branch_inventories.Select(bi => bi.product_variants))
                .Include(b => b.branch_inventories.Select(bi => bi.product_variants.product))
                .FirstOrDefault(b => b.id == id);

            if (branch == null)
            {
                return HttpNotFound();
            }

            var inventories = branch.branch_inventories
                .OrderBy(bi => bi.product_variants.product.name)
                .Select(bi => new BranchInventoryViewModel
                {
                    BranchName = branch.name ?? "N/A",
                    Variant = (bi.product_variants.product.name ?? "N/A") + " / " + (bi.product_variants.name ?? "N/A"),
                    QuantityOnHand = bi.quantity_on_hand,
                    QuantityReserved = bi.quantity_reserved
                })
                .ToList();

            ViewBag.BranchName = branch.name;
            ViewBag.BranchId = branch.id;

            return View(inventories);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
