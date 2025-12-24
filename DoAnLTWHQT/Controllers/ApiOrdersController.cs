using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Security;
using System.Data.Entity;
using System.Net;
using DoAnLTWHQT.Models;

namespace DoAnLTWHQT.Controllers
{
    /// <summary>
    /// API Controller for order management (Client role)
    /// </summary>
    [RoutePrefix("api/orders")]
    public class ApiOrdersController : ApiController
    {
        private readonly perwEntities _db = new perwEntities();

        // =====================================================
        // Helper: Lấy User ID từ FormsAuthentication ticket
        // =====================================================
        private long? GetCurrentUserId()
        {
            try
            {
                var identity = HttpContext.Current?.User?.Identity;
                if (identity == null || !identity.IsAuthenticated)
                {
                    System.Diagnostics.Debug.WriteLine("[ApiOrders.GetCurrentUserId] Not authenticated");
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
                System.Diagnostics.Debug.WriteLine($"[ApiOrders.GetCurrentUserId] Error: {ex.Message}");
                return null;
            }
        }

        // =====================================================
        // GET /api/orders - Lấy danh sách đơn hàng của user
        // =====================================================
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetMyOrders()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Ok(new { Success = false, Message = "Vui lòng đăng nhập", IsAuthenticated = false });
            }

            try
            {
                var orders = _db.purchase_orders
                    .Where(o => o.user_id == userId.Value && o.deleted_at == null)
                    .OrderByDescending(o => o.created_at)
                    .ToList()
                    .Select(o => new
                    {
                        Id = o.id,
                        OrderCode = o.order_code,
                        UserId = o.user_id,
                        Status = o.status,
                        ShippingRecipientName = o.shipping_recipient_name,
                        ShippingRecipientPhone = o.shipping_recipient_phone,
                        ShippingAddress = o.shipping_address,
                        SubTotal = o.sub_total,
                        ShippingFee = o.shipping_fee,
                        DiscountAmount = o.discount_amount,
                        TotalAmount = o.total_amount,
                        BranchId = o.branch_id,
                        BranchName = GetBranchName(o.branch_id),
                        CreatedAt = o.created_at,
                        UpdatedAt = o.updated_at,

                        // Load Items từ purchase_order_details
                        Items = _db.purchase_order_details
                            .Where(d => d.order_id == o.id)
                            .Select(d => new
                            {
                                ProductVariantId = d.product_variant_id,
                                ProductId = d.product_variants != null ? d.product_variants.product_id : 0,
                                ProductName = d.product_variants != null && d.product_variants.product != null
                                    ? d.product_variants.product.name : "",
                                VariantName = d.product_variants != null ? d.product_variants.name : "",
                                Quantity = d.quantity,
                                PriceAtPurchase = d.price_at_purchase,
                                Subtotal = d.subtotal,
                                ImageUrl = d.product_variants != null ? d.product_variants.image_url : ""
                            }).ToList(),

                        // Load Payment
                        Payment = _db.payments
                            .Where(p => p.order_id == o.id && p.deleted_at == null)
                            .Select(p => new
                            {
                                Id = p.id,
                                PaymentMethodId = p.payment_method_id,
                                MethodName = p.payment_methods != null ? p.payment_methods.name : "COD",
                                Status = p.status,
                                Amount = p.amount,
                                TransactionCode = p.transaction_code,
                                CreatedAt = p.created_at
                            }).FirstOrDefault()
                    })
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Data = orders,
                    Total = orders.Count,
                    Message = "Lấy danh sách đơn hàng thành công"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetMyOrders] Error: {ex.Message}");
                return Ok(new
                {
                    Success = false,
                    Message = "Lỗi khi lấy danh sách đơn hàng: " + ex.Message,
                    Error = ex.ToString()
                });
            }
        }

        // Helper to get branch name
        private string GetBranchName(long? branchId)
        {
            if (!branchId.HasValue) return "Chi nhánh chính";
            var branch = _db.branches.Find(branchId.Value);
            return branch?.name ?? "Chi nhánh chính";
        }

        // =====================================================
        // POST /api/orders - Tạo đơn hàng mới
        // =====================================================
        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateOrder([FromBody] CreateOrderRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Ok(new { Success = false, Message = "Vui lòng đăng nhập", IsAuthenticated = false });
            }

            if (request == null)
            {
                return Ok(new { Success = false, Message = "Dữ liệu không hợp lệ" });
            }

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] Starting for user: {userId.Value}");

                    // 1. Get branch_id from request (required for multi-branch cart)
                    long branchId = request.BranchId ?? 0;
                    if (branchId <= 0)
                    {
                        return Ok(new { Success = false, Message = "Vui lòng chọn chi nhánh để thanh toán" });
                    }

                    // 2. Lấy giỏ hàng của user CHỈ TỪ BRANCH ĐÃ CHỌN
                    var allCartItems = _db.carts
                        .Include(c => c.product_variants)
                        .Where(c => c.user_id == userId.Value && c.deleted_at == null)
                        .ToList();

                    // Filter by selected branch (in memory because branch_id may not work in EF query)
                    var cartItems = allCartItems.Where(c => c.branch_id == branchId).ToList();

                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] Found {cartItems.Count} cart items for branch {branchId}");

                    if (!cartItems.Any())
                    {
                        return Ok(new { Success = false, Message = "Không có sản phẩm nào từ chi nhánh đã chọn trong giỏ hàng" });
                    }

                    // 3. Kiểm tra payment method tồn tại
                    long paymentMethodId = request.PaymentMethodId ?? 1; // Mặc định là COD
                    var paymentMethod = _db.payment_methods.FirstOrDefault(pm => pm.id == paymentMethodId && pm.deleted_at == null && pm.is_active);
                    if (paymentMethod == null)
                    {
                        paymentMethod = _db.payment_methods.FirstOrDefault(pm => pm.deleted_at == null && pm.is_active);
                        if (paymentMethod == null)
                        {
                            return Ok(new { Success = false, Message = "Không có phương thức thanh toán khả dụng" });
                        }
                        paymentMethodId = paymentMethod.id;
                    }

                    // 4. Gộp các cart items có cùng product_variant_id
                    var groupedItems = cartItems
                        .GroupBy(c => c.product_variant_id)
                        .Select(g => new
                        {
                            ProductVariantId = g.Key,
                            Quantity = g.Sum(x => x.quantity),
                            Price = g.First().price,
                            ProductVariant = g.First().product_variants
                        })
                        .ToList();

                    // 5. Tính toán tổng tiền
                    decimal subTotal = groupedItems.Sum(c => c.Price * c.Quantity);
                    decimal shippingFee = request.ShippingFee ?? 30000;
                    decimal discountAmount = request.DiscountAmount ?? 0;
                    decimal totalAmount = subTotal + shippingFee - discountAmount;

                    // 6. Tạo mã đơn hàng unique
                    string orderCode = "ORD-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + new Random().Next(1000, 9999);

                    // 7. Tạo đơn hàng
                    var order = new purchase_orders
                    {
                        user_id = userId.Value,
                        order_code = orderCode,
                        status = "pending",
                        shipping_recipient_name = request.ShippingRecipientName ?? "",
                        shipping_recipient_phone = request.ShippingRecipientPhone ?? "",
                        shipping_address = request.ShippingAddress ?? "",
                        sub_total = subTotal,
                        shipping_fee = shippingFee,
                        discount_amount = discountAmount,
                        total_amount = totalAmount,
                        branch_id = branchId,
                        discount_id = request.DiscountId,
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };
                    _db.purchase_orders.Add(order);
                    _db.SaveChanges();

                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] Created order with ID: {order.id}");

                    // 8. Tạo chi tiết đơn hàng
                    foreach (var item in groupedItems)
                    {
                        var detail = new purchase_order_details
                        {
                            order_id = order.id,
                            product_variant_id = item.ProductVariantId,
                            quantity = item.Quantity,
                            price_at_purchase = item.Price,
                            subtotal = item.Price * item.Quantity,
                            created_at = DateTime.Now
                        };
                        _db.purchase_order_details.Add(detail);
                    }

                    // 9. Tạo record thanh toán
                    var payment = new payment
                    {
                        order_id = order.id,
                        payment_method_id = paymentMethodId,
                        amount = totalAmount,
                        status = "pending",
                        created_at = DateTime.Now
                    };
                    _db.payments.Add(payment);

                    // 10. Xóa giỏ hàng (soft delete)
                    foreach (var item in cartItems)
                    {
                        item.deleted_at = DateTime.Now;
                    }

                    _db.SaveChanges();
                    transaction.Commit();

                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] Success! Order ID: {order.id}");

                    return Ok(new
                    {
                        Success = true,
                        Message = "Đặt hàng thành công",
                        Data = new
                        {
                            Id = order.id,
                            OrderCode = order.order_code,
                            TotalAmount = order.total_amount,
                            Status = order.status,
                            BranchId = order.branch_id
                        }
                    });
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    transaction.Rollback();
                    var errors = dbEx.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => $"{x.PropertyName}: {x.ErrorMessage}");
                    var errorMessage = string.Join("; ", errors);
                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] Validation Error: {errorMessage}");
                    return Ok(new { Success = false, Message = "Lỗi validation: " + errorMessage });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] Error: {ex.Message}");
                    return Ok(new
                    {
                        Success = false,
                        Message = "Lỗi tạo đơn hàng: " + ex.Message,
                        Error = ex.ToString()
                    });
                }
            }
        }

        // =====================================================
        // GET /api/orders/{id} - Chi tiết đơn hàng cụ thể
        // =====================================================
        [HttpGet]
        [Route("{id:long}")]
        public IHttpActionResult GetOrderById(long id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Ok(new { Success = false, Message = "Vui lòng đăng nhập", IsAuthenticated = false });
            }

            try
            {
                var order = _db.purchase_orders
                    .Where(o => o.id == id && o.user_id == userId.Value && o.deleted_at == null)
                    .ToList()
                    .Select(o => new
                    {
                        Id = o.id,
                        OrderCode = o.order_code,
                        Status = o.status,
                        ShippingRecipientName = o.shipping_recipient_name,
                        ShippingRecipientPhone = o.shipping_recipient_phone,
                        ShippingAddress = o.shipping_address,
                        SubTotal = o.sub_total,
                        ShippingFee = o.shipping_fee,
                        DiscountAmount = o.discount_amount,
                        TotalAmount = o.total_amount,
                        BranchId = o.branch_id,
                        BranchName = GetBranchName(o.branch_id),
                        CreatedAt = o.created_at,
                        UpdatedAt = o.updated_at,
                        Items = _db.purchase_order_details
                            .Where(d => d.order_id == o.id && d.deleted_at == null)
                            .Select(d => new
                            {
                                ProductVariantId = d.product_variant_id,
                                ProductName = d.product_variants.product.name,
                                VariantName = d.product_variants.name,
                                Quantity = d.quantity,
                                Price = d.price_at_purchase,
                                Subtotal = d.subtotal,
                                ImageUrl = d.product_variants.image_url
                            }).ToList(),
                        Payment = _db.payments
                            .Where(p => p.order_id == o.id && p.deleted_at == null)
                            .Select(p => new
                            {
                                Id = p.id,
                                MethodName = p.payment_methods.name,
                                Status = p.status,
                                Amount = p.amount
                            }).FirstOrDefault()
                    })
                    .FirstOrDefault();

                if (order == null)
                {
                    return Ok(new { Success = false, Message = "Không tìm thấy đơn hàng" });
                }

                return Ok(new { Success = true, Data = order });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = "Lỗi: " + ex.Message });
            }
        }

        // =====================================================
        // PUT /api/orders/{id}/cancel - Hủy đơn hàng
        // =====================================================
        [HttpPut]
        [Route("{id:long}/cancel")]
        public IHttpActionResult CancelOrder(long id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Ok(new { Success = false, Message = "Vui lòng đăng nhập", IsAuthenticated = false });
            }

            try
            {
                var order = _db.purchase_orders
                    .FirstOrDefault(o => o.id == id && o.user_id == userId.Value && o.deleted_at == null);

                if (order == null)
                {
                    return Ok(new { Success = false, Message = "Không tìm thấy đơn hàng" });
                }

                // Chỉ cho phép hủy khi đơn hàng ở trạng thái 'pending'
                if (order.status != "pending")
                {
                    return Ok(new
                    {
                        Success = false,
                        Message = "Chỉ có thể hủy đơn hàng đang chờ xác nhận."
                    });
                }

                order.status = "cancelled";
                order.updated_at = DateTime.Now;

                _db.SaveChanges();

                return Ok(new { Success = true, Message = "Đã hủy đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = "Lỗi: " + ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }

    // =====================================================
    // Request Model
    // =====================================================
    public class CreateOrderRequest
    {
        public long? BranchId { get; set; } // Required - which branch to checkout from
        public string ShippingRecipientName { get; set; }
        public string ShippingRecipientPhone { get; set; }
        public string ShippingAddress { get; set; }
        public long? PaymentMethodId { get; set; }
        public decimal? ShippingFee { get; set; }
        public decimal? DiscountAmount { get; set; }
        public long? DiscountId { get; set; }
    }
}
