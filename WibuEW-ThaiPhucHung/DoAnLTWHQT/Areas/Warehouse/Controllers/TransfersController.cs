using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace DoAnLTWHQT.Areas.Warehouse.Controllers
{
    public class TransfersController : WarehouseBaseController
    {
        private readonly string _connectionString;
        private perwEntities db = new perwEntities();

        public TransfersController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["PerwDbContext"]?.ConnectionString;
        }

        // GET: Warehouse/Transfers
        public ActionResult Index(string status = "all")
        {
            var transfers = GetAllTransfersViaSP();
            
            if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                transfers = transfers.Where(t => string.Equals(t.status, status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.StatusFilter = status;
            
            return View(transfers);
        }

        // GET: Warehouse/Transfers/Create
        public ActionResult Create()
        {
            ViewBag.Warehouses = GetWarehouseOptions();
            ViewBag.Branches = GetBranchOptions();
            ViewBag.ProductVariants = GetProductVariantOptions();
            
            return View();
        }

        // POST: Warehouse/Transfers/Create
        // SỬ DỤNG STORED PROCEDURE: sp_Warehouse_CreateTransfer và sp_Warehouse_AddTransferDetail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(long branchId, string detailsJson, string notes = null)
        {
            try
            {
                // Mặc định warehouse ID = 1 (Kho tổng)
                const long defaultWarehouseId = 1;
                
                if (string.IsNullOrEmpty(detailsJson))
                {
                    TempData["Error"] = "Vui lòng thêm ít nhất một sản phẩm.";
                    return RedirectToAction("Create");
                }

                var details = JsonConvert.DeserializeObject<List<TransferDetailItem>>(detailsJson);
                
                if (details == null || !details.Any())
                {
                    TempData["Error"] = "Dữ liệu sản phẩm không hợp lệ.";
                    return RedirectToAction("Create");
                }

                // SỬ DỤNG STORED PROCEDURE ĐỂ TẠO PHIẾU - Mặc định status = Shipping
                long newTransferId = CreateTransferViaSP(defaultWarehouseId, branchId, "Shipping", notes);

                if (newTransferId <= 0)
                {
                    TempData["Error"] = "Không thể tạo phiếu điều chuyển.";
                    return RedirectToAction("Create");
                }

                // SỬ DỤNG STORED PROCEDURE ĐỂ THÊM CHI TIẾT SẢN PHẨM
                foreach (var item in details)
                {
                    AddTransferDetailViaSP(newTransferId, item.ProductVariantId, item.Quantity, item.Notes);
                }

                TempData["Success"] = $"Tạo phiếu chuyển kho #{newTransferId} thành công!";
                return RedirectToAction("Details", new { id = newTransferId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Create");
            }
        }

        // GET: Warehouse/Transfers/Details/5
        public ActionResult Details(long id)
        {
            var transfer = db.warehouse_transfers
                .Include("warehouse")
                .Include("branch")
                .Include("warehouse_transfer_details.product_variants")
                .FirstOrDefault(t => t.id == id);

            if (transfer == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu chuyển kho.";
                return RedirectToAction("Index");
            }

            return View(transfer);
        }

        // POST: Warehouse/Transfers/UpdateStatus
        // SỬ DỤNG STORED PROCEDURE: sp_Warehouse_UpdateTransferStatus
        [HttpPost]
        public ActionResult UpdateStatus(long id, string newStatus)
        {
            try
            {
                UpdateTransferStatusViaSP(id, newStatus, null);
                return Json(new { success = true, message = $"Đã cập nhật trạng thái thành {newStatus}." });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.InnerException?.Message 
                    ?? ex.InnerException?.Message 
                    ?? ex.Message;
                return Json(new { success = false, message = "Lỗi: " + msg });
            }
        }

        // POST: Warehouse/Transfers/AutoDeliver
        [HttpPost]
        public ActionResult AutoDeliver(long id, bool autoComplete = true)
        {
            try
            {
                // Execute stored procedure
                var result = db.Database.ExecuteSqlCommand(
                    "EXEC sp_Auto_DeliverTransfer @TransferID, @AutoComplete",
                    new System.Data.SqlClient.SqlParameter("@TransferID", id),
                    new System.Data.SqlClient.SqlParameter("@AutoComplete", autoComplete)
                );

                var message = autoComplete 
                    ? "✅ Giao hàng thành công! Hàng đã đến chi nhánh." 
                    : "📦 Đã xuất hàng! Phiếu đang trên đường giao.";

                return Json(new { success = true, message = message });
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbEx)
            {
                var innerMsg = dbEx.InnerException?.InnerException?.Message 
                    ?? dbEx.InnerException?.Message 
                    ?? dbEx.Message;
                return Json(new { success = false, message = "DB Error: " + innerMsg });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.InnerException?.Message 
                    ?? ex.InnerException?.Message 
                    ?? ex.Message;
                return Json(new { success = false, message = "Lỗi: " + msg });
            }
        }

        // POST: Warehouse/Transfers/ApproveToShipping
        // Duyệt phiếu Requested → Shipping
        [HttpPost]
        public ActionResult ApproveToShipping(long id, string notes = null)
        {
            try
            {
                ApproveRequestToShippingViaSP(id, notes);
                return Json(new { success = true, message = "✅ Đã duyệt yêu cầu! Phiếu chuyển sang trạng thái Shipping." });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.InnerException?.Message 
                    ?? ex.InnerException?.Message 
                    ?? ex.Message;
                return Json(new { success = false, message = "Lỗi: " + msg });
            }
        }

        // POST: Warehouse/Transfers/Complete
        // SỬ DỤNG STORED PROCEDURE: sp_Warehouse_CompleteTransfer
        [HttpPost]
        public ActionResult Complete(long id, string notes = null)
        {
            try
            {
                CompleteTransferViaSP(id, notes);
                return Json(new { success = true, message = "✅ Đã hoàn thành phiếu và cập nhật tồn kho!" });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.InnerException?.Message 
                    ?? ex.InnerException?.Message 
                    ?? ex.Message;
                return Json(new { success = false, message = "Lỗi: " + msg });
            }
        }

        // POST: Warehouse/Transfers/CancelRequest
        // Hủy phiếu Requested
        [HttpPost]
        public ActionResult CancelRequest(long id, string reason)
        {
            try
            {
                if (string.IsNullOrEmpty(reason))
                {
                    return Json(new { success = false, message = "Vui lòng nhập lý do hủy." });
                }

                CancelRequestViaSP(id, reason);
                return Json(new { success = true, message = "✅ Đã hủy phiếu yêu cầu." });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.InnerException?.Message 
                    ?? ex.InnerException?.Message 
                    ?? ex.Message;
                return Json(new { success = false, message = "Lỗi: " + msg });
            }
        }

        // POST: Warehouse/Transfers/RemoveDetail
        // Xóa sản phẩm khỏi phiếu (chỉ cho phép khi phiếu ở Requested/Pending)
        [HttpPost]
        public JsonResult RemoveDetail(long detailId)
        {
            try
            {
                var detail = db.warehouse_transfer_details
                    .Include("warehouse_transfers")
                    .FirstOrDefault(d => d.id == detailId);

                if (detail == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy chi tiết sản phẩm." });
                }

                // Chỉ cho phép xóa khi phiếu ở Requested hoặc Pending
                var status = detail.warehouse_transfers.status;
                if (status != "Requested" && status != "Pending")
                {
                    return Json(new { success = false, message = "Chỉ có thể xóa sản phẩm khi phiếu ở trạng thái Requested hoặc Pending." });
                }

                db.warehouse_transfer_details.Remove(detail);
                db.SaveChanges();

                return Json(new { success = true, message = "Đã xóa sản phẩm khỏi phiếu." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // GET: Ajax - Kiểm tra tồn kho
        [HttpGet]
        public JsonResult CheckInventory(long warehouseId, long variantId)
        {
            var inventory = db.inventories
                .FirstOrDefault(i => i.warehouse_id == warehouseId 
                    && i.product_variant_id == variantId);

            var available = inventory?.quantity_on_hand ?? 0;

            return Json(new { 
                available = available,
                productName = inventory?.product_variants?.name ?? "N/A"
            }, JsonRequestBehavior.AllowGet);
        }

        #region Stored Procedure Calls via ADO.NET

        /// <summary>
        /// Gọi sp_Warehouse_CreateTransfer để tạo phiếu điều chuyển
        /// </summary>
        private long CreateTransferViaSP(long warehouseId, long branchId, string status, string notes)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("sp_Warehouse_CreateTransfer", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@from_warehouse_id", warehouseId);
                    cmd.Parameters.AddWithValue("@to_branch_id", branchId);
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@notes", (object)notes ?? DBNull.Value);
                    
                    var outputParam = new SqlParameter("@new_transfer_id", SqlDbType.BigInt)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    return outputParam.Value != DBNull.Value ? Convert.ToInt64(outputParam.Value) : 0;
                }
            }
        }

        /// <summary>
        /// Gọi sp_Warehouse_AddTransferDetail để thêm chi tiết sản phẩm
        /// </summary>
        private void AddTransferDetailViaSP(long transferId, long productVariantId, int quantity, string notes)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("sp_Warehouse_AddTransferDetail", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@transfer_id", transferId);
                    cmd.Parameters.AddWithValue("@product_variant_id", productVariantId);
                    cmd.Parameters.AddWithValue("@quantity", quantity);
                    cmd.Parameters.AddWithValue("@notes", (object)notes ?? DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gọi sp_Warehouse_UpdateTransferStatus để cập nhật status
        /// </summary>
        private void UpdateTransferStatusViaSP(long transferId, string newStatus, string notes)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("sp_Warehouse_UpdateTransferStatus", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@transfer_id", transferId);
                    cmd.Parameters.AddWithValue("@new_status", newStatus);
                    cmd.Parameters.AddWithValue("@notes", (object)notes ?? DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gọi sp_Warehouse_ApproveRequestToShipping để duyệt Requested → Shipping
        /// </summary>
        private void ApproveRequestToShippingViaSP(long transferId, string notes)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("sp_Warehouse_ApproveRequestToShipping", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@transfer_id", transferId);
                    cmd.Parameters.AddWithValue("@notes", (object)notes ?? DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gọi sp_Warehouse_CompleteTransfer để hoàn thành (cập nhật tồn kho)
        /// </summary>
        private void CompleteTransferViaSP(long transferId, string notes)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("sp_Warehouse_CompleteTransfer", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@transfer_id", transferId);
                    cmd.Parameters.AddWithValue("@notes", (object)notes ?? DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gọi sp_Warehouse_CancelRequest để hủy phiếu Requested
        /// </summary>
        private void CancelRequestViaSP(long transferId, string reason)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("sp_Warehouse_CancelRequest", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@transfer_id", transferId);
                    cmd.Parameters.AddWithValue("@reason", reason);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gọi sp_Warehouse_GetAllTransfers để lấy danh sách phiếu
        /// </summary>
        private List<warehouse_transfers> GetAllTransfersViaSP()
        {
            // Sử dụng EF để đơn giản hóa việc load navigation properties
            return db.warehouse_transfers
                .Include("warehouse")
                .Include("branch")
                .Include("warehouse_transfer_details")
                .OrderByDescending(t => t.created_at)
                .ToList();
        }

        #endregion

        #region Helper Methods

        private List<SelectListItem> GetWarehouseOptions()
        {
            return db.warehouses
                .Where(w => w.deleted_at == null)
                .Select(w => new SelectListItem
                {
                    Value = w.id.ToString(),
                    Text = w.name
                })
                .ToList();
        }

        private List<SelectListItem> GetBranchOptions()
        {
            return db.branches
                .Select(b => new SelectListItem
                {
                    Value = b.id.ToString(),
                    Text = b.name
                })
                .ToList();
        }

        private List<SelectListItem> GetProductVariantOptions()
        {
            return db.product_variants
                .Where(v => v.deleted_at == null)
                .Select(v => new SelectListItem
                {
                    Value = v.id.ToString(),
                    Text = v.name + " - " + v.sku
                })
                .ToList();
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        // Helper class for JSON deserialization
        public class TransferDetailItem
        {
            public long ProductVariantId { get; set; }
            public int Quantity { get; set; }
            public string Notes { get; set; }
        }
    }
}
