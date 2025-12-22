using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class InventoriesController : AdminBaseController
    {
        private readonly perwEntities _db = new perwEntities();

        /// <summary>
        /// GET: Admin/Inventories - Hiển thị báo cáo tổng tồn kho
        /// </summary>
        public ActionResult Index()
        {
            var report = GetInventorySummaryReport();
            
            // Debug: nếu không có data, thêm thông báo
            if (!report.Any())
            {
                TempData["Warning"] = "Không có dữ liệu tồn kho. Vui lòng chạy script TestData_Inventory.sql để thêm dữ liệu test.";
            }
            
            return View(report);
        }

        /// <summary>
        /// GET: Admin/Inventories/Details - Chi tiết tồn kho theo kho/chi nhánh
        /// </summary>
        public ActionResult Details(string type, string name)
        {
            IList<InventorySnapshotViewModel> details;

            if (type == "warehouse")
            {
                details = GetWarehouseInventoryDetails(name);
            }
            else
            {
                details = GetBranchInventoryDetails(name);
            }

            ViewBag.LocationName = name;
            ViewBag.LocationType = type == "warehouse" ? "Kho Tổng" : "Chi Nhánh";
            return View(details);
        }

        /// <summary>
        /// GET: Admin/Inventories/Adjust
        /// </summary>
        public ActionResult Adjust(long variantId, long? warehouseId)
        {
            var variant = _db.product_variants.Find(variantId);
            if (variant == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Index");
            }

            var inventory = _db.inventories
                .FirstOrDefault(i => i.product_variant_id == variantId && 
                                    (warehouseId == null || i.warehouse_id == warehouseId));

            ViewBag.Title = "Điều chỉnh tồn kho";
            return View(new InventoryAdjustmentViewModel
            {
                VariantId = variantId,
                VariantName = variant.name,
                CurrentQuantity = inventory?.quantity_on_hand ?? 0
            });
        }

        /// <summary>
        /// Lấy báo cáo tổng tồn kho
        /// Logic: Kho Tổng và Chi nhánh là 2 hệ thống RIÊNG BIỆT
        /// - Kho Tổng: Tồn kho tại warehouses (hệ thống kho chính)
        /// - Chi nhánh: Tồn kho tại branches (điểm bán)
        /// KHÔNG cộng 2 hệ thống lại với nhau
        /// </summary>
        private List<TotalInventoryReportViewModel> GetInventorySummaryReport()
        {
            var result = new List<TotalInventoryReportViewModel>();

            try
            {
                // 1. Tồn kho từ WAREHOUSES (Kho Tổng)
                var warehouseInventory = _db.inventories
                    .Where(i => i.deleted_at == null)
                    .GroupBy(i => i.warehouse.name)
                    .Select(g => new TotalInventoryReportViewModel
                    {
                        TenKho = g.Key ?? "Không xác định",
                        LoaiKho = "Kho Tổng",
                        TongSoLuongTon = g.Sum(i => i.quantity_on_hand),
                        HangDatTruoc = g.Sum(i => i.quantity_reserved)
                    })
                    .ToList();

                // 2. Tồn kho từ BRANCHES (Chi nhánh)
                var branchInventory = _db.branch_inventories
                    .GroupBy(bi => bi.branch.name)
                    .Select(g => new TotalInventoryReportViewModel
                    {
                        TenKho = g.Key ?? "Không xác định",
                        LoaiKho = "Chi nhánh",
                        TongSoLuongTon = g.Sum(bi => bi.quantity_on_hand),
                        HangDatTruoc = g.Sum(bi => bi.quantity_reserved)
                    })
                    .ToList();

                // 3. Gộp 2 danh sách (KHÔNG cộng, chỉ hiển thị cùng nhau)
                // Kho Tổng trước, sau đó Chi nhánh
                result.AddRange(warehouseInventory);
                result.AddRange(branchInventory);

                System.Diagnostics.Debug.WriteLine($"Inventory report: {warehouseInventory.Count} warehouses, {branchInventory.Count} branches");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting inventory report: {ex.Message}");
                TempData["Error"] = $"Lỗi khi lấy báo cáo tồn kho: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Lấy chi tiết tồn kho theo kho tổng
        /// </summary>
        private IList<InventorySnapshotViewModel> GetWarehouseInventoryDetails(string warehouseName)
        {
            var warehouse = _db.warehouses.FirstOrDefault(w => w.name == warehouseName);
            if (warehouse == null) return new List<InventorySnapshotViewModel>();

            return _db.inventories
                .Where(i => i.warehouse_id == warehouse.id)
                .Select(i => new InventorySnapshotViewModel
                {
                    VariantId = i.product_variant_id,
                    VariantName = i.product_variants.name,
                    WarehouseName = warehouse.name,
                    QuantityOnHand = i.quantity_on_hand,
                    QuantityReserved = i.quantity_reserved,
                    ReorderLevel = i.reorder_level
                })
                .ToList();
        }

        /// <summary>
        /// Lấy chi tiết tồn kho theo chi nhánh
        /// </summary>
        private IList<InventorySnapshotViewModel> GetBranchInventoryDetails(string branchName)
        {
            var branch = _db.branches.FirstOrDefault(b => b.name == branchName);
            if (branch == null) return new List<InventorySnapshotViewModel>();

            return _db.branch_inventories
                .Where(bi => bi.branch_id == branch.id)
                .Select(bi => new InventorySnapshotViewModel
                {
                    VariantId = bi.product_variant_id,
                    VariantName = bi.product_variants.name,
                    WarehouseName = branch.name,
                    QuantityOnHand = bi.quantity_on_hand,
                    QuantityReserved = bi.quantity_reserved,
                    ReorderLevel = bi.reorder_level
                })
                .ToList();
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
