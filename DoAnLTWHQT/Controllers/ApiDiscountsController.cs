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
        private readonly perwEntities _db = new perwEntities();

        // ========================================
        // GET /api/discounts/active
        // Lay danh sach ma giam gia dang co hieu luc cho khach hang
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
                    Message = "Lay danh sach ma giam gia thanh cong",
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
        // Lay toan bo ma giam gia (Danh cho trang quan tri)
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
        // Kiem tra tinh hop le cua mot ma cu the
        // ========================================
        [HttpGet]
        [Route("validate/{code}")]
        public IHttpActionResult ValidateCode(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Vui long nhap ma giam gia" });
                }

                var now = DateTime.Now;
                var normalizedCode = code.Trim().ToUpper();

                var discount = _db.discounts
                    .FirstOrDefault(d => d.code.ToUpper() == normalizedCode && d.is_active == true);

                if (discount == null)
                {
                    return Content(HttpStatusCode.NotFound, new { Success = false, Message = "Ma giam gia khong ton tai" });
                }

                // Kiem tra thoi han su dung
                if (discount.start_at.HasValue && discount.start_at > now)
                {
                    return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Ma giam gia chua den thoi gian ap dung" });
                }

                if (discount.end_at.HasValue && discount.end_at < now)
                {
                    return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Ma giam gia da het han" });
                }

                // Kiem tra luot dung
                if (discount.max_uses.HasValue && discount.used_count >= discount.max_uses)
                {
                    return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Ma giam gia da het luot su dung" });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Ma giam gia hop le",
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
        // Ap dung ma va cap nhat so luot su dung
        // ========================================
        [HttpPost]
        [Route("apply")]
        public IHttpActionResult ApplyDiscount([FromBody] ApplyDiscountRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Code))
                {
                    return Content(HttpStatusCode.BadRequest, new { Success = false, Message = "Du lieu khong hop le" });
                }

                var normalizedCode = request.Code.Trim().ToUpper();
                var discount = _db.discounts
                    .FirstOrDefault(d => d.code.ToUpper() == normalizedCode && d.is_active == true);

                if (discount == null)
                {
                    return Content(HttpStatusCode.NotFound, new { Success = false, Message = "Ma giam gia khong hop le hoac da bi tat" });
                }

                // Kiem tra gia tri don hang toi thieu
                if (discount.min_order_amount.HasValue && request.OrderTotal < discount.min_order_amount)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        Success = false,
                        Message = $"Don hang toi thieu {discount.min_order_amount:N0}d de ap dung ma nay"
                    });
                }

                // Logic tinh toan so tien giam
                decimal discountAmount = 0;
                if (discount.type == "percentage")
                {
                    discountAmount = Math.Round(request.OrderTotal * discount.value / 100);
                }
                else
                {
                    discountAmount = Math.Min(discount.value, request.OrderTotal);
                }

                // Cap nhat trang thai su dung vao DB
                discount.used_count++;
                discount.updated_at = DateTime.Now;
                _db.SaveChanges();

                return Ok(new
                {
                    Success = true,
                    Message = $"Ap dung ma thanh cong! Ban duoc giam {discountAmount:N0}d",
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
