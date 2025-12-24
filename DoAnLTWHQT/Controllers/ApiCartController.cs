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
                    System.Diagnostics.Debug.WriteLine("[GetCurrentUserId] Not authenticated");
                    return null;
                }

                var userName = identity.Name;
                System.Diagnostics.Debug.WriteLine($"[GetCurrentUserId] userName: {userName}");
                if (string.IsNullOrEmpty(userName)) return null;

                var user = _db.users.FirstOrDefault(u =>
                    (u.email == userName || u.name == userName) &&
                    u.deleted_at == null &&
                    u.status == "active");

                System.Diagnostics.Debug.WriteLine($"[GetCurrentUserId] Found user: {user?.id}");
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
                // Tra ve gio hang rong thay vi 401 de tranh CORS issues
                return Ok(new
                {
                    Success = true,
                    Message = "Chua dang nhap - gio hang rong",
                    Data = new object[] { },
                    Total = 0,
                    IsAuthenticated = false
                });
            }

            try
            {
                // Step 1: Load cart items with related data (NO branch navigation - not in EDMX)
                var cartEntities = _db.carts
                    .Include(c => c.product_variants.product)
                    .Where(c => c.user_id == userId && c.deleted_at == null)
                    .OrderByDescending(c => c.created_at)
                    .ToList();

                // Step 2: Get distinct branch IDs from cart items
                var branchIds = cartEntities.Select(c => c.branch_id).Distinct().ToList();
                
                // Step 3: Load branches for lookup
                var branches = _db.branches
                    .Where(b => branchIds.Contains(b.id))
                    .ToDictionary(b => b.id, b => b.name);

                // Step 4: Get all variant IDs for stock lookup
                var variantIds = cartEntities.Select(c => c.product_variant_id).Distinct().ToList();

                // Step 5: Load stock quantities
                var stocks = _db.branch_inventories
                    .Where(bi => variantIds.Contains(bi.product_variant_id))
                    .ToList();

                // Step 6: Map to response DTOs
                var cartItems = cartEntities.Select(c => {
                    var branchId = c.branch_id;
                    var branchName = branches.ContainsKey(branchId) ? branches[branchId] : "Không xác định";
                    var stock = stocks.FirstOrDefault(s => s.product_variant_id == c.product_variant_id && s.branch_id == branchId);
                    
                    return new
                    {
                        Id = c.id,
                        UserId = c.user_id,
                        ProductVariantId = c.product_variant_id,
                        Quantity = c.quantity,
                        Price = c.price,
                        CreatedAt = c.created_at,

                        // Thong tin san pham & Variant
                        ProductId = c.product_variants?.product_id ?? 0,
                        ProductName = c.product_variants?.product?.name ?? "",
                        ProductSlug = c.product_variants?.product?.slug ?? "",
                        VariantName = c.product_variants?.name ?? "",
                        VariantSku = c.product_variants?.sku ?? "",
                        VariantImageUrl = c.product_variants?.image_url ?? "",
                        OriginalPrice = c.product_variants?.original_price,

                        // Branch info - from manual lookup
                        BranchId = branchId,
                        BranchName = branchName,
                        // Stock quantity at this specific branch
                        StockQuantity = stock?.quantity_on_hand ?? 0
                    };
                }).ToList();

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
                System.Diagnostics.Debug.WriteLine($"[GetCart] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[GetCart] StackTrace: {ex.StackTrace}");
                // Return detailed error for debugging
                return Ok(new
                {
                    Success = false,
                    Message = "Loi khi lay gio hang: " + ex.Message,
                    Error = ex.ToString(),
                    InnerError = ex.InnerException?.Message
                });
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
                return Ok(new { Success = false, Message = "Chua dang nhap", IsAuthenticated = false });
            }

            if (request == null || request.ProductVariantId <= 0 || request.Quantity <= 0)
            {
                return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Du lieu khong hop le" });
            }

            // Validate BranchId
            if (request.BranchId <= 0)
            {
                return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Vui long chon chi nhanh" });
            }

            try
            {
                // 1. Kiem tra variant ton tai
                var variant = _db.product_variants.Find(request.ProductVariantId);
                if (variant == null || variant.deleted_at != null)
                {
                    return Content(HttpStatusCode.NotFound, new { Success = false, Message = "San pham khong ton tai" });
                }

                // 2. Kiem tra branch ton tai
                var branch = _db.branches.Find(request.BranchId);
                if (branch == null)
                {
                    return Content(HttpStatusCode.NotFound, new { Success = false, Message = "Chi nhanh khong ton tai" });
                }

                // 3. Kiem tra ton kho tai branch nay
                var inventory = _db.branch_inventories
                    .FirstOrDefault(bi => bi.product_variant_id == request.ProductVariantId && bi.branch_id == request.BranchId);
                
                if (inventory == null || inventory.quantity_on_hand < request.Quantity)
                {
                    var availableStock = inventory?.quantity_on_hand ?? 0;
                    return Ok(new { 
                        Success = false, 
                        Message = $"San pham khong du ton kho tai chi nhanh {branch.name}. Hien co: {availableStock}" 
                    });
                }

                // 4. Load existing cart items (for checking duplicate variant+branch)
                // NOTE: Cart NOW ALLOWS products from MULTIPLE branches
                // Products will be grouped by branch in frontend
                // At checkout, only same-branch products go into one purchase order
                var existingCartItems = _db.carts
                    .Where(c => c.user_id == userId && c.deleted_at == null)
                    .ToList();

                // 5. Kiem tra xem item nay da ton tai trong gio chua (cung variant + cung branch)
                var existingItem = existingCartItems
                    .FirstOrDefault(c => c.product_variant_id == request.ProductVariantId && c.branch_id == request.BranchId);

                if (existingItem != null)
                {
                    // Kiem tra tong so luong sau khi update co vuot qua ton kho khong
                    var newQuantity = existingItem.quantity + request.Quantity;
                    if (newQuantity > inventory.quantity_on_hand)
                    {
                        return Ok(new { 
                            Success = false, 
                            Message = $"Tong so luong ({newQuantity}) vuot qua ton kho ({inventory.quantity_on_hand})" 
                        });
                    }
                    
                    existingItem.quantity = newQuantity;
                    existingItem.updated_at = DateTime.Now;
                }
                else
                {
                    var newItem = new cart
                    {
                        user_id = userId.Value,
                        product_variant_id = request.ProductVariantId,
                        branch_id = request.BranchId,
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
                System.Diagnostics.Debug.WriteLine($"[AddToCart] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AddToCart] StackTrace: {ex.StackTrace}");
                
                // Get full inner exception chain
                var innerMsg = ex.InnerException?.Message ?? "";
                var innerInner = ex.InnerException?.InnerException?.Message ?? "";
                
                // Return detailed error for debugging (don't use 500)
                return Ok(new { 
                    Success = false, 
                    Message = "Loi them san pham: " + ex.Message,
                    Error = ex.ToString(),
                    InnerError = innerMsg,
                    InnerInnerError = innerInner
                });
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
                return Ok(new { Success = false, Message = "Chua dang nhap", IsAuthenticated = false });
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
            if (!userId.HasValue) return Ok(new { Success = false, Message = "Chua dang nhap", IsAuthenticated = false });

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
            if (!userId.HasValue) return Ok(new { Success = false, Message = "Chua dang nhap", IsAuthenticated = false });

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
