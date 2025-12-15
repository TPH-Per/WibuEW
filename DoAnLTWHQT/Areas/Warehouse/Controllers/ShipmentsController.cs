using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Warehouse;
using Newtonsoft.Json;

namespace DoAnLTWHQT.Areas.Warehouse.Controllers
{
    public class ShipmentsController : WarehouseBaseController
    {
        private readonly Entities _db = new Entities();

        // GET: Warehouse/Shipments
        public ActionResult Index(string status = "all")
        {
            var query = _db.inbound_receipts
                .Where(r => r.deleted_at == null)
                .Include(r => r.supplier)
                .Include(r => r.warehouse)
                .Include(r => r.inbound_receipt_details)
                .AsQueryable();

            // Filter by status
            if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.status == status);
            }

            var receipts = query
                .OrderByDescending(r => r.created_at)
                .ToList()
                .Select(r => new InboundReceiptViewModel
                {
                    Id = r.id,
                    Code = r.code,
                    SupplierName = r.supplier.name,
                    WarehouseName = r.warehouse.name,
                    Status = r.status,
                    ReceivedAt = r.received_at,
                    TotalAmount = r.total_amount ?? 0,
                    Notes = r.notes,
                    ItemCount = r.inbound_receipt_details != null ? r.inbound_receipt_details.Count : 0,
                    CreatedAt = r.created_at
                })
                .ToList();

            ViewBag.StatusFilter = status;
            return View(receipts);
        }

        // GET: Warehouse/Shipments/Create
        public ActionResult Create()
        {
            var vm = new InboundReceiptFormViewModel
            {
                Code = "IB" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                ReceivedAt = DateTime.Now,
                SupplierOptions = GetSupplierOptions(),
                WarehouseOptions = GetWarehouseOptions(),
                ProductVariantOptions = new List<SelectListItem>(), // Empty initially
                Status = "pending"
            };
            return View(vm);
        }

        // POST: Warehouse/Shipments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(InboundReceiptFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.SupplierOptions = GetSupplierOptions();
                model.WarehouseOptions = GetWarehouseOptions();
                model.ProductVariantOptions = GetProductVariantOptions();
                return View(model);
            }

            try
            {
                // Parse details JSON
                var details = JsonConvert.DeserializeObject<List<InboundReceiptDetailItemViewModel>>(model.DetailsJson ?? "[]");
                
                if (details == null || !details.Any())
                {
                    TempData["ErrorMessage"] = "Vui lòng thêm ít nhất một sản phẩm vào phiếu nhập.";
                    model.SupplierOptions = GetSupplierOptions();
                    model.WarehouseOptions = GetWarehouseOptions();
                    model.ProductVariantOptions = GetProductVariantOptions();
                    return View(model);
                }

                // VALIDATION: Check all products belong to selected supplier
                var variantIds = details.Select(d => d.ProductVariantId).ToList();
                var invalidProducts = _db.product_variants
                    .Where(v => variantIds.Contains(v.id) && v.product.supplier_id != model.SupplierId.Value)
                    .Select(v => v.product.name + " - " + v.name)
                    .ToList();

                if (invalidProducts.Any())
                {
                    TempData["ErrorMessage"] = "Các sản phẩm sau không thuộc nhà cung cấp đã chọn: " + string.Join(", ", invalidProducts);
                    model.SupplierOptions = GetSupplierOptions();
                    model.WarehouseOptions = GetWarehouseOptions();
                    model.ProductVariantOptions = GetProductVariantOptions();
                    return View(model);
                }

                // Create receipt
                var receipt = new inbound_receipts
                {
                    code = model.Code,
                    supplier_id = model.SupplierId.Value,
                    warehouse_id = model.WarehouseId.Value,
                    received_at = model.ReceivedAt ?? DateTime.Now,
                    status = model.Status,
                    notes = model.Notes,
                    created_by = Session["user_id"] != null ? (long?)Convert.ToInt64(Session["user_id"]) : null,
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now,
                    total_amount = 0
                };

                _db.inbound_receipts.Add(receipt);
                _db.SaveChanges();

                // Add details and calculate total
                decimal totalAmount = 0;
                foreach (var item in details)
                {
                    var detail = new inbound_receipt_details
                    {
                        receipt_id = receipt.id,
                        product_variant_id = item.ProductVariantId,
                        quantity = item.Quantity,
                        input_price = item.InputPrice,
                        created_at = DateTime.Now
                    };
                    totalAmount += item.Quantity * item.InputPrice;
                    _db.inbound_receipt_details.Add(detail);
                }

                receipt.total_amount = totalAmount;
                _db.SaveChanges();

                // Update inventory if status is completed
                if (receipt.status == "completed")
                {
                    UpdateInventory(receipt.id, receipt.warehouse_id);
                }

                receipt.updated_at = DateTime.Now;
                _db.SaveChanges();

                TempData["SuccessMessage"] = "Tạo phiếu nhập kho thành công!";
                return RedirectToAction("Details", new { id = receipt.id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tạo phiếu nhập: " + ex.Message;
                model.SupplierOptions = GetSupplierOptions();
                model.WarehouseOptions = GetWarehouseOptions();
                model.ProductVariantOptions = GetProductVariantOptions();
                return View(model);
            }
        }

        // GET: Warehouse/Shipments/Details/5
        public ActionResult Details(long id)
        {
            var receipt = _db.inbound_receipts
                .Where(r => r.id == id && r.deleted_at == null)
                .Include(r => r.supplier)
                .Include(r => r.warehouse)
                .Include(r => r.inbound_receipt_details)
                .FirstOrDefault();

            if (receipt == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiếu nhập kho.";
                return RedirectToAction("Index");
            }

            var vm = new InboundReceiptDetailViewModel
            {
                Id = receipt.id,
                Code = receipt.code,
                SupplierName = receipt.supplier.name,
                WarehouseName = receipt.warehouse.name,
                Status = receipt.status,
                ReceivedAt = receipt.received_at,
                Notes = receipt.notes,
                CreatedAt = receipt.created_at ?? DateTime.Now,
                Details = receipt.inbound_receipt_details
                    .ToList()
                    .Select(d => new InboundReceiptDetailItemViewModel
                    {
                        ProductVariantId = d.product_variant_id,
                        ProductName = d.product_variants.product.name,
                        VariantName = d.product_variants.name,
                        Sku = d.product_variants.sku,
                        Quantity = d.quantity,
                        InputPrice = d.input_price
                    })
                    .ToList()
            };

            vm.TotalAmount = vm.Details.Sum(d => d.LineTotal);

            return View(vm);
        }

        // POST: Warehouse/Shipments/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(long id)
        {
            try
            {
                var receipt = _db.inbound_receipts.Find(id);
                if (receipt == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phiếu nhập kho.";
                    return RedirectToAction("Index");
                }

                // Soft delete receipt only
                receipt.deleted_at = DateTime.Now;
                receipt.updated_at = DateTime.Now;

                _db.SaveChanges();

                TempData["SuccessMessage"] = "Xóa phiếu nhập kho thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: Warehouse/Shipments/GetProductVariantsBySupplierId
        [HttpGet]
        public JsonResult GetProductVariantsBySupplierId(long? supplierId)
        {
            if (!supplierId.HasValue)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            var variants = _db.product_variants
                .Where(v => v.deleted_at == null 
                         && v.product.deleted_at == null 
                         && v.product.supplier_id == supplierId.Value)
                .OrderBy(v => v.product.name)
                .ThenBy(v => v.name)
                .Select(v => new
                {
                    id = v.id,
                    text = v.product.name + " - " + v.name + " (" + v.sku + ")",
                    productName = v.product.name,
                    variantName = v.name,
                    sku = v.sku
                })
                .ToList();

            return Json(variants, JsonRequestBehavior.AllowGet);
        }

        #region Helper Methods

        private IEnumerable<SelectListItem> GetSupplierOptions()
        {
            return _db.suppliers
                .Where(s => s.deleted_at == null)
                .OrderBy(s => s.name)
                .Select(s => new SelectListItem
                {
                    Value = s.id.ToString(),
                    Text = s.name
                })
                .ToList();
        }

        private IEnumerable<SelectListItem> GetWarehouseOptions()
        {
            return _db.warehouses
                .Where(w => w.deleted_at == null)
                .OrderBy(w => w.name)
                .Select(w => new SelectListItem
                {
                    Value = w.id.ToString(),
                    Text = w.name
                })
                .ToList();
        }

        private IEnumerable<SelectListItem> GetProductVariantOptions()
        {
            return _db.product_variants
                .Where(v => v.deleted_at == null && v.product.deleted_at == null)
                .OrderBy(v => v.product.name)
                .ThenBy(v => v.name)
                .Select(v => new SelectListItem
                {
                    Value = v.id.ToString(),
                    Text = v.product.name + " - " + v.name + " (" + v.sku + ")"
                })
                .ToList();
        }

        private void UpdateInventory(long receiptId, long warehouseId)
        {
            var details = _db.inbound_receipt_details
                .Where(d => d.receipt_id == receiptId)
                .ToList();

            foreach (var detail in details)
            {
                var inventory = _db.inventories
                    .FirstOrDefault(i => i.product_variant_id == detail.product_variant_id 
                                      && i.warehouse_id == warehouseId);

                if (inventory == null)
                {
                    // Create new inventory record
                    inventory = new inventory
                    {
                        product_variant_id = detail.product_variant_id,
                        warehouse_id = warehouseId,
                        quantity_on_hand = detail.quantity,
                        quantity_reserved = 0,
                        reorder_level = 10,
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };
                    _db.inventories.Add(inventory);
                }
                else
                {
                    // Update existing
                    inventory.quantity_on_hand += detail.quantity;
                    inventory.updated_at = DateTime.Now;
                }

                // Create transaction record
                var transaction = new inventory_transactions
                {
                    product_variant_id = detail.product_variant_id,
                    warehouse_id = warehouseId,
                    type = "inbound",
                    quantity = detail.quantity,
                    notes = "Nhập kho từ phiếu #" + receiptId,
                    created_at = DateTime.Now
                };
                _db.inventory_transactions.Add(transaction);
            }

            _db.SaveChanges();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
