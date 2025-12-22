using System;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Security;
using System.Data.Entity;
using System.Net;
using DoAnLTWHQT.Models;

namespace DoAnLTWHQT.Controllers
{
    [RoutePrefix("api/cart")]
    public class ApiCartController : ApiController
    {
        private readonly perwEntities _db = new perwEntities();

        // =====================================================
        // Helper: Lay user ID tu FormsAuthentication
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

                // Lay email/username tu identity
                var userName = identity.Name;
                if (string.IsNullOrEmpty(userName)) return null;

                // Query database de lay user id
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
        // GET /api/cart
        // Lay danh sach san pham trong gio hang
        // ========================================
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetCart()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Content(HttpStatusCode.Unauthorized, new { Success = false, Message = "Vui long dang nhap" });
            }

            try
            {
                var cartItems = _db.carts
                    .Include(c => c.product_variants.product)
                    .Where(c => c.user_id == userId && c.deleted_at == null)
                    .Select(c => new
                    {
                        Id = c.id,
                        UserId = c.user_id,
                        ProductVariantId = c.product_variant_id,
                        Quantity = c.quantity,
                        Price = c.price,
                        CreatedAt = c.created_at,

                        // Thong tin san pham & Variant
                        ProductId = c.product_variants.product_id,
                        ProductName = c.product_variants.product.name,
                        ProductSlug = c.product_variants.product.slug,
                        VariantName = c.product_variants.name,
                        VariantSku = c.product_variants.sku,
                        VariantImageUrl = c.product_variants.image_url,
                        OriginalPrice = c.product_variants.original_price,

                        // Thong tin ton kho (Lay tu chi nhanh dau tien co hang)
                        BranchId = _db.branch_inventories
                            .Where(bi => bi.product_variant_id == c.product_variant_id && bi.quantity_on_hand > 0)
                            .Select(bi => bi.branch_id)
                            .FirstOrDefault(),
                        BranchName = _db.branch_inventories
                            .Where(bi => bi.product_variant_id == c.product_variant_id && bi.quantity_on_hand > 0)
                            .Select(bi => bi.branch.name)
                            .FirstOrDefault(),
                        StockQuantity = _db.branch_inventories
                            .Where(bi => bi.product_variant_id == c.product_variant_id)
                            .Sum(bi => (int?)bi.quantity_on_hand) ?? 0
                    })
                    .OrderByDescending(c => c.CreatedAt)
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Message = "Lay gio hang thanh cong",
                    Data = cartItems,
                    Total = cartItems.Count
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // POST /api/cart
        // Them san pham vao gio
        // ========================================
        [HttpPost]
        [Route("")]
        public IHttpActionResult AddToCart([FromBody] AddToCartRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Content(HttpStatusCode.Unauthorized, new { Success = false, Message = "Vui long dang nhap" });
            }

            if (request == null || request.ProductVariantId <= 0 || request.Quantity <= 0)
            {
                return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Du lieu khong hop le" });
            }

            try
            {
                var variant = _db.product_variants.Find(request.ProductVariantId);
                if (variant == null || variant.deleted_at != null)
                {
                    return Content(HttpStatusCode.NotFound, new { Success = false, Message = "San pham khong ton tai" });
                }

                // Kiem tra xem item nay da ton tai trong gio chua
                var existingItem = _db.carts
                    .FirstOrDefault(c => c.user_id == userId && c.product_variant_id == request.ProductVariantId && c.deleted_at == null);

                if (existingItem != null)
                {
                    existingItem.quantity += request.Quantity;
                    existingItem.updated_at = DateTime.Now;
                }
                else
                {
                    var newItem = new cart
                    {
                        user_id = userId.Value,
                        product_variant_id = request.ProductVariantId,
                        quantity = request.Quantity,
                        price = request.Price > 0 ? request.Price : variant.price,
                        created_at = DateTime.Now
                    };
                    _db.carts.Add(newItem);
                }

                _db.SaveChanges();
                return Ok(new { Success = true, Message = "Da them vao gio hang" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // PUT /api/cart/{id}
        // Cap nhat so luong item
        // ========================================
        [HttpPut]
        [Route("{id:long}")]
        public IHttpActionResult UpdateQuantity(long id, [FromBody] UpdateCartRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Content(HttpStatusCode.Unauthorized, new { Success = false, Message = "Vui long dang nhap" });
            }

            if (request == null || request.Quantity < 1)
            {
                return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "So luong khong hop le" });
            }

            try
            {
                var item = _db.carts.FirstOrDefault(c => c.id == id && c.user_id == userId && c.deleted_at == null);
                if (item == null)
                {
                    return Content(HttpStatusCode.NotFound, new { Success = false, Message = "Khong tim thay san pham trong gio" });
                }

                item.quantity = request.Quantity;
                item.updated_at = DateTime.Now;
                _db.SaveChanges();

                return Ok(new { Success = true, Message = "Da cap nhat so luong" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // DELETE /api/cart/{id}
        // Xoa item (Soft Delete)
        // ========================================
        [HttpDelete]
        [Route("{id:long}")]
        public IHttpActionResult RemoveItem(long id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Content(HttpStatusCode.Unauthorized, new { Success = false, Message = "Vui long dang nhap" });

            try
            {
                var item = _db.carts.FirstOrDefault(c => c.id == id && c.user_id == userId && c.deleted_at == null);
                if (item == null) return Content(HttpStatusCode.NotFound, new { Success = false, Message = "Khong tim thay san pham" });

                item.deleted_at = DateTime.Now;
                _db.SaveChanges();

                return Ok(new { Success = true, Message = "Da xoa san pham khoi gio hang" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // DELETE /api/cart/clear
        // Xoa tat ca gio hang (Soft Delete)
        // ========================================
        [HttpDelete]
        [Route("clear")]
        public IHttpActionResult ClearCart()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Content(HttpStatusCode.Unauthorized, new { Success = false, Message = "Vui long dang nhap" });

            try
            {
                var items = _db.carts.Where(c => c.user_id == userId && c.deleted_at == null).ToList();
                foreach (var item in items)
                {
                    item.deleted_at = DateTime.Now;
                }
                _db.SaveChanges();

                return Ok(new { Success = true, Message = "Da xoa tat ca gio hang" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/cart/count
        // Lay so luong badge
        // ========================================
        [HttpGet]
        [Route("count")]
        public IHttpActionResult GetCartCount()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Ok(new { Success = true, Data = new { ItemCount = 0, TotalQuantity = 0 } });
            }

            try
            {
                var items = _db.carts.Where(c => c.user_id == userId && c.deleted_at == null).ToList();
                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        ItemCount = items.Count,
                        TotalQuantity = items.Sum(i => i.quantity)
                    }
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
}
