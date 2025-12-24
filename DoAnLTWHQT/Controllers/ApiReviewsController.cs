using System;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Net;
using System.Data.Entity;

namespace DoAnLTWHQT.Controllers
{
    /// <summary>
    /// API Controller for product reviews
    /// Note: product_reviews uses composite key (user_id, product_id)
    /// </summary>
    [RoutePrefix("api/reviews")]
    public class ApiReviewsController : ApiController
    {
        private readonly perwEntities _db = new perwEntities();

        // =====================================================
        // Helper: Get current user ID from FormsAuthentication
        // =====================================================
        private long? GetCurrentUserId()
        {
            try
            {
                var identity = HttpContext.Current?.User?.Identity;
                if (identity == null || !identity.IsAuthenticated)
                {
                    return null;
                }

                var userName = identity.Name;
                if (string.IsNullOrEmpty(userName)) return null;

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

        // ========================================
        // GET /api/reviews/my
        // Get current user's reviews
        // ========================================
        [HttpGet]
        [Route("my")]
        public IHttpActionResult GetMyReviews()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                // Return empty list instead of 401 to avoid CORS issues
                return Ok(new
                {
                    Success = true,
                    Message = "Chua dang nhap",
                    Data = new System.Collections.Generic.List<object>(),
                    Total = 0
                });
            }

            try
            {
                var reviews = _db.product_reviews
                    .Where(r => r.user_id == userId && r.deleted_at == null)
                    .Include(r => r.product)
                    .OrderByDescending(r => r.created_at)
                    .ToList()
                    .Select(r => new
                    {
                        UserId = r.user_id,
                        ProductId = r.product_id,
                        ProductName = r.product != null ? r.product.name : "",
                        ProductImage = r.product != null && r.product.product_variants.Any()
                            ? r.product.product_variants.FirstOrDefault()?.image_url
                            : null,
                        Rating = r.rating,
                        Comment = r.comment,
                        IsApproved = r.is_approved,
                        Status = r.status ?? (r.is_approved ? "approved" : "pending"),
                        CreatedAt = r.created_at
                    })
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Message = "Lay danh sach danh gia thanh cong",
                    Data = reviews,
                    Total = reviews.Count
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/reviews/product/{productId}
        // Get reviews for a product
        // ========================================
        [HttpGet]
        [Route("product/{productId:long}")]
        public IHttpActionResult GetProductReviews(long productId)
        {
            try
            {
                var reviews = _db.product_reviews
                    .Where(r => r.product_id == productId && r.deleted_at == null)
                    .Include(r => r.user)
                    .OrderByDescending(r => r.created_at)
                    .ToList()
                    .Select(r => new
                    {
                        UserId = r.user_id,
                        ProductId = r.product_id,
                        UserName = r.user != null ? r.user.full_name : "Khách hàng",
                        Rating = r.rating,
                        Comment = r.comment,
                        IsApproved = r.is_approved,
                        Status = r.status,
                        CreatedAt = r.created_at
                    })
                    .ToList();

                // Calculate average rating
                var avgRating = reviews.Any() ? reviews.Average(r => (double)r.Rating) : 0;

                return Ok(new
                {
                    Success = true,
                    Message = "Lay danh gia san pham thanh cong",
                    Data = reviews,
                    Total = reviews.Count,
                    AverageRating = Math.Round(avgRating, 1)
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/reviews/can-review/{productId}
        // Check if user can review a product
        // ========================================
        [HttpGet]
        [Route("can-review/{productId:long}")]
        public IHttpActionResult CanReview(long productId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Ok(new
                {
                    Success = true,
                    CanReview = false,
                    HasPurchased = false,
                    HasReviewed = false,
                    Message = "Chua dang nhap"
                });
            }

            try
            {
                // 1. Check if user already reviewed this product
                bool hasReviewed = _db.product_reviews.Any(r =>
                    r.user_id == userId.Value &&
                    r.product_id == productId &&
                    r.deleted_at == null);

                // 2. Check if user purchased the product
                // Must have at least one order with status 'completed' or 'delivered'
                // containing a variant of the product
                bool hasPurchased = _db.purchase_orders
                    .Where(o => o.user_id == userId.Value &&
                           (o.status == "completed" || o.status == "delivered" || o.status == "shipped") && // Allow shipped too for testing
                           o.deleted_at == null)
                    .Any(o => _db.purchase_order_details
                        .Any(d => d.order_id == o.id &&
                                  d.product_variants.product_id == productId &&
                                  d.deleted_at == null));

                return Ok(new
                {
                    Success = true,
                    CanReview = hasPurchased && !hasReviewed,
                    HasPurchased = hasPurchased,
                    HasReviewed = hasReviewed
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateReview([FromBody] CreateReviewRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Ok(new
                {
                    Success = false,
                    Message = "Chua dang nhap",
                    IsAuthenticated = false
                });
            }

            if (request == null || request.ProductId <= 0 || request.Rating < 1 || request.Rating > 5)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    Success = false,
                    Message = "Du lieu khong hop le"
                });
            }

            try
            {
                // Check if product exists
                var product = _db.products.FirstOrDefault(p => p.id == request.ProductId && p.deleted_at == null);
                if (product == null)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        Success = false,
                        Message = "Khong tim thay san pham"
                    });
                }

                // Check if user already reviewed this product
                var existingReview = _db.product_reviews
                    .FirstOrDefault(r => r.user_id == userId && r.product_id == request.ProductId && r.deleted_at == null);
                if (existingReview != null)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        Success = false,
                        Message = "Ban da danh gia san pham nay roi"
                    });
                }

                var review = new product_reviews
                {
                    user_id = userId.Value,
                    product_id = request.ProductId,
                    rating = (byte)request.Rating,
                    comment = request.Comment ?? "",
                    is_approved = false,
                    status = "pending",
                    created_at = DateTime.Now
                };

                _db.product_reviews.Add(review);
                _db.SaveChanges();

                return Ok(new
                {
                    Success = true,
                    Message = "Danh gia da duoc gui, dang cho duyet",
                    Data = new
                    {
                        UserId = review.user_id,
                        ProductId = review.product_id,
                        Rating = review.rating,
                        Comment = review.comment,
                        Status = "pending",
                        CreatedAt = review.created_at
                    }
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // PUT /api/reviews/{productId}
        // Update a review (using productId since composite key)
        // ========================================
        [HttpPut]
        [Route("{productId:long}")]
        public IHttpActionResult UpdateReview(long productId, [FromBody] UpdateReviewRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Ok(new
                {
                    Success = false,
                    Message = "Chua dang nhap",
                    IsAuthenticated = false
                });
            }

            try
            {
                // Find review by composite key (user_id + product_id)
                var review = _db.product_reviews.FirstOrDefault(r =>
                    r.user_id == userId && r.product_id == productId && r.deleted_at == null);

                if (review == null)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        Success = false,
                        Message = "Khong tim thay danh gia"
                    });
                }

                if (request.Rating.HasValue && request.Rating >= 1 && request.Rating <= 5)
                {
                    review.rating = (byte)request.Rating.Value;
                }

                if (!string.IsNullOrEmpty(request.Comment))
                {
                    review.comment = request.Comment;
                }

                // Reset approval when updated
                review.is_approved = false;
                review.status = "pending";
                review.updated_at = DateTime.Now;
                _db.SaveChanges();

                return Ok(new
                {
                    Success = true,
                    Message = "Cap nhat danh gia thanh cong",
                    Data = new
                    {
                        UserId = review.user_id,
                        ProductId = review.product_id,
                        Rating = review.rating,
                        Comment = review.comment,
                        Status = "pending"
                    }
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // DELETE /api/reviews/{productId}
        // Delete a review (soft delete)
        // ========================================
        [HttpDelete]
        [Route("{productId:long}")]
        public IHttpActionResult DeleteReview(long productId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Ok(new
                {
                    Success = false,
                    Message = "Chua dang nhap",
                    IsAuthenticated = false
                });
            }

            try
            {
                // Find review by composite key (user_id + product_id)
                var review = _db.product_reviews.FirstOrDefault(r =>
                    r.user_id == userId && r.product_id == productId && r.deleted_at == null);

                if (review == null)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        Success = false,
                        Message = "Khong tim thay danh gia"
                    });
                }

                // Soft delete
                review.deleted_at = DateTime.Now;
                _db.SaveChanges();

                return Ok(new
                {
                    Success = true,
                    Message = "Xoa danh gia thanh cong"
                });
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

    // ========================================
    // Request Models
    // ========================================
    public class CreateReviewRequest
    {
        public long ProductId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
    }

    public class UpdateReviewRequest
    {
        public int? Rating { get; set; }
        public string Comment { get; set; }
    }
}
