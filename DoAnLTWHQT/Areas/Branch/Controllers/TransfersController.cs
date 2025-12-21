using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Branch;
using Newtonsoft.Json;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    public class TransfersController : BranchBaseController
    {
        private readonly string _connectionString;

        public TransfersController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["PerwDbContext"]?.ConnectionString;
        }

        /// <summary>
        /// GET: Branch/Transfers - Danh sách yêu cầu nhập hàng
        /// </summary>
        public ActionResult Index(string status = "all")
        {
            long branchId = GetCurrentBranchId();
            
            var transfers = GetMyTransferRequests(branchId);
            
            if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                transfers = transfers.Where(t => string.Equals(t.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.StatusFilter = status;
            ViewBag.BranchId = branchId;
            return View(transfers);
        }

        /// <summary>
        /// GET: Branch/Transfers/Details/5
        /// </summary>
        public ActionResult Details(long id)
        {
            var detail = GetTransferRequestDetails(id);
            if (detail == null || detail.Header == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu yêu cầu.";
                return RedirectToAction("Index");
            }
            return View(detail);
        }

        /// <summary>
        /// GET: Branch/Transfers/Create
        /// </summary>
        public ActionResult Create()
        {
            long branchId = GetCurrentBranchId();
            var branch = _db.branches.Find(branchId);
            
            ViewBag.BranchId = branchId;
            ViewBag.BranchName = branch?.name ?? "Chi nhánh";
            ViewBag.Warehouses = GetWarehouseOptions();
            ViewBag.ProductVariants = GetProductVariantOptions();

            return View();
        }

        /// <summary>
        /// POST: Branch/Transfers/Create - Tạo yêu cầu với chi tiết products
        /// SỬ DỤNG STORED PROCEDURE: sp_Branch_CreateTransferRequest và sp_Branch_AddTransferRequestDetail
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(long warehouseId, long branchId, string detailsJson, string notes = null)
        {
            try
            {
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

                // SỬ DỤNG STORED PROCEDURE ĐỂ TẠO PHIẾU YÊU CẦU
                long newTransferId = CreateTransferRequestViaSP(warehouseId, branchId, notes);

                if (newTransferId <= 0)
                {
                    TempData["Error"] = "Không thể tạo phiếu yêu cầu.";
                    return RedirectToAction("Create");
                }

                // SỬ DỤNG STORED PROCEDURE ĐỂ THÊM CHI TIẾT SẢN PHẨM
                foreach (var item in details)
                {
                    AddTransferDetailViaSP(newTransferId, item.ProductVariantId, item.Quantity, item.Notes);
                }

                TempData["Success"] = $"Đã tạo yêu cầu nhập hàng #{newTransferId} thành công! Chờ Kho duyệt.";
                return RedirectToAction("Details", new { id = newTransferId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Create");
            }
        }

        /// <summary>
        /// GET: Branch/Transfers/AddItems/5 - Thêm sản phẩm vào phiếu đã tạo
        /// </summary>
        public ActionResult AddItems(long id)
        {
            var detail = GetTransferRequestDetails(id);
            if (detail == null || detail.Header == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu yêu cầu.";
                return RedirectToAction("Index");
            }

            // Chỉ cho phép thêm sản phẩm khi status = Requested
            if (detail.Header.Status != "Requested")
            {
                TempData["Error"] = "Không thể thêm sản phẩm vào phiếu đã được xử lý.";
                return RedirectToAction("Details", new { id });
            }

            var model = new AddTransferDetailViewModel
            {
                TransferId = id,
                ProductVariantOptions = _db.product_variants
                    .Where(pv => pv.deleted_at == null)
                    .Select(pv => new SelectListItem
                    {
                        Value = pv.id.ToString(),
                        Text = pv.product.name + " - " + pv.name + " (" + pv.sku + ")"
                    }).ToList()
            };

            ViewBag.TransferDetail = detail;
            return View(model);
        }

        /// <summary>
        /// POST: Branch/Transfers/AddItems
        /// SỬ DỤNG STORED PROCEDURE: sp_Branch_AddTransferRequestDetail
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddItems(AddTransferDetailViewModel model)
        {
            if (!ModelState.IsValid || model.Quantity <= 0)
            {
                TempData["Error"] = "Vui lòng nhập số lượng hợp lệ.";
                return RedirectToAction("AddItems", new { id = model.TransferId });
            }

            try
            {
                // SỬ DỤNG STORED PROCEDURE
                AddTransferDetailViaSP(model.TransferId, model.ProductVariantId, model.Quantity, model.Notes);
                TempData["Success"] = "Đã thêm sản phẩm vào phiếu yêu cầu!";
                return RedirectToAction("AddItems", new { id = model.TransferId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("AddItems", new { id = model.TransferId });
            }
        }

        /// <summary>
        /// POST: Branch/Transfers/ConfirmDelivery
        /// Branch xác nhận đã nhận hàng: Shipping → Delivered
        /// </summary>
        [HttpPost]
        public ActionResult ConfirmDelivery(long id, string notes = null)
        {
            try
            {
                long branchId = GetCurrentBranchId();
                ConfirmDeliveryViaSP(id, branchId, notes);
                return Json(new { success = true, message = "✅ Đã xác nhận nhận hàng! Tồn kho đã được cập nhật." });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.InnerException?.Message 
                    ?? ex.InnerException?.Message 
                    ?? ex.Message;
                return Json(new { success = false, message = "Lỗi: " + msg });
            }
        }

        #region Helper Methods

        private new long GetCurrentBranchId()
        {
            // Su dung base class method
            return base.GetCurrentBranchId() ?? 1;
        }

        private List<SelectListItem> GetWarehouseOptions()
        {
            return _db.warehouses
                .Where(w => w.deleted_at == null)
                .Select(w => new SelectListItem
                {
                    Value = w.id.ToString(),
                    Text = w.name
                }).ToList();
        }

        private List<SelectListItem> GetProductVariantOptions()
        {
            return _db.product_variants
                .Where(v => v.deleted_at == null)
                .Select(v => new SelectListItem
                {
                    Value = v.id.ToString(),
                    Text = v.product.name + " - " + v.name + " (" + v.sku + ")"
                }).ToList();
        }

        #endregion

        #region Stored Procedure Calls using ADO.NET

        /// <summary>
        /// Gọi sp_Branch_CreateTransferRequest để tạo phiếu yêu cầu
        /// </summary>
        private long CreateTransferRequestViaSP(long warehouseId, long branchId, string notes)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("sp_Branch_CreateTransferRequest", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@from_warehouse_id", warehouseId);
                    cmd.Parameters.AddWithValue("@to_branch_id", branchId);
                    cmd.Parameters.AddWithValue("@notes", (object)notes ?? DBNull.Value);
                    
                    // Output parameter
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
        /// Gọi sp_Branch_AddTransferRequestDetail để thêm chi tiết sản phẩm
        /// </summary>
        private void AddTransferDetailViaSP(long transferId, long productVariantId, int quantity, string notes)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("sp_Branch_AddTransferRequestDetail", conn))
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
        /// Gọi sp_Branch_GetMyTransferRequests để lấy danh sách yêu cầu
        /// </summary>
        private List<TransferRequestListViewModel> GetMyTransferRequests(long branchId)
        {
            var result = new List<TransferRequestListViewModel>();

            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("sp_Branch_GetMyTransferRequests", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@branch_id", branchId);
                    
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new TransferRequestListViewModel
                            {
                                Id = Convert.ToInt64(reader["id"]),
                                FromWarehouseId = Convert.ToInt64(reader["from_warehouse_id"]),
                                WarehouseName = reader["warehouse_name"]?.ToString() ?? "",
                                ToBranchId = Convert.ToInt64(reader["to_branch_id"]),
                                BranchName = reader["branch_name"]?.ToString() ?? "",
                                TransferDate = Convert.ToDateTime(reader["transfer_date"]),
                                Status = reader["status"]?.ToString() ?? "",
                                Notes = reader["notes"]?.ToString() ?? "",
                                CreatedAt = Convert.ToDateTime(reader["created_at"]),
                                UpdatedAt = Convert.ToDateTime(reader["updated_at"]),
                                TotalItems = reader["total_items"] != DBNull.Value ? Convert.ToInt32(reader["total_items"]) : 0,
                                TotalAmount = reader["total_amount"] != DBNull.Value ? Convert.ToDecimal(reader["total_amount"]) : 0
                            });
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gọi sp_Branch_GetTransferRequestDetails để lấy chi tiết phiếu
        /// </summary>
        private TransferRequestFullViewModel GetTransferRequestDetails(long transferId)
        {
            var result = new TransferRequestFullViewModel();

            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("sp_Branch_GetTransferRequestDetails", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@transfer_id", transferId);
                    
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        // Header
                        if (reader.Read())
                        {
                            result.Header = new TransferRequestListViewModel
                            {
                                Id = Convert.ToInt64(reader["id"]),
                                FromWarehouseId = Convert.ToInt64(reader["from_warehouse_id"]),
                                WarehouseName = reader["warehouse_name"]?.ToString() ?? "",
                                ToBranchId = Convert.ToInt64(reader["to_branch_id"]),
                                BranchName = reader["branch_name"]?.ToString() ?? "",
                                TransferDate = Convert.ToDateTime(reader["transfer_date"]),
                                Status = reader["status"]?.ToString() ?? "",
                                Notes = reader["notes"]?.ToString() ?? "",
                                CreatedAt = Convert.ToDateTime(reader["created_at"]),
                                UpdatedAt = Convert.ToDateTime(reader["updated_at"])
                            };
                        }

                        // Details
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                result.Details.Add(new TransferRequestDetailViewModel
                                {
                                    Id = Convert.ToInt64(reader["id"]),
                                    TransferId = Convert.ToInt64(reader["transfer_id"]),
                                    ProductVariantId = Convert.ToInt64(reader["product_variant_id"]),
                                    Sku = reader["sku"]?.ToString() ?? "",
                                    ProductName = reader["product_name"]?.ToString() ?? "",
                                    VariantName = reader["variant_name"]?.ToString() ?? "",
                                    Quantity = Convert.ToInt32(reader["quantity"]),
                                    Price = reader["price"] != DBNull.Value ? Convert.ToDecimal(reader["price"]) : 0,
                                    LineTotal = reader["line_total"] != DBNull.Value ? Convert.ToDecimal(reader["line_total"]) : 0,
                                    Notes = reader["notes"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gọi sp_Branch_ConfirmDelivery để xác nhận nhận hàng
        /// </summary>
        private void ConfirmDeliveryViaSP(long transferId, long branchId, string notes)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("sp_Branch_ConfirmDelivery", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@transfer_id", transferId);
                    cmd.Parameters.AddWithValue("@branch_id", branchId);
                    cmd.Parameters.AddWithValue("@notes", (object)notes ?? DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion



        // Helper class for JSON deserialization
        public class TransferDetailItem
        {
            public long ProductVariantId { get; set; }
            public int Quantity { get; set; }
            public string Notes { get; set; }
        }
    }
}
