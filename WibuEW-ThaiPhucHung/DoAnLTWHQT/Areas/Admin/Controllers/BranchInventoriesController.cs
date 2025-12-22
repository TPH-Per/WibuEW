using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class BranchInventoriesController : AdminBaseController
    {
        private readonly perwEntities db = new perwEntities();

        /// <summary>
        /// GET: Admin/BranchInventories - Danh sách tồn kho tất cả chi nhánh
        /// </summary>
        public ActionResult Index(long? branchId = null)
        {
            var query = db.branch_inventories.AsQueryable();

            // Filter theo chi nhánh nếu có
            if (branchId.HasValue)
            {
                query = query.Where(bi => bi.branch_id == branchId.Value);
            }

            var inventories = query
                .Select(bi => new BranchInventoryViewModel
                {
                    BranchId = bi.branch_id,
                    BranchName = bi.branch.name,
                    VariantId = bi.product_variant_id,
                    ProductName = bi.product_variants.product.name,
                    VariantName = bi.product_variants.name,
                    QuantityOnHand = bi.quantity_on_hand,
                    QuantityReserved = bi.quantity_reserved,
                    ReorderLevel = bi.reorder_level
                })
                .OrderBy(bi => bi.BranchName)
                .ThenBy(bi => bi.ProductName)
                .ToList();

            // Lấy danh sách chi nhánh cho dropdown
            ViewBag.Branches = db.branches
                .Select(b => new SelectListItem
                {
                    Value = b.id.ToString(),
                    Text = b.name,
                    Selected = branchId.HasValue && b.id == branchId.Value
                })
                .ToList();

            ViewBag.SelectedBranchId = branchId;

            return View(inventories);
        }

        /// <summary>
        /// GET: Admin/BranchInventories/Details/{branchId} - Chi tiết tồn kho của 1 chi nhánh
        /// </summary>
        public ActionResult Details(long id)
        {
            var branch = db.branches.Find(id);
            if (branch == null)
            {
                return HttpNotFound();
            }

            var inventories = db.branch_inventories
                .Where(bi => bi.branch_id == id)
                .Select(bi => new BranchInventoryViewModel
                {
                    BranchId = bi.branch_id,
                    BranchName = bi.branch.name,
                    VariantId = bi.product_variant_id,
                    ProductName = bi.product_variants.product.name,
                    VariantName = bi.product_variants.name,
                    QuantityOnHand = bi.quantity_on_hand,
                    QuantityReserved = bi.quantity_reserved,
                    ReorderLevel = bi.reorder_level
                })
                .OrderBy(bi => bi.ProductName)
                .ToList();

            ViewBag.BranchName = branch.name;
            ViewBag.BranchLocation = branch.location;
            
            // Thống kê
            ViewBag.TotalSKU = inventories.Count;
            ViewBag.TotalQuantity = inventories.Sum(i => i.QuantityOnHand);
            ViewBag.LowStockCount = inventories.Count(i => i.IsLowStock);

            return View(inventories);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
