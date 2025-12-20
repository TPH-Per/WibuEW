using Ltwhqt.ViewModels.Admin;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class ProductsController : AdminBaseController
    {
        private readonly Entities _db = new Entities();

        // GET: Admin/Products
        public ActionResult Index(string status = "all")
        {
            var query = _db.products
                .Include(p => p.category)
                .Include(p => p.supplier)
                .Include(p => p.product_variants)
                .Where(p => p.deleted_at == null);

            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.status == status);
            }

            var products = query
                .OrderByDescending(p => p.updated_at)
                .Select(p => new ProductManagementViewModel
                {
                    Id = p.id,
                    Name = p.name,
                    Category = p.category.name,
                    Supplier = p.supplier.name,
                    Status = p.status,
                    VariantCount = p.product_variants.Count(v => v.deleted_at == null),
                    UpdatedAt = p.updated_at.HasValue ? (DateTimeOffset)p.updated_at.Value : DateTimeOffset.UtcNow
                })
                .ToList();

            ViewBag.StatusFilter = status;
            ViewBag.StatusOptions = BuildStatusOptions();
            return View(products);
        }

        // GET: Admin/Products/Create
        public ActionResult Create()
        {
            var vm = BuildProductForm();
            ViewBag.Title = "Thêm sản phẩm";
            return View("Form", vm);
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var product = new product
                    {
                        name = model.Name,
                        description = model.Description,
                        slug = string.IsNullOrWhiteSpace(model.Slug) ? GenerateSlug(model.Name) : model.Slug,
                        category_id = model.CategoryId,
                        supplier_id = model.SupplierId,
                        status = string.IsNullOrWhiteSpace(model.Status) ? "draft" : model.Status,
                        created_at = DateTime.UtcNow,
                        updated_at = DateTime.UtcNow,
                        deleted_at = null
                    };

                    _db.products.Add(product);
                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Đã thêm sản phẩm thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi thêm sản phẩm: " + ex.Message);
                }
            }

            model.CategoryOptions = BuildCategoryOptions();
            model.SupplierOptions = BuildSupplierOptions();
            model.StatusOptions = BuildStatusOptions();
            ViewBag.Title = "Thêm sản phẩm";
            return View("Form", model);
        }

        // GET: Admin/Products/Edit/5
        public ActionResult Edit(long id)
        {
            var product = _db.products
                .Include(p => p.category)
                .Include(p => p.supplier)
                .FirstOrDefault(p => p.id == id && p.deleted_at == null);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction("Index");
            }

            var vm = new ProductFormViewModel
            {
                Id = product.id,
                Name = product.name,
                Description = product.description,
                Slug = product.slug,
                CategoryId = product.category_id,
                SupplierId = product.supplier_id,
                Status = product.status,
                CategoryOptions = BuildCategoryOptions(),
                SupplierOptions = BuildSupplierOptions(),
                StatusOptions = BuildStatusOptions()
            };

            ViewBag.Title = "Cập nhật sản phẩm";
            return View("Form", vm);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(long id, ProductFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var product = _db.products.Find(id);
                    if (product == null || product.deleted_at != null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                        return RedirectToAction("Index");
                    }

                    product.name = model.Name;
                    product.description = model.Description;
                    product.slug = string.IsNullOrWhiteSpace(model.Slug) ? GenerateSlug(model.Name) : model.Slug;
                    product.category_id = model.CategoryId;
                    product.supplier_id = model.SupplierId;
                    product.status = string.IsNullOrWhiteSpace(model.Status) ? product.status : model.Status;
                    product.updated_at = DateTime.UtcNow;

                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Đã cập nhật sản phẩm thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật sản phẩm: " + ex.Message);
                }
            }

            model.Id = id;
            model.CategoryOptions = BuildCategoryOptions();
            model.SupplierOptions = BuildSupplierOptions();
            model.StatusOptions = BuildStatusOptions();
            ViewBag.Title = "Cập nhật sản phẩm";
            return View("Form", model);
        }

        // GET: Admin/Products/Details/5
        public ActionResult Details(long id)
        {
            var product = _db.products
                .Include(p => p.category)
                .Include(p => p.supplier)
                .Include(p => p.product_variants)
                .FirstOrDefault(p => p.id == id && p.deleted_at == null);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction("Index");
            }

            var vm = new ProductManagementViewModel
            {
                Id = product.id,
                Name = product.name,
                Category = product.category?.name,
                Supplier = product.supplier?.name,
                Status = product.status,
                VariantCount = product.product_variants.Count(v => v.deleted_at == null),
                UpdatedAt = product.updated_at.HasValue ? (DateTimeOffset)product.updated_at.Value : DateTimeOffset.UtcNow,
                Variants = product.product_variants
                    .Where(v => v.deleted_at == null)
                    .Select(v => new ProductVariantManagementViewModel
                    {
                        Id = v.id,
                        Name = $"{product.name} / {v.name}",
                        Sku = v.sku,
                        Price = v.price,
                        QuantityOnHand = 0 // TODO: Calculate from inventory
                    })
                    .ToList()
            };

            return View(vm);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(long id)
        {
            try
            {
                var product = _db.products.Find(id);
                if (product == null || product.deleted_at != null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                    return RedirectToAction("Index");
                }

                // Soft delete product and its variants
                product.deleted_at = DateTime.UtcNow;

                foreach (var variant in product.product_variants.Where(v => v.deleted_at == null))
                {
                    variant.deleted_at = DateTime.UtcNow;
                }

                _db.SaveChanges();

                TempData["SuccessMessage"] = "Đã xóa sản phẩm thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa sản phẩm: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        private IEnumerable<SelectListItem> BuildCategoryOptions()
        {
            return _db.categories
                .Where(c => c.deleted_at == null)
                .OrderBy(c => c.name)
                .Select(c => new SelectListItem
                {
                    Value = c.id.ToString(),
                    Text = c.name
                })
                .ToList();
        }

        private IEnumerable<SelectListItem> BuildSupplierOptions()
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

        private static IEnumerable<SelectListItem> BuildStatusOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "draft", Text = "Nháp" },
                new SelectListItem { Value = "published", Text = "Đã xuất bản" },
                new SelectListItem { Value = "archived", Text = "Ngừng bán" }
            };
        }

        private ProductFormViewModel BuildProductForm(long? id = null)
        {
            return new ProductFormViewModel
            {
                Id = id,
                CategoryOptions = BuildCategoryOptions(),
                SupplierOptions = BuildSupplierOptions(),
                StatusOptions = BuildStatusOptions()
            };
        }

        private string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Convert to lowercase and replace spaces with hyphens
            var slug = name.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("đ", "d")
                .Replace("á", "a").Replace("à", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
                .Replace("ă", "a").Replace("ắ", "a").Replace("ằ", "a").Replace("ẳ", "a").Replace("ẵ", "a").Replace("ặ", "a")
                .Replace("â", "a").Replace("ấ", "a").Replace("ầ", "a").Replace("ẩ", "a").Replace("ẫ", "a").Replace("ậ", "a")
                .Replace("é", "e").Replace("è", "e").Replace("ẻ", "e").Replace("ẽ", "e").Replace("ẹ", "e")
                .Replace("ê", "e").Replace("ế", "e").Replace("ề", "e").Replace("ể", "e").Replace("ễ", "e").Replace("ệ", "e")
                .Replace("í", "i").Replace("ì", "i").Replace("ỉ", "i").Replace("ĩ", "i").Replace("ị", "i")
                .Replace("ó", "o").Replace("ò", "o").Replace("ỏ", "o").Replace("õ", "o").Replace("ọ", "o")
                .Replace("ô", "o").Replace("ố", "o").Replace("ồ", "o").Replace("ổ", "o").Replace("ỗ", "o").Replace("ộ", "o")
                .Replace("ơ", "o").Replace("ớ", "o").Replace("ờ", "o").Replace("ở", "o").Replace("ỡ", "o").Replace("ợ", "o")
                .Replace("ú", "u").Replace("ù", "u").Replace("ủ", "u").Replace("ũ", "u").Replace("ụ", "u")
                .Replace("ư", "u").Replace("ứ", "u").Replace("ừ", "u").Replace("ử", "u").Replace("ữ", "u").Replace("ự", "u")
                .Replace("ý", "y").Replace("ỳ", "y").Replace("ỷ", "y").Replace("ỹ", "y").Replace("ỵ", "y");

            // Remove special characters
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\-+", "-");
            slug = slug.Trim('-');

            return slug;
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
