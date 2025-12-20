using Ltwhqt.ViewModels.Admin;
using System;
using System.Linq;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class CategoriesController : AdminBaseController
    {
        private readonly Entities _db = new Entities();

        // GET: Admin/Categories
        public ActionResult Index()
        {
            var categories = _db.categories
                .Where(c => c.deleted_at == null)
                .OrderBy(c => c.name)
                .Select(c => new CategoryListItemViewModel
                {
                    Id = c.id,
                    Name = c.name,
                    Slug = c.slug
                })
                .ToList();

            return View(categories);
        }

        // GET: Admin/Categories/Create
        public ActionResult Create()
        {
            var vm = new CategoryFormViewModel();
            ViewBag.Title = "Thêm danh mục";
            return View("Form", vm);
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CategoryFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var category = new category
                    {
                        name = model.Name,
                        slug = model.Slug ?? GenerateSlug(model.Name),
                        created_at = DateTime.UtcNow,
                        updated_at = DateTime.UtcNow,
                        deleted_at = null
                    };

                    _db.categories.Add(category);
                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Đã thêm danh mục thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi thêm danh mục: " + ex.Message);
                }
            }

            ViewBag.Title = "Thêm danh mục";
            return View("Form", model);
        }

        // GET: Admin/Categories/Edit/5
        public ActionResult Edit(long id)
        {
            var category = _db.categories.Find(id);
            if (category == null || category.deleted_at != null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục!";
                return RedirectToAction("Index");
            }

            var vm = new CategoryFormViewModel
            {
                Id = category.id,
                Name = category.name,
                Slug = category.slug
            };

            ViewBag.Title = "Cập nhật danh mục";
            return View("Form", vm);
        }

        // POST: Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(long id, CategoryFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var category = _db.categories.Find(id);
                    if (category == null || category.deleted_at != null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy danh mục!";
                        return RedirectToAction("Index");
                    }

                    category.name = model.Name;
                    category.slug = model.Slug ?? GenerateSlug(model.Name);
                    category.updated_at = DateTime.UtcNow;

                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Đã cập nhật danh mục thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật danh mục: " + ex.Message);
                }
            }

            model.Id = id;
            ViewBag.Title = "Cập nhật danh mục";
            return View("Form", model);
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(long id)
        {
            try
            {
                var category = _db.categories.Find(id);
                if (category == null || category.deleted_at != null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy danh mục!";
                    return RedirectToAction("Index");
                }

                // Count products in this category
                var productCount = _db.products
                    .Count(p => p.category_id == id && p.deleted_at == null);

                // Soft delete category (products remain visible but category won't show in dropdown)
                category.deleted_at = DateTime.UtcNow;
                _db.SaveChanges();

                if (productCount > 0)
                {
                    TempData["SuccessMessage"] = $"Đã xóa danh mục! Lưu ý: {productCount} sản phẩm thuộc danh mục này vẫn hiển thị nhưng bạn không thể chọn danh mục này khi tạo sản phẩm mới.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Đã xóa danh mục thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa danh mục: " + ex.Message;
            }

            return RedirectToAction("Index");
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
