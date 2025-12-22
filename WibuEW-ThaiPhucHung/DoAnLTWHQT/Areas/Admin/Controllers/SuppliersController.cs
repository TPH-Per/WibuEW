using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class SuppliersController : AdminBaseController
    {
        private readonly perwEntities _db = new perwEntities();

        // GET: Admin/Suppliers
        public ActionResult Index()
        {
            var suppliers = _db.suppliers
                .Where(s => s.deleted_at == null)
                .OrderByDescending(s => s.created_at)
                .Select(s => new SupplierViewModel
                {
                    Id = s.id,
                    Name = s.name,
                    ContactInfo = s.contact_info,
                    CreatedAt = s.created_at.HasValue ? (DateTimeOffset)s.created_at.Value : DateTimeOffset.UtcNow
                })
                .ToList();

            return View(suppliers);
        }

        // GET: Admin/Suppliers/Create
        public ActionResult Create()
        {
            ViewBag.Title = "Thêm nhà cung cấp";
            return View("Form", new SupplierFormViewModel());
        }

        // POST: Admin/Suppliers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SupplierFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var supplier = new supplier
                    {
                        name = model.Name,
                        contact_info = model.ContactInfo,
                        created_at = DateTime.UtcNow,
                        updated_at = DateTime.UtcNow,
                        deleted_at = null
                    };

                    _db.suppliers.Add(supplier);
                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Đã thêm nhà cung cấp thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi thêm nhà cung cấp: " + ex.Message);
                }
            }

            ViewBag.Title = "Thêm nhà cung cấp";
            return View("Form", model);
        }

        // GET: Admin/Suppliers/Edit/5
        public ActionResult Edit(long id)
        {
            var supplier = _db.suppliers.Find(id);
            if (supplier == null || supplier.deleted_at != null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhà cung cấp!";
                return RedirectToAction("Index");
            }

            var vm = new SupplierFormViewModel
            {
                Id = supplier.id,
                Name = supplier.name,
                ContactInfo = supplier.contact_info
            };

            ViewBag.Title = "Cập nhật nhà cung cấp";
            return View("Form", vm);
        }

        // POST: Admin/Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(long id, SupplierFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var supplier = _db.suppliers.Find(id);
                    if (supplier == null || supplier.deleted_at != null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy nhà cung cấp!";
                        return RedirectToAction("Index");
                    }

                    supplier.name = model.Name;
                    supplier.contact_info = model.ContactInfo;
                    supplier.updated_at = DateTime.UtcNow;

                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Đã cập nhật nhà cung cấp thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật nhà cung cấp: " + ex.Message);
                }
            }

            model.Id = id;
            ViewBag.Title = "Cập nhật nhà cung cấp";
            return View("Form", model);
        }

        // POST: Admin/Suppliers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(long id)
        {
            try
            {
                var supplier = _db.suppliers.Find(id);
                if (supplier == null || supplier.deleted_at != null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy nhà cung cấp!";
                    return RedirectToAction("Index");
                }

                // Cascade soft-delete: Supplier → Products → Variants
                var now = DateTime.UtcNow;
                
                // Get all products of this supplier
                var products = _db.products
                    .Include(p => p.product_variants)
                    .Where(p => p.supplier_id == id && p.deleted_at == null)
                    .ToList();

                // Soft delete all products and their variants
                foreach (var product in products)
                {
                    product.deleted_at = now;
                    
                    // Soft delete all variants of this product
                    foreach (var variant in product.product_variants.Where(v => v.deleted_at == null))
                    {
                        variant.deleted_at = now;
                    }
                }

                // Soft delete supplier
                supplier.deleted_at = now;
                _db.SaveChanges();

                TempData["SuccessMessage"] = $"Đã xóa nhà cung cấp và {products.Count} sản phẩm liên quan!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa nhà cung cấp: " + ex.Message;
            }

            return RedirectToAction("Index");
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
