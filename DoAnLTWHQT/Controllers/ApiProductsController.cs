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
        // GET /api/products/in-stock
        // Lấy sản phẩm có trong kho (tùy chọn lọc theo chi nhánh)
        // ========================================
        [HttpGet]
        [Route("in-stock")]
        public IHttpActionResult GetProductsInStock(
            [FromUri] long? branchId = null,
            [FromUri] long? categoryId = null,
            [FromUri] long? supplierId = null,
            [FromUri] int page = 1,
            [FromUri] int pageSize = 20)
        {
            try
            {
                var query = _db.products
                    .Include(p => p.category)
                    .Include(p => p.supplier)
                    .Include(p => p.product_variants)
                    .Where(p => p.deleted_at == null && p.status == "active");

                // Filter: Chỉ lấy sản phẩm có tồn kho > 0
                query = query.Where(p => p.product_variants.Any(v =>
                    _db.branch_inventories.Any(bi =>
                        bi.product_variant_id == v.id &&
                        (!branchId.HasValue || bi.branch_id == branchId.Value) &&
                        bi.quantity_on_hand > 0)));

                if (categoryId.HasValue)
                    query = query.Where(p => p.category_id == categoryId.Value);

                if (supplierId.HasValue)
                    query = query.Where(p => p.supplier_id == supplierId.Value);

                var totalCount = query.Count();

                var products = query
                    .OrderByDescending(p => p.created_at)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        Id = p.id,
                        Name = p.name,
                        Slug = p.slug,
                        Description = p.description,
                        CategoryId = p.category_id,
                        CategoryName = p.category.name,
                        SupplierId = p.supplier_id,
                        SupplierName = p.supplier.name,
                        // Lấy giá thấp nhất trong các variant còn hoạt động
                        Price = p.product_variants.Where(v => v.deleted_at == null).Min(v => (decimal?)v.price) ?? 0,
                        OriginalPrice = p.product_variants.Where(v => v.deleted_at == null).Min(v => v.original_price),
                        ImageUrl = p.product_variants.Where(v => v.deleted_at == null).Select(v => v.image_url).FirstOrDefault(),
                        // Tính tổng tồn kho
                        TotalStock = _db.branch_inventories
                            .Where(bi => p.product_variants.Select(v => v.id).Contains(bi.product_variant_id)
                                         && (!branchId.HasValue || bi.branch_id == branchId.Value))
                            .Sum(bi => (int?)bi.quantity_on_hand) ?? 0
                    })
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Data = products,
                    Total = totalCount,
                    Page = page,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    Message = "Lấy danh sách sản phẩm tồn kho thành công"
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/products/{id}
        // ========================================
        [HttpGet]
        [Route("{id:long}")]
        public IHttpActionResult GetById(long id)
        {
            try
            {
                var product = _db.products
                    .Include(p => p.category)
                    .Include(p => p.supplier)
                    .Include(p => p.product_variants)
                    .FirstOrDefault(p => p.id == id && p.deleted_at == null);

                if (product == null)
                    return Content(HttpStatusCode.NotFound, new { Success = false, Message = "Không tìm thấy sản phẩm" });

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        Id = product.id,
                        Name = product.name,
                        Slug = product.slug,
                        Description = product.description,
                        Status = product.status,
                        Category = new { Id = product.category.id, Name = product.category.name },
                        Supplier = new { Id = product.supplier.id, Name = product.supplier.name },
                        Variants = product.product_variants.Where(v => v.deleted_at == null).Select(v => new
                        {
                            Id = v.id,
                            Name = v.name,
                            Sku = v.sku,
                            Price = v.price,
                            OriginalPrice = v.original_price,
                            ImageUrl = v.image_url
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/products
        // ========================================
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll([FromUri] bool inStock = false, [FromUri] long? branchId = null)
        {
            try
            {
                var query = _db.products
                    .Include(p => p.product_variants)
                    .Where(p => p.deleted_at == null && p.status == "active");

                if (inStock)
                {
                    query = query.Where(p => p.product_variants.Any(v =>
                        _db.branch_inventories.Any(bi =>
                            bi.product_variant_id == v.id &&
                            (!branchId.HasValue || bi.branch_id == branchId.Value) &&
                            bi.quantity_on_hand > 0)));
                }

                var products = query
                    .OrderByDescending(p => p.created_at)
                    .Take(50)
                    .Select(p => new
                    {
                        Id = p.id,
                        Name = p.name,
                        Slug = p.slug,
                        Price = p.product_variants.Where(v => v.deleted_at == null).Min(v => (decimal?)v.price) ?? 0,
                        ImageUrl = p.product_variants.Where(v => v.deleted_at == null).Select(v => v.image_url).FirstOrDefault()
                    })
                    .ToList();

                return Ok(new { Success = true, Data = products, Total = products.Count });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/products/category/{categoryId}
        // ========================================
        [HttpGet]
        [Route("category/{categoryId:long}")]
        public IHttpActionResult GetByCategory(long categoryId, [FromUri] long? branchId = null)
        {
            try
            {
                var query = _db.products
                    .Where(p => p.category_id == categoryId && p.deleted_at == null && p.status == "active");

                if (branchId.HasValue)
                {
                    query = query.Where(p => p.product_variants.Any(v =>
                        _db.branch_inventories.Any(bi =>
                            bi.product_variant_id == v.id &&
                            bi.branch_id == branchId.Value &&
                            bi.quantity_on_hand > 0)));
                }

                var products = query.Select(p => new
                {
                    Id = p.id,
                    Name = p.name,
                    Slug = p.slug,
                    Price = p.product_variants.FirstOrDefault() != null ? p.product_variants.FirstOrDefault().price : 0,
                    ImageUrl = p.product_variants.FirstOrDefault() != null ? p.product_variants.FirstOrDefault().image_url : null
                }).ToList();

                return Ok(new { Success = true, Data = products, Total = products.Count });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/products/search
        // ========================================
        [HttpGet]
        [Route("search")]
        public IHttpActionResult Search([FromUri] string q, [FromUri] long? branchId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                    return Ok(new { Success = true, Data = new object[] { }, Total = 0 });

                var keyword = q.ToLower().Trim();
                var query = _db.products
                    .Where(p => p.deleted_at == null && p.status == "active" &&
                               (p.name.ToLower().Contains(keyword) || p.description.ToLower().Contains(keyword)));

                if (branchId.HasValue)
                {
                    query = query.Where(p => p.product_variants.Any(v =>
                        _db.branch_inventories.Any(bi =>
                            bi.product_variant_id == v.id &&
                            bi.branch_id == branchId.Value &&
                            bi.quantity_on_hand > 0)));
                }

                var products = query.Take(20).Select(p => new
                {
                    Id = p.id,
                    Name = p.name,
                    Slug = p.slug,
                    Price = p.product_variants.FirstOrDefault() != null ? p.product_variants.FirstOrDefault().price : 0,
                    ImageUrl = p.product_variants.FirstOrDefault() != null ? p.product_variants.FirstOrDefault().image_url : null
                }).ToList();

                return Ok(new { Success = true, Keyword = q, Data = products, Total = products.Count });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}