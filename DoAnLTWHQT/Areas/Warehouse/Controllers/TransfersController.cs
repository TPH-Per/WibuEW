using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Warehouse.Controllers
{
    public class TransfersController : WarehouseBaseController
    {
        private Entities db = new Entities();

        // GET: Warehouse/Transfers
        public ActionResult Index(string status = "all")
        {
            var query = db.warehouse_transfers
                .Include(t => t.warehouse)
                .Include(t => t.branch)
                .OrderByDescending(t => t.created_at);

            if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                query = (IOrderedQueryable<warehouse_transfers>)query.Where(t => t.status == status);
            }

            var transfers = query.ToList();
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

                // Tạo phiếu chuyển kho
                var transfer = new warehouse_transfers
                {
                    from_warehouse_id = warehouseId,
                    to_branch_id = branchId,
                    transfer_date = DateTime.Now,
                    status = "pending",
                    notes = notes,
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                };

                db.warehouse_transfers.Add(transfer);
                db.SaveChanges();

                // Lưu chi tiết sản phẩm
                foreach (var item in details)
                {
                    var detail = new warehouse_transfer_details
                    {
                        transfer_id = transfer.id,
                        product_variant_id = item.ProductVariantId,
                        quantity = item.Quantity,
                        notes = item.Notes,
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };

                    db.warehouse_transfer_details.Add(detail);
                }

                db.SaveChanges();

                TempData["Success"] = $"Tạo phiếu chuyển kho #{transfer.id} thành công!";
                return RedirectToAction("Details", new { id = transfer.id });
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
                .Include(t => t.warehouse)
                .Include(t => t.branch)
                .Include(t => t.warehouse_transfer_details.Select(d => d.product_variants))
                .FirstOrDefault(t => t.id == id);

            if (transfer == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu chuyển kho.";
                return RedirectToAction("Index");
            }

            return View(transfer);
        }

        // POST: Warehouse/Transfers/UpdateStatus
        [HttpPost]
        public ActionResult UpdateStatus(long id, string newStatus)
        {
            try
            {
                var transfer = db.warehouse_transfers.Find(id);

                if (transfer == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu chuyển kho." });
                }

                // Validate status transitions
                var validTransitions = new Dictionary<string, List<string>>
                {
                    { "pending", new List<string> { "shipping", "cancelled" } },
                    { "shipping", new List<string> { "completed", "returned" } }
                };

                if (!validTransitions.ContainsKey(transfer.status) ||
                    !validTransitions[transfer.status].Contains(newStatus))
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Không thể chuyển từ {transfer.status} sang {newStatus}."
                    });
                }

                // Cập nhật status - Triggers sẽ tự động chạy
                transfer.status = newStatus;
                transfer.updated_at = DateTime.Now;

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Đã cập nhật trạng thái thành {newStatus}."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                });
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

        // GET: Ajax - Kiểm tra tồn kho
        [HttpGet]
        public JsonResult CheckInventory(long warehouseId, long variantId)
        {
            var inventory = db.inventories
                .FirstOrDefault(i => i.warehouse_id == warehouseId
                    && i.product_variant_id == variantId);

            var available = inventory?.quantity_on_hand ?? 0;

            return Json(new
            {
                available = available,
                productName = inventory?.product_variants?.name ?? "N/A"
            }, JsonRequestBehavior.AllowGet);
        }

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
