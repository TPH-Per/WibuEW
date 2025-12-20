using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Data.Entity;
using DoAnLTWHQT.Models;

namespace DoAnLTWHQT.Controllers
{
    [RoutePrefix("api/products")]
    public class ApiProductsController : ApiController
    {
        private readonly Entities _db = new Entities();

        // ========================================
        // GET /api/products
        // Lấy danh sách 20 sản phẩm mới nhất
        // ========================================
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var products = _db.products
                    .Include(p => p.category)
                    .Include(p => p.product_variants)
                    .Where(p => p.deleted_at == null && p.status == "active")
                    .OrderByDescending(p => p.created_at)
                    .Take(20)
                    .Select(p => new
                    {
                        Id = p.id,
                        Name = p.name,
                        Slug = p.slug,
                        Description = p.description,
                        CategoryId = p.category_id,
                        CategoryName = p.category.name,
                        // Lấy thông tin từ variant đầu tiên làm mặc định
                        Price = p.product_variants.FirstOrDefault() != null
                                ? p.product_variants.FirstOrDefault().price
                                : 0,
                        OriginalPrice = p.product_variants.FirstOrDefault() != null
                                ? p.product_variants.FirstOrDefault().original_price
                                : 0,
                        ImageUrl = p.product_variants.FirstOrDefault() != null
                                ? p.product_variants.FirstOrDefault().image_url
                                : null,
                        CreatedAt = p.created_at
                    })
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Message = "Lấy danh sách sản phẩm thành công",
                    Data = products,
                    Total = products.Count
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAll Error: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/products/{id}
        // Lấy chi tiết 1 sản phẩm kèm các variants
        // ========================================
        [HttpGet]
        [Route("{id:long}")]
        public IHttpActionResult GetById(long id)
        {
            try
            {
                var product = _db.products
                    .Include(p => p.category)
                    .Include(p => p.product_variants)
                    .FirstOrDefault(p => p.id == id && p.deleted_at == null);

                if (product == null)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        Success = false,
                        Message = "Không tìm thấy sản phẩm"
                    });
                }

                var result = new
                {
                    Id = product.id,
                    Name = product.name,
                    Slug = product.slug,
                    Description = product.description,
                    Category = new
                    {
                        Id = product.category.id,
                        Name = product.category.name
                    },
                    Variants = product.product_variants.Select(v => new
                    {
                        Id = v.id,
                        Name = v.name,
                        Sku = v.sku,
                        Price = v.price,
                        OriginalPrice = v.original_price,
                        ImageUrl = v.image_url
                    }).ToList()
                };

                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/products/category/{categoryId}
        // Lấy sản phẩm theo danh mục
        // ========================================
        [HttpGet]
        [Route("category/{categoryId:long}")]
        public IHttpActionResult GetByCategory(long categoryId)
        {
            try
            {
                var products = _db.products
                    .Include(p => p.product_variants)
                    .Where(p => p.category_id == categoryId
                             && p.deleted_at == null
                             && p.status == "active")
                    .Select(p => new
                    {
                        Id = p.id,
                        Name = p.name,
                        Slug = p.slug,
                        Price = p.product_variants.FirstOrDefault() != null ? p.product_variants.FirstOrDefault().price : 0,
                        ImageUrl = p.product_variants.FirstOrDefault() != null ? p.product_variants.FirstOrDefault().image_url : null
                    })
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Data = products,
                    Total = products.Count
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/products/search?q=keyword
        // Tìm kiếm sản phẩm theo tên hoặc mô tả
        // ========================================
        [HttpGet]
        [Route("search")]
        public IHttpActionResult Search([FromUri] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return Ok(new { Success = true, Data = new object[] { }, Total = 0 });
                }

                var keyword = q.ToLower().Trim();

                var products = _db.products
                    .Include(p => p.product_variants)
                    .Where(p => p.deleted_at == null
                             && p.status == "active"
                             && (p.name.ToLower().Contains(keyword) || p.description.ToLower().Contains(keyword)))
                    .Take(20)
                    .Select(p => new
                    {
                        Id = p.id,
                        Name = p.name,
                        Slug = p.slug,
                        Price = p.product_variants.FirstOrDefault() != null ? p.product_variants.FirstOrDefault().price : 0,
                        ImageUrl = p.product_variants.FirstOrDefault() != null ? p.product_variants.FirstOrDefault().image_url : null
                    })
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Keyword = q,
                    Data = products,
                    Total = products.Count
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
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