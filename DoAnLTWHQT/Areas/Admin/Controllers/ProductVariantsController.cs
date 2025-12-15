using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class ProductVariantsController : AdminBaseController
    {
        private readonly Entities _db = new Entities();

        // GET: Admin/ProductVariants?productId=10
        public ActionResult Index(long productId)
        {
            var product = _db.products
                .Include(p => p.product_variants)
                .FirstOrDefault(p => p.id == productId && p.deleted_at == null);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction("Index", "Products");
            }

            var variants = product.product_variants
                .Where(v => v.deleted_at == null)
                .Select(v => new ProductVariantManagementViewModel
                {
                    Id = v.id,
                    Name = $"{product.name} / {v.name}",
                    Sku = v.sku,
                    Price = v.price,
                    OriginalPrice = v.original_price,
                    ImageUrl = v.image_url,
                    QuantityOnHand = 0 // TODO: Calculate from inventory
                })
                .ToList();

            ViewBag.ProductId = productId;
            ViewBag.ProductName = product.name;
            return View(variants);
        }

        // GET: Admin/ProductVariants/Create?productId=10
        public ActionResult Create(long productId)
        {
            var product = _db.products.Find(productId);
            if (product == null || product.deleted_at != null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction("Index", "Products");
            }

            ViewBag.ProductId = productId;
            ViewBag.ProductName = product.name;
            ViewBag.Title = "Thêm biến thể";
            return View("Form", new ProductVariantFormViewModel { ProductId = productId });
        }

        // POST: Admin/ProductVariants/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductVariantFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if product exists
                    var product = _db.products.Find(model.ProductId);
                    if (product == null || product.deleted_at != null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                        return RedirectToAction("Index", "Products");
                    }

                    // Auto-generate SKU from variant name
                    var generatedSku = GenerateSku(model.ProductId, model.Name);

                    // Handle image upload
                    string imageUrl = model.ImageUrl; // Keep existing if no new upload
                    if (model.ImageFile != null && model.ImageFile.ContentLength > 0)
                    {
                        imageUrl = SaveUploadedFile(model.ImageFile);
                    }

                    var variant = new product_variants
                    {
                        product_id = model.ProductId,
                        name = model.Name,
                        sku = generatedSku,
                        price = model.Price,
                        original_price = model.OriginalPrice,
                        image_url = imageUrl,
                        created_at = DateTime.UtcNow,
                        updated_at = DateTime.UtcNow,
                        deleted_at = null
                    };

                    _db.product_variants.Add(variant);
                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Đã thêm biến thể thành công!";
                    return RedirectToAction("Index", new { productId = model.ProductId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi thêm biến thể: " + ex.Message);
                }
            }

            var prod = _db.products.Find(model.ProductId);
            ViewBag.ProductId = model.ProductId;
            ViewBag.ProductName = prod?.name ?? "";
            ViewBag.Title = "Thêm biến thể";
            return View("Form", model);
        }

        // GET: Admin/ProductVariants/Edit/5
        public ActionResult Edit(long id)
        {
            var variant = _db.product_variants
                .Include(v => v.product)
                .FirstOrDefault(v => v.id == id && v.deleted_at == null);

            if (variant == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy biến thể!";
                return RedirectToAction("Index", "Products");
            }

            var vm = new ProductVariantFormViewModel
            {
                Id = variant.id,
                ProductId = variant.product_id,
                Name = variant.name,
                Sku = variant.sku,
                Price = variant.price,
                OriginalPrice = variant.original_price,
                ImageUrl = variant.image_url
            };

            ViewBag.ProductId = variant.product_id;
            ViewBag.ProductName = variant.product.name;
            ViewBag.Title = "Cập nhật biến thể";
            return View("Form", vm);
        }

        // POST: Admin/ProductVariants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(long id, ProductVariantFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var variant = _db.product_variants.Find(id);
                    if (variant == null || variant.deleted_at != null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy biến thể!";
                        return RedirectToAction("Index", "Products");
                    }

                    // Auto-generate new SKU if variant name changed
                    var newSku = variant.sku; // Keep existing by default
                    if (variant.name != model.Name)
                    {
                        newSku = GenerateSku(model.ProductId, model.Name);
                    }

                    // Handle image upload
                    string imageUrl = variant.image_url; // Keep existing by default
                    if (model.ImageFile != null && model.ImageFile.ContentLength > 0)
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrWhiteSpace(variant.image_url))
                        {
                            DeleteUploadedFile(variant.image_url);
                        }
                        imageUrl = SaveUploadedFile(model.ImageFile);
                    }

                    variant.name = model.Name;
                    variant.sku = newSku;
                    variant.price = model.Price;
                    variant.original_price = model.OriginalPrice;
                    variant.image_url = imageUrl;
                    variant.updated_at = DateTime.UtcNow;

                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Đã cập nhật biến thể thành công!";
                    return RedirectToAction("Index", new { productId = variant.product_id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật biến thể: " + ex.Message);
                }
            }

            model.Id = id;
            var prod = _db.products.Find(model.ProductId);
            ViewBag.ProductId = model.ProductId;
            ViewBag.ProductName = prod?.name ?? "";
            ViewBag.Title = "Cập nhật biến thể";
            return View("Form", model);
        }

        // POST: Admin/ProductVariants/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(long id)
        {
            try
            {
                var variant = _db.product_variants.Find(id);
                if (variant == null || variant.deleted_at != null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy biến thể!";
                    return RedirectToAction("Index", "Products");
                }

                var productId = variant.product_id;

                // Soft delete (don't delete physical file, might be needed for references)
                variant.deleted_at = DateTime.UtcNow;
                _db.SaveChanges();

                TempData["SuccessMessage"] = "Đã xóa biến thể thành công!";
                return RedirectToAction("Index", new { productId = productId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa biến thể: " + ex.Message;
                return RedirectToAction("Index", "Products");
            }
        }

        #region Helper Methods

        private string GenerateSku(long productId, string variantName)
        {
            // Create base SKU from product ID and variant name
            var baseSlug = GenerateSlug(variantName);
            var baseSku = $"P{productId:D4}-{baseSlug}";
            
            // Check if SKU exists, if yes, add counter
            var sku = baseSku;
            var counter = 1;
            
            while (_db.product_variants.Any(v => v.sku == sku && v.deleted_at == null))
            {
                sku = $"{baseSku}-{counter:D2}";
                counter++;
            }
            
            return sku.ToUpperInvariant();
        }

        private string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var slug = text.ToLowerInvariant()
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

            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\-+", "-");
            slug = slug.Trim('-');

            return slug;
        }

        private string SaveUploadedFile(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                return null;

            // Validate file size (max 5MB)
            if (file.ContentLength > 5 * 1024 * 1024)
            {
                throw new Exception("Kích thước file không được vượt quá 5MB");
            }

            // Validate file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new Exception("Chỉ chấp nhận file ảnh: JPG, PNG, GIF");
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadFolder = Server.MapPath("~/wwwroot/uploads/products");

            // Create directory if not exists
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var filePath = Path.Combine(uploadFolder, fileName);
            file.SaveAs(filePath);

            // Return relative URL
            return $"/wwwroot/uploads/products/{fileName}";
        }

        private void DeleteUploadedFile(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            try
            {
                var filePath = Server.MapPath("~" + imageUrl);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch
            {
                // Ignore errors when deleting files
            }
        }

        #endregion

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
