using DoAnLTWHQT.ViewModels.Admin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class InventoriesController : AdminBaseController
    {
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
            {
                // 1. Luôn luôn lấy báo cáo tổng (để hiện bảng bên trên)
                var report = GetInventorySummaryReport();

                // 2. Kiểm tra nếu có yêu cầu xem chi tiết (khi bấm nút Xem nhanh)
                if (!string.IsNullOrEmpty(searchLocation))
                {
                    // Gọi hàm lấy dữ liệu chi tiết tùy theo loại kho
                    if (type == "Kho Tổng")
                    {
                        ViewBag.DetailedInventory = GetWarehouseInventoryDetails(searchLocation);
                    }
                    else
                    {
                        ViewBag.DetailedInventory = GetBranchInventoryDetails(searchLocation);
                    }

                    // Lưu tên kho đang xem để hiện lên tiêu đề bảng xanh
                    ViewBag.SelectedLocation = searchLocation;
                }

                // Trả về View chính (không dùng PartialView)
                return View(report);
            }
        }

        /// <summary>
        /// GET: Admin/Inventories/Details - Chi tiết tồn kho theo kho/chi nhánh
        /// </summary>
        public ActionResult Details(long id)
        {
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

        /// <summary>
        /// Gọi stored procedure sp_BaoCaoTongTonKhoDonGian
        /// </summary>
        private List<TotalInventoryReportViewModel> GetInventorySummaryReport()
        {
            var result = new List<TotalInventoryReportViewModel>();

            try
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    System.Diagnostics.Debug.WriteLine("Connection string PerwDbContext is null or empty!");
                    return result;
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_BaoCaoTongTonKhoDonGian", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;
                        
                        connection.Open();
                        
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new TotalInventoryReportViewModel
                                {
                                    TenKho = reader["Tên Kho"]?.ToString() ?? string.Empty,
                                    LoaiKho = reader["Loại Kho"]?.ToString() ?? string.Empty,
                                    TongSoLuongTon = reader["Tổng Số Lượng Tồn"] != DBNull.Value 
                                        ? Convert.ToInt32(reader["Tổng Số Lượng Tồn"]) 
                                        : 0,
                                    HangDatTruoc = reader["Hàng Đặt Trước"] != DBNull.Value 
                                        ? Convert.ToInt32(reader["Hàng Đặt Trước"]) 
                                        : 0
                                });
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"sp_BaoCaoTongTonKhoDonGian returned {result.Count} rows");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calling sp_BaoCaoTongTonKhoDonGian: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Lưu lỗi để hiển thị
                TempData["Error"] = $"Lỗi khi gọi stored procedure: {ex.Message}";
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
