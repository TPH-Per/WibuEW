using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using DoAnLTWHQT.Models;

namespace DoAnLTWHQT.Controllers
{
    [RoutePrefix("api/discounts")]
    public class ApiDiscountsController : ApiController
    {
        private readonly Entities _db = new Entities();

        // ========================================
        // GET /api/discounts/active
        // Lấy danh sách mã giảm giá đang có hiệu lực cho khách hàng
        // ========================================
        [HttpGet]
        [Route("active")]
        public IHttpActionResult GetActiveDiscounts()
        {
            try
            {
                var now = DateTime.Now;

                var discounts = _db.discounts
                    .Where(d => d.is_active == true &&
                                (d.start_at == null || d.start_at <= now) &&
                                (d.end_at == null || d.end_at >= now) &&
                                (d.max_uses == null || d.used_count < d.max_uses))
                    .OrderByDescending(d => d.value)
                    .Select(d => new
                    {
                        Id = d.id,
                        Code = d.code,
                        Type = d.type,
                        Value = d.value,
                        MinOrderAmount = d.min_order_amount,
                        MaxUses = d.max_uses,
                        UsedCount = d.used_count,
                        StartAt = d.start_at,
                        EndAt = d.end_at,
                        IsActive = d.is_active
                    })
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Message = "Lấy danh sách mã giảm giá thành công",
                    Data = discounts,
                    Total = discounts.Count
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetActiveDiscounts Error: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/discounts
        // Lấy toàn bộ mã giảm giá (Dành cho trang quản trị)
        // ========================================
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var discounts = _db.discounts
                    .OrderByDescending(d => d.created_at)
                    .Select(d => new
                    {
                        Id = d.id,
                        Code = d.code,
                        Type = d.type,
                        Value = d.value,
                        MinOrderAmount = d.min_order_amount,
                        MaxUses = d.max_uses,
                        UsedCount = d.used_count,
                        StartAt = d.start_at,
                        EndAt = d.end_at,
                        IsActive = d.is_active,
                        CreatedAt = d.created_at
                    })
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Data = discounts,
                    Total = discounts.Count
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/discounts/validate/{code}
        // Kiểm tra tính hợp lệ của một mã cụ thể
        // ========================================
        [HttpGet]
        [Route("validate/{code}")]
        public IHttpActionResult ValidateCode(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Vui lòng nhập mã giảm giá" });
                }

                var now = DateTime.Now;
                var normalizedCode = code.Trim().ToUpper();

                var discount = _db.discounts
                    .FirstOrDefault(d => d.code.ToUpper() == normalizedCode && d.is_active == true);

                if (discount == null)
                {
                    return Content(HttpStatusCode.NotFound, new { Success = false, Message = "Mã giảm giá không tồn tại" });
                }

                // Kiểm tra thời hạn sử dụng
                if (discount.start_at.HasValue && discount.start_at > now)
                {
                    return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Mã giảm giá chưa đến thời gian áp dụng" });
                }

                if (discount.end_at.HasValue && discount.end_at < now)
                {
                    return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Mã giảm giá đã hết hạn" });
                }

                // Kiểm tra lượt dùng
                if (discount.max_uses.HasValue && discount.used_count >= discount.max_uses)
                {
                    return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Mã giảm giá đã hết lượt sử dụng" });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Mã giảm giá hợp lệ",
                    Data = new
                    {
                        Id = discount.id,
                        Code = discount.code,
                        Type = discount.type,
                        Value = discount.value,
                        MinOrderAmount = discount.min_order_amount
                    }
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // POST /api/discounts/apply
        // Áp dụng mã và cập nhật số lượt sử dụng
        // ========================================
        [HttpPost]
        [Route("apply")]
        public IHttpActionResult ApplyDiscount([FromBody] ApplyDiscountRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Code))
                {
                    return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Dữ liệu không hợp lệ" });
                }

                var normalizedCode = request.Code.Trim().ToUpper();
                var discount = _db.discounts
                    .FirstOrDefault(d => d.code.ToUpper() == normalizedCode && d.is_active == true);

                if (discount == null)
                {
                    return Content(HttpStatusCode.NotFound, new { Success = false, Message = "Mã giảm giá không hợp lệ hoặc đã bị tắt" });
                }

                // Kiểm tra giá trị đơn hàng tối thiểu
                if (discount.min_order_amount.HasValue && request.OrderTotal < discount.min_order_amount)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        Success = false,
                        Message = $"Đơn hàng tối thiểu {discount.min_order_amount:N0}đ để áp dụng mã này"
                    });
                }

                // Logic tính toán số tiền giảm
                decimal discountAmount = 0;
                if (discount.type == "percentage")
                {
                    discountAmount = Math.Round(request.OrderTotal * (decimal)discount.value / 100);
                }
                else
                {
                    discountAmount = Math.Min((decimal)discount.value, request.OrderTotal);
                }

                // Cập nhật trạng thái sử dụng vào DB
                discount.used_count++;
                discount.updated_at = DateTime.Now;
                _db.SaveChanges();

                return Ok(new
                {
                    Success = true,
                    Message = $"Áp dụng mã thành công! Bạn được giảm {discountAmount:N0}đ",
                    Data = new
                    {
                        DiscountId = discount.id,
                        DiscountCode = discount.code,
                        DiscountAmount = discountAmount,
                        FinalTotal = request.OrderTotal - discountAmount
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