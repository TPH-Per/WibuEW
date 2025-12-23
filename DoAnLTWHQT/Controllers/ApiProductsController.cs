using DoAnLTWHQT.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;

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

        private long? GetCurrentUserId()
        {
            try
            {
                var identity = HttpContext.Current?.User?.Identity;
                if (identity == null || !identity.IsAuthenticated)
                {
                    return null;
                }

                // Lấy email/username từ identity
                var userName = identity.Name;
                if (string.IsNullOrEmpty(userName)) return null;

                // Query database để lấy user id (Cache kết quả này nếu có thể)
                var user = _db.users.FirstOrDefault(u =>
                    (u.email == userName || u.name == userName) &&
                    u.deleted_at == null &&
                    u.status == "active");

                return user?.id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetCurrentUserId] Error: {ex.Message}");
                return null;
            }
        }

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


        // ==========================================
        // GET: /api/products/{productId}/reviews
        // Lấy danh sách đánh giá của sản phẩm
        // ==========================================
        [HttpGet]
        [Route("{productId}/reviews")]
        public IHttpActionResult GetReviews(long productId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[GetReviews] ProductId: {productId}");

                var currentUserId = GetCurrentUserId();

                // Kiểm tra product tồn tại
                var product = _db.products.Find(productId);
                if (product == null)
                {
                    return Content(System.Net.HttpStatusCode.NotFound, new
                    {
                        Success = false,
                        Message = "Sản phẩm không tồn tại"
                    });
                }

                // Lấy danh sách reviews
                // CHẾ ĐỘ TEST: Lấy tất cả reviews không cần duyệt
                var reviewsQuery = _db.product_reviews
                    .Where(r => r.product_id == productId && r.deleted_at == null)
                    // PRODUCTION: Bỏ comment dòng dưới
                    // .Where(r => r.is_approved == true || r.user_id == currentUserId)
                    .OrderByDescending(r => r.created_at)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[GetReviews] Found {reviewsQuery.Count} reviews");

                // Map sang DTO
                var reviews = new List<ReviewResponse>();
                foreach (var r in reviewsQuery)
                {
                    var user = _db.users.Find(r.user_id);
                    var userName = user?.full_name ?? "Ẩn danh";

                    // Kiểm tra verified purchase (đơn giản hóa để tránh lỗi)
                    var isVerifiedPurchase = false;
                    try
                    {
                        isVerifiedPurchase = _db.purchase_orders
                            .Join(_db.purchase_order_details,
                                po => po.id,
                                pod => pod.order_id,
                                (po, pod) => new { po, pod })
                            .Join(_db.product_variants,
                                x => x.pod.product_variant_id,
                                pv => pv.id,
                                (x, pv) => new { x.po, pv })
                            .Any(x => x.po.user_id == r.user_id
                                   && x.pv.product_id == productId
                                   && x.po.status == "delivered");
                    }
                    catch
                    {
                        // Nếu lỗi join, để false
                    }

                    reviews.Add(new ReviewResponse
                    {
                        UserId = r.user_id,
                        ProductId = r.product_id,
                        UserName = userName,
                        Rating = r.rating,
                        Comment = r.comment,
                        IsApproved = r.is_approved,
                        IsVerifiedPurchase = isVerifiedPurchase,
                        Status = r.status,
                        CreatedAt = r.created_at,
                        UpdatedAt = r.updated_at
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Data = reviews,
                    Message = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetReviews] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[GetReviews] StackTrace: {ex.StackTrace}");
                return InternalServerError(new Exception("Đã xảy ra lỗi khi lấy danh sách đánh giá"));
            }
        }

        // ==========================================
        // POST: /api/products/{productId}/reviews
        // Tạo đánh giá mới
        // ==========================================
        [HttpPost]
        [Route("{productId}/reviews")]
        public IHttpActionResult CreateReview(long productId, [FromBody] ReviewCreateRequest request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CreateReview] ProductId: {productId}");
                System.Diagnostics.Debug.WriteLine($"[CreateReview] Rating: {request?.Rating}, Comment: {request?.Comment}");

                // 1. Kiểm tra đăng nhập
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CreateReview] User not authenticated");
                    return Content(System.Net.HttpStatusCode.Unauthorized, new
                    {
                        Success = false,
                        Message = "Vui lòng đăng nhập để đánh giá"
                    });
                }

                System.Diagnostics.Debug.WriteLine($"[CreateReview] UserId: {userId}");

                // 2. Validate rating
                if (request == null || request.Rating < 1 || request.Rating > 5)
                {
                    return Content(System.Net.HttpStatusCode.BadRequest, new
                    {
                        Success = false,
                        Message = "Rating phải từ 1 đến 5"
                    });
                }

                // 3. Kiểm tra product tồn tại
                var product = _db.products.Find(productId);
                if (product == null)
                {
                    return Content(System.Net.HttpStatusCode.NotFound, new
                    {
                        Success = false,
                        Message = "Sản phẩm không tồn tại"
                    });
                }

                // 4. Kiểm tra user đã review chưa
                var existingReview = _db.product_reviews
                    .FirstOrDefault(r => r.user_id == userId && r.product_id == productId && r.deleted_at == null);

                if (existingReview != null)
                {
                    return Content(System.Net.HttpStatusCode.Conflict, new
                    {
                        Success = false,
                        Message = "Bạn đã đánh giá sản phẩm này rồi"
                    });
                }

                // 5. Tạo review mới
                // CHẾ ĐỘ TEST: Auto approved
                var review = new product_reviews
                {
                    user_id = userId.Value,
                    product_id = productId,
                    rating = (byte)request.Rating,
                    comment = request.Comment ?? "",
                    is_approved = true,      // TEST: Auto approve
                    status = "approved",     // TEST: Auto approve
                    created_at = DateTime.Now
                };

                _db.product_reviews.Add(review);
                _db.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"[CreateReview] Review created successfully");

                // Lấy thông tin user để trả về
                var user = _db.users.Find(userId.Value);

                // Kiểm tra verified purchase
                var isVerifiedPurchase = false;
                try
                {
                    isVerifiedPurchase = _db.purchase_orders
                        .Join(_db.purchase_order_details,
                            po => po.id,
                            pod => pod.order_id,
                            (po, pod) => new { po, pod })
                        .Join(_db.product_variants,
                            x => x.pod.product_variant_id,
                            pv => pv.id,
                            (x, pv) => new { x.po, pv })
                        .Any(x => x.po.user_id == userId.Value
                               && x.pv.product_id == productId
                               && x.po.status == "delivered");
                }
                catch { }

                var response = new ReviewResponse
                {
                    UserId = review.user_id,
                    ProductId = review.product_id,
                    UserName = user?.full_name ?? "Ẩn danh",
                    Rating = review.rating,
                    Comment = review.comment,
                    IsApproved = review.is_approved,
                    IsVerifiedPurchase = isVerifiedPurchase,
                    Status = review.status,
                    CreatedAt = review.created_at,
                    UpdatedAt = review.updated_at
                };

                return Ok(new
                {
                    Success = true,
                    Message = "Đánh giá đã được gửi thành công!",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateReview] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CreateReview] StackTrace: {ex.StackTrace}");
                return InternalServerError(new Exception("Đã xảy ra lỗi khi gửi đánh giá: " + ex.Message));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}