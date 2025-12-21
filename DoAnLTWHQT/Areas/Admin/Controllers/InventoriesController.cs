using DoAnLTWHQT.ViewModels.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class InventoriesController : AdminBaseController
    {
        public ActionResult Index()
        {
            return View(BuildInventory());
        private readonly perwEntities _db = new perwEntities();
        private readonly string _connectionString;

        public InventoriesController()
        {
            // Lấy connection string từ Web.config
            _connectionString = ConfigurationManager.ConnectionStrings["PerwDbContext"]?.ConnectionString;
        }

        /// <summary>
        /// GET: Admin/Inventories - Hiển thị báo cáo tổng tồn kho
        /// </summary>
        public ActionResult Index(string searchLocation = null, string type = null)
        {
            var report = GetInventorySummaryReport();
            if (!string.IsNullOrEmpty(searchLocation))
            {
                ViewBag.DetailedInventory = (type == "Kho Tổng") ? GetWarehouseInventoryDetails(searchLocation) : GetBranchInventoryDetails(searchLocation);
                ViewBag.SelectedLocation = searchLocation;
            }
            return View(report);
        }

        public ActionResult Adjust(long variantId)
        {
            var variant = BuildInventory().FirstOrDefault(v => v.VariantId == variantId) ?? BuildInventory().First();
            ViewBag.Title = "Điều chỉnh tồn kho";
            return View(new InventoryAdjustmentViewModel
            {
                VariantId = variantId,
                VariantName = variant.VariantName,
                CurrentQuantity = variant.QuantityOnHand
            });
        }
            // 1. Lấy thông tin tồn kho hiện tại
            var inventoryItem = _db.inventories
                .Include(i => i.product_variants)
                .Include(i => i.product_variants.product)
                .Include(i => i.warehouse)
                .FirstOrDefault(i => i.id == id);

            if (inventoryItem == null)
            {
                return HttpNotFound();
            }

            // 2. Tìm các giao dịch liên quan (Cùng Kho và Cùng Sản phẩm)
            var transactions = _db.inventory_transactions
                .Where(t => t.warehouse_id == inventoryItem.warehouse_id
                         && t.product_variant_id == inventoryItem.product_variant_id)
                .OrderByDescending(t => t.created_at) // Mới nhất lên đầu
                .ToList();

            // 3. Đóng gói vào ViewModel
            var viewModel = new InventoryDetailsViewModel
            {
                Inventory = inventoryItem,
                Transactions = transactions
            };

            return View(viewModel);
        }

        // GET: Admin/Inventories/LocationDetails
        public ActionResult LocationDetails(string name, string type)
        {
            IList<InventorySnapshotViewModel> details;

            if (type == "Kho Tổng")
            {
                // Hàm này bạn đã có trong code cũ
                details = GetWarehouseInventoryDetails(name);
            }
            else
            {
                // Hàm này bạn đã có trong code cũ
                details = GetBranchInventoryDetails(name);
            }

            ViewBag.LocationName = name;
            ViewBag.LocationType = type;

            // Bạn cần tạo View LocationDetails.cshtml để hiển thị list này
            return View(details);
        }

        /// <summary>
        /// GET: Admin/Inventories/Adjust
        /// </summary>

        private static IList<InventorySnapshotViewModel> BuildInventory()
        {
            return new List<InventorySnapshotViewModel>
            {
                new InventorySnapshotViewModel { VariantId = 101, VariantName = "Sneaker Aurora / 39", WarehouseName = "Kho trung tâm", QuantityOnHand = 52, QuantityReserved = 6, ReorderLevel = 20 },
                new InventorySnapshotViewModel { VariantId = 202, VariantName = "Áo khoác Varsity / L", WarehouseName = "Kho trung tâm", QuantityOnHand = 18, QuantityReserved = 10, ReorderLevel = 25 },
                new InventorySnapshotViewModel { VariantId = 303, VariantName = "Balo Transit / Đen", WarehouseName = "Kho trung tâm", QuantityOnHand = 8, QuantityReserved = 2, ReorderLevel = 10 }
            };
        }
    }
}
