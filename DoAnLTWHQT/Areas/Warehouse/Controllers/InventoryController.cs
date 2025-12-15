using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Warehouse;

namespace DoAnLTWHQT.Areas.Warehouse.Controllers
{
    public class InventoryController : WarehouseBaseController
    {
        private readonly Entities _db = new Entities();

        /// <summary>
        /// GET: Warehouse/Inventory - Danh sách tồn kho (sử dụng ORM)
        /// </summary>
        public ActionResult Index(string filter = "all", long? warehouseId = null)
        {
            // Lấy danh sách kho để filter
            ViewBag.Warehouses = _db.warehouses
                .Where(w => w.deleted_at == null)
                .Select(w => new SelectListItem
                {
                    Value = w.id.ToString(),
                    Text = w.name,
                    Selected = warehouseId.HasValue && w.id == warehouseId.Value
                }).ToList();

            // Query tồn kho từ database
            var query = _db.inventories
                .Include(i => i.product_variants)
                .Include(i => i.product_variants.product)
                .Include(i => i.warehouse)
                .Where(i => i.product_variants.deleted_at == null);

            // Filter theo warehouse
            if (warehouseId.HasValue)
            {
                query = query.Where(i => i.warehouse_id == warehouseId.Value);
            }

            var inventoryData = query.ToList();

            // Map sang ViewModel
            var items = inventoryData.Select(i => new WarehouseInventoryItemViewModel
            {
                VariantId = i.product_variant_id,
                ProductName = i.product_variants != null && i.product_variants.product != null 
                    ? i.product_variants.product.name : "N/A",
                VariantName = i.product_variants != null ? i.product_variants.name : "N/A",
                Sku = i.product_variants != null ? i.product_variants.sku : "N/A",
                QuantityOnHand = i.quantity_on_hand,
                QuantityReserved = i.quantity_reserved,
                ReorderLevel = i.reorder_level,
                WarehouseId = i.warehouse_id,
                WarehouseName = i.warehouse != null ? i.warehouse.name : "N/A"
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
            ViewBag.WarehouseId = warehouseId;
            ViewBag.TotalItems = items.Count;
            ViewBag.LowStockCount = items.Count(i => i.IsLowStock);
            ViewBag.ZeroStockCount = items.Count(i => i.QuantityOnHand == 0);

            return View(items);
        }

        /// <summary>
        /// GET: Warehouse/Inventory/Detail - Chi tiết tồn kho sản phẩm
        /// </summary>
        public ActionResult Detail(long variantId, long? warehouseId = null)
        {
            // Lấy thông tin tồn kho
            var inventoryQuery = _db.inventories
                .Include(i => i.product_variants)
                .Include(i => i.product_variants.product)
                .Include(i => i.warehouse)
                .Where(i => i.product_variant_id == variantId);

            if (warehouseId.HasValue)
            {
                inventoryQuery = inventoryQuery.Where(i => i.warehouse_id == warehouseId.Value);
            }

            var inventory = inventoryQuery.FirstOrDefault();

            if (inventory == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin tồn kho.";
                return RedirectToAction("Index");
            }

            var summary = new WarehouseInventoryItemViewModel
            {
                VariantId = inventory.product_variant_id,
                ProductName = inventory.product_variants != null && inventory.product_variants.product != null 
                    ? inventory.product_variants.product.name : "N/A",
                VariantName = inventory.product_variants != null ? inventory.product_variants.name : "N/A",
                Sku = inventory.product_variants != null ? inventory.product_variants.sku : "N/A",
                QuantityOnHand = inventory.quantity_on_hand,
                QuantityReserved = inventory.quantity_reserved,
                ReorderLevel = inventory.reorder_level,
                WarehouseId = inventory.warehouse_id,
                WarehouseName = inventory.warehouse != null ? inventory.warehouse.name : "N/A"
            };

            // Lấy lịch sử giao dịch (inventory_transactions)
            var transactions = _db.inventory_transactions
                .Include(t => t.product_variants)
                .Where(t => t.product_variant_id == variantId)
                .OrderByDescending(t => t.created_at)
                .Take(20)
                .ToList()
                .Select(t => new WarehouseTransactionViewModel
                {
                    Id = t.id,
                    Type = t.type ?? "unknown",
                    Variant = t.product_variants != null ? t.product_variants.name : "N/A",
                    Quantity = t.quantity,
                    Reference = t.order_id.HasValue ? "Order #" + t.order_id.Value : "",
                    OccurredAt = t.created_at.HasValue 
                        ? new System.DateTimeOffset(t.created_at.Value) 
                        : System.DateTimeOffset.UtcNow,
                    PerformedBy = "system",
                    Notes = t.notes ?? ""
                }).ToList();

            // Lấy danh sách đặt trước (nếu có)
            var reservations = new List<WarehouseReservationViewModel>();

            var vm = new WarehouseInventoryDetailViewModel
            {
                Summary = summary,
                Transactions = transactions,
                Reservations = reservations
            };

            return View(vm);
        }

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
