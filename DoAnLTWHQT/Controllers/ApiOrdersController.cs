using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Security;
using System.Data.Entity;
using System.Net;
using DoAnLTWHQT.Models; // Đảm bảo namespace này khớp với project của bạn

namespace DoAnLTWHQT.Controllers.Api
{
    [RoutePrefix("api/orders")]
    public class ApiOrdersController : ApiController
    {
        private readonly Entities db = new Entities();

        // =====================================================
        // Helper: Lấy User ID từ FormsAuthentication ticket
        // =====================================================
        private long? GetCurrentUserId()
        {
            var authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie == null) return null;

            try
            {
                var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                if (ticket == null || ticket.Expired) return null;

                // ticket.Name chứa email của user
                if (!string.IsNullOrEmpty(ticket.Name))
                {
                    var user = db.users.FirstOrDefault(u => u.email == ticket.Name && u.deleted_at == null);
                    if (user != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ApiOrders] Found user by email: {user.id}");
                        return user.id;  // Trả về long trực tiếp
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiOrders] Exception: {ex.Message}");
            }

            return null;
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
                return Content(HttpStatusCode.Unauthorized, new { Success = false, Message = "Vui lòng đăng nhập" });
            }

            try
            {
                var orders = db.purchase_orders
                    .Where(o => o.user_id == userId.Value && o.deleted_at == null)
                    .OrderByDescending(o => o.created_at)
                    .ToList() // Lấy data trước
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
                        BranchName = o.branch != null ? o.branch.name : "Chi nhánh chính",
                        CreatedAt = o.created_at,
                        UpdatedAt = o.updated_at,

                        // Load Items từ purchase_order_details
                        Items = db.purchase_order_details
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
                        Payment = db.payments
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
                    Message = "Lấy danh sách đơn hàng thành công"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetMyOrders] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[GetMyOrders] StackTrace: {ex.StackTrace}");
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateOrder([FromBody] CreateOrderRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Content(HttpStatusCode.Unauthorized, new { Success = false, Message = "Vui lòng đăng nhập" });
            }

            // Validate request
            if (request == null)
            {
                return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Dữ liệu không hợp lệ" });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] Starting for user: {userId.Value}");

                    // 1. Lấy giỏ hàng của user
                    var cartItems = db.carts
                        .Include(c => c.product_variants)
                        .Where(c => c.user_id == userId.Value && c.deleted_at == null)
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] Found {cartItems.Count} cart items");

                    if (!cartItems.Any())
                    {
                        return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Giỏ hàng trống" });
                    }

                    // 2. Kiểm tra payment method tồn tại
                    long paymentMethodId = request.PaymentMethodId ?? 1; // Mặc định là COD
                    var paymentMethod = db.payment_methods.FirstOrDefault(pm => pm.id == paymentMethodId && pm.deleted_at == null && pm.is_active);
                    if (paymentMethod == null)
                    {
                        // Fallback: Lấy payment method đầu tiên có sẵn
                        paymentMethod = db.payment_methods.FirstOrDefault(pm => pm.deleted_at == null && pm.is_active);
                        if (paymentMethod == null)
                        {
                            return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Không có phương thức thanh toán khả dụng" });
                        }
                        paymentMethodId = paymentMethod.id;
                    }

                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] Using payment method: {paymentMethodId}");

                    // 3. Gộp các cart items có cùng product_variant_id (tránh lỗi composite key)
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

                    // 4. Tính toán tổng tiền
                    decimal subTotal = groupedItems.Sum(c => c.Price * c.Quantity);
                    decimal shippingFee = 30000; // Phí ship cố định
                    decimal discountAmount = 0;
                    decimal totalAmount = subTotal + shippingFee - discountAmount;

                    // 5. Tạo mã đơn hàng unique
                    string orderCode = "ORD-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + new Random().Next(1000, 9999);

                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] Order code: {orderCode}, Total: {totalAmount}");

                    // 6. Tạo đơn hàng
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
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };
                    db.purchase_orders.Add(order);
                    db.SaveChanges(); // Lưu để lấy ID

                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] Created order with ID: {order.id}");

                    // 7. Tạo chi tiết đơn hàng (từ grouped items để tránh duplicate key)
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
                        db.purchase_order_details.Add(detail);
                    }

                    // 8. Tạo record thanh toán
                    var payment = new payment
                    {
                        order_id = order.id,
                        payment_method_id = paymentMethodId,
                        amount = totalAmount,
                        status = "pending",
                        created_at = DateTime.Now
                    };
                    db.payments.Add(payment);

                    // 9. Xóa giỏ hàng (soft delete)
                    foreach (var item in cartItems)
                    {
                        item.deleted_at = DateTime.Now;
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] Success! Order ID: {order.id}");

                    // 10. Trả về thông tin đơn hàng
                    return Ok(new
                    {
                        Success = true,
                        Message = "Đặt hàng thành công",
                        Data = new
                        {
                            Id = order.id,
                            OrderCode = order.order_code,
                            TotalAmount = order.total_amount,
                            Status = order.status
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
                    return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Lỗi validation: " + errorMessage });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] Error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] StackTrace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CreateOrder] InnerException: {ex.InnerException.Message}");
                        if (ex.InnerException.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CreateOrder] InnerInner: {ex.InnerException.InnerException.Message}");
                        }
                    }
                    return Content(HttpStatusCode.InternalServerError, new
                    {
                        Success = false,
                        Message = "Lỗi tạo đơn hàng: " + ex.Message,
                        Inner = ex.InnerException?.Message
                    });
                }
            }
        }

        // =====================================================
        // GET /api/orders/{id} - Chi tiết đơn hàng cụ thể
        // =====================================================
        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetOrderById(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            try
            {
                // Tái sử dụng logic projection để đảm bảo cấu trúc dữ liệu đồng nhất
                var order = db.purchase_orders
                    .Where(o => o.id == id && o.user_id == userId.Value && o.deleted_at == null)
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
                        CreatedAt = o.created_at,
                        Items = o.purchase_order_details
                            .Where(d => d.deleted_at == null)
                            .Select(d => new {
                                ProductName = d.product_variants.product.name,
                                VariantName = d.product_variants.name,
                                Quantity = d.quantity,
                                Price = d.price_at_purchase,
                                ImageUrl = d.product_variants.image_url
                            }).ToList()
                    })
                    .FirstOrDefault();

                if (order == null) return NotFound();

                return Ok(new { Success = true, Data = order });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // =====================================================
        // PUT /api/orders/{id}/cancel - Hủy đơn hàng
        // =====================================================
        [HttpPut]
        [Route("{id:int}/cancel")]
        public IHttpActionResult CancelOrder(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            try
            {
                var order = db.purchase_orders
                    .FirstOrDefault(o => o.id == id && o.user_id == userId.Value && o.deleted_at == null);

                if (order == null) return NotFound();

                // Chỉ cho phép hủy khi đơn hàng ở trạng thái 'pending'
                if (order.status != "pending")
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        Success = false,
                        Message = "Chỉ có thể hủy đơn hàng đang chờ xác nhận."
                    });
                }

                order.status = "cancelled";
                order.updated_at = DateTime.Now;

                db.SaveChanges();

                return Ok(new { Success = true, Message = "Đã hủy đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}