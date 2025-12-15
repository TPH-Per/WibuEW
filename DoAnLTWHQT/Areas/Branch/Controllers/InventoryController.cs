using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Branch;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    public class InventoryController : BranchBaseController
    {
        private readonly Entities _db = new Entities();

        /// <summary>
        /// GET: Branch/Inventory - Danh sách tồn kho chi nhánh (sử dụng ORM)
        /// </summary>
        public ActionResult Index(string filter = "all")
        {
            long branchId = GetCurrentBranchId();
            var branch = _db.branches.Find(branchId);

            // Query tồn kho chi nhánh từ database
            var inventoryData = _db.branch_inventories
                .Include(i => i.product_variants)
                .Include(i => i.product_variants.product)
                .Include(i => i.branch)
                .Where(i => i.branch_id == branchId)
                .Where(i => i.product_variants.deleted_at == null)
                .ToList();

            // Map sang ViewModel
            var items = inventoryData.Select(i => new BranchInventoryItemViewModel
            {
                VariantId = i.product_variant_id,
                ProductName = i.product_variants != null && i.product_variants.product != null 
                    ? i.product_variants.product.name : "N/A",
                VariantName = i.product_variants != null ? i.product_variants.name : "N/A",
                Sku = i.product_variants != null ? i.product_variants.sku : "N/A",
                QuantityOnHand = i.quantity_on_hand,
                QuantityReserved = i.quantity_reserved,
                ReorderLevel = i.reorder_level,
                BranchId = i.branch_id,
                BranchName = i.branch != null ? i.branch.name : "N/A"
            }).ToList();

            // Filter theo trạng thái
            if (string.Equals(filter, "low", System.StringComparison.OrdinalIgnoreCase))
            {
                items = items.Where(i => i.IsLowStock).ToList();
            }
            else if (string.Equals(filter, "zero", System.StringComparison.OrdinalIgnoreCase))
            {
                items = items.Where(i => i.QuantityOnHand == 0).ToList();
            }

            ViewBag.Filter = filter;
            ViewBag.BranchId = branchId;
            ViewBag.BranchName = branch != null ? branch.name : "Chi nhánh";
            ViewBag.TotalItems = items.Count;
            ViewBag.LowStockCount = items.Count(i => i.IsLowStock);
            ViewBag.ZeroStockCount = items.Count(i => i.QuantityOnHand == 0);

            return View(items);
        }

        /// <summary>
        /// GET: Branch/Inventory/Detail - Chi tiết tồn kho sản phẩm của chi nhánh
        /// </summary>
        public ActionResult Detail(long variantId)
        {
            long branchId = GetCurrentBranchId();

            // Lấy thông tin tồn kho
            var inventory = _db.branch_inventories
                .Include(i => i.product_variants)
                .Include(i => i.product_variants.product)
                .Include(i => i.branch)
                .FirstOrDefault(i => i.product_variant_id == variantId && i.branch_id == branchId);

            if (inventory == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin tồn kho.";
                return RedirectToAction("Index");
            }

            var summary = new BranchInventoryItemViewModel
            {
                VariantId = inventory.product_variant_id,
                ProductName = inventory.product_variants != null && inventory.product_variants.product != null 
                    ? inventory.product_variants.product.name : "N/A",
                VariantName = inventory.product_variants != null ? inventory.product_variants.name : "N/A",
                Sku = inventory.product_variants != null ? inventory.product_variants.sku : "N/A",
                QuantityOnHand = inventory.quantity_on_hand,
                QuantityReserved = inventory.quantity_reserved,
                ReorderLevel = inventory.reorder_level,
                BranchId = inventory.branch_id,
                BranchName = inventory.branch != null ? inventory.branch.name : "N/A"
            };

            // Lấy lịch sử nhập hàng từ warehouse
            var recentTransfers = _db.warehouse_transfers
                .Include(t => t.warehouse)
                .Include(t => t.branch)
                .Include(t => t.warehouse_transfer_details)
                .Where(t => t.to_branch_id == branchId)
                .Where(t => t.warehouse_transfer_details.Any(d => d.product_variant_id == variantId))
                .OrderByDescending(t => t.created_at)
                .Take(10)
                .ToList()
                .Select(t => new BranchTransferViewModel
                {
                    Id = t.id,
                    FromWarehouse = t.warehouse != null ? t.warehouse.name : "N/A",
                    Branch = t.branch != null ? t.branch.name : "N/A",
                    Variant = summary.VariantName,
                    Quantity = t.warehouse_transfer_details
                        .Where(d => d.product_variant_id == variantId)
                        .Sum(d => d.quantity),
                    Status = t.status ?? "unknown",
                    CreatedAt = t.created_at.HasValue 
                        ? new System.DateTimeOffset(t.created_at.Value) 
                        : System.DateTimeOffset.UtcNow,
                    Notes = t.notes ?? ""
                }).ToList();

            var vm = new BranchInventoryDetailViewModel
            {
                Summary = summary,
                RecentTransfers = recentTransfers
            };

            return View(vm);
        }

        /// <summary>
        /// GET: Branch/Inventory/Adjust - Form điều chỉnh tồn kho
        /// </summary>
        public ActionResult Adjust(long variantId)
        {
            long branchId = GetCurrentBranchId();

            var inventory = _db.branch_inventories
                .Include(i => i.product_variants)
                .FirstOrDefault(i => i.product_variant_id == variantId && i.branch_id == branchId);

            if (inventory == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin tồn kho.";
                return RedirectToAction("Index");
            }

            return View(new BranchAdjustmentViewModel 
            { 
                VariantId = variantId, 
                Variant = inventory.product_variants != null ? inventory.product_variants.name : "N/A"
            });
        }

        #region Helper Methods

        private long GetCurrentBranchId()
        {
            if (Session["BranchId"] != null)
            {
                return System.Convert.ToInt64(Session["BranchId"]);
            }
            
            var firstBranch = _db.branches.FirstOrDefault();
            return firstBranch != null ? firstBranch.id : 1;
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
