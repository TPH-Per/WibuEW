using DoAnLTWHQT.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace DoAnLTWHQT.Controllers
{
    [RoutePrefix("api/addresses")]
    public class ApiAddressController : ApiController
    {
        private readonly perwEntities _db = new perwEntities();

        // ================================================
        // GET /api/addresses?userId={userId}
        // Lấy tất cả địa chỉ của user
        // ================================================
        [HttpGet]
        [Route("")]
        [AllowAnonymous]
        public IHttpActionResult GetAddresses(long userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return Content(HttpStatusCode.BadRequest, new ApiResponse
                    {
                        Success = false,
                        Message = "User ID không hợp lệ."
                    });
                }

                var addresses = _db.addresses
                    .Where(a => a.user_id == userId && a.deleted_at == null)
                    .OrderByDescending(a => a.is_default)
                    .ThenByDescending(a => a.created_at)
                    .Select(a => new AddressDto
                    {
                        Id = a.id,
                        UserId = a.user_id,
                        RecipientName = a.recipient_name,
                        RecipientPhone = a.recipient_phone,
                        StreetAddress = a.street_address,
                        Ward = a.ward,
                        District = a.district,
                        City = a.city,
                        IsDefault = a.is_default
                    })
                    .ToList();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Lấy danh sách địa chỉ thành công.",
                    Data = addresses
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Get Addresses Error: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // ================================================
        // GET /api/addresses/{id}
        // Lấy chi tiết địa chỉ
        // ================================================
        [HttpGet]
        [Route("{id:long}")]
        [AllowAnonymous]
        public IHttpActionResult GetAddress(long id)
        {
            try
            {
                var address = _db.addresses
                    .Where(a => a.id == id && a.deleted_at == null)
                    .Select(a => new AddressDto
                    {
                        Id = a.id,
                        UserId = a.user_id,
                        RecipientName = a.recipient_name,
                        RecipientPhone = a.recipient_phone,
                        StreetAddress = a.street_address,
                        Ward = a.ward,
                        District = a.district,
                        City = a.city,
                        IsDefault = a.is_default
                    })
                    .FirstOrDefault();

                if (address == null)
                {
                    return Content(HttpStatusCode.NotFound, new ApiResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy địa chỉ."
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = address
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ================================================
        // POST /api/addresses
        // Tạo địa chỉ mới
        // ================================================
        [HttpPost]
        [Route("")]
        [AllowAnonymous]
        public IHttpActionResult CreateAddress([FromBody] ApiAddressRequest request)
        {
            System.Diagnostics.Debug.WriteLine("========== API CREATE ADDRESS CALLED ==========");

            try
            {
                if (request == null || request.UserId <= 0)
                {
                    return Content(HttpStatusCode.BadRequest, new ApiResponse
                    {
                        Success = false,
                        Message = "Dữ liệu không hợp lệ."
                    });
                }

                // Validation
                var errors = new Dictionary<string, string[]>();

                if (string.IsNullOrWhiteSpace(request.RecipientName))
                    errors["recipient_name"] = new[] { "Tên người nhận không được để trống." };

                if (string.IsNullOrWhiteSpace(request.RecipientPhone))
                    errors["recipient_phone"] = new[] { "Số điện thoại không được để trống." };

                if (string.IsNullOrWhiteSpace(request.StreetAddress))
                    errors["street_address"] = new[] { "Địa chỉ không được để trống." };

                if (string.IsNullOrWhiteSpace(request.City))
                    errors["city"] = new[] { "Tỉnh/Thành phố không được để trống." };

                if (errors.Count > 0)
                {
                    return Content((HttpStatusCode)422, new ApiValidationResponse
                    {
                        Success = false,
                        Message = "Dữ liệu không hợp lệ.",
                        Errors = errors
                    });
                }

                // Nếu đây là địa chỉ mặc định, bỏ mặc định của các địa chỉ khác
                if (request.IsDefault)
                {
                    var existingDefaults = _db.addresses
                        .Where(a => a.user_id == request.UserId && a.is_default && a.deleted_at == null)
                        .ToList();
                    foreach (var addr in existingDefaults)
                    {
                        addr.is_default = false;
                    }
                }

                // Tạo địa chỉ mới
                var newAddress = new address
                {
                    user_id = request.UserId,
                    recipient_name = request.RecipientName.Trim(),
                    recipient_phone = request.RecipientPhone.Trim(),
                    street_address = request.StreetAddress.Trim(),
                    ward = !string.IsNullOrWhiteSpace(request.Ward) ? request.Ward.Trim() : null,
                    district = !string.IsNullOrWhiteSpace(request.District) ? request.District.Trim() : null,
                    city = request.City.Trim(),
                    is_default = request.IsDefault,
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                };

                _db.addresses.Add(newAddress);
                _db.SaveChanges();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Thêm địa chỉ thành công!",
                    Data = new AddressDto
                    {
                        Id = newAddress.id,
                        UserId = newAddress.user_id,
                        RecipientName = newAddress.recipient_name,
                        RecipientPhone = newAddress.recipient_phone,
                        StreetAddress = newAddress.street_address,
                        Ward = newAddress.ward,
                        District = newAddress.district,
                        City = newAddress.city,
                        IsDefault = newAddress.is_default
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Create Address Error: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // ================================================
        // PUT /api/addresses/{id}
        // Cập nhật địa chỉ
        // ================================================
        [HttpPut]
        [Route("{id:long}")]
        [AllowAnonymous]
        public IHttpActionResult UpdateAddress(long id, [FromBody] ApiAddressRequest request)
        {
            System.Diagnostics.Debug.WriteLine("========== API UPDATE ADDRESS CALLED ==========");

            try
            {
                var address = _db.addresses.FirstOrDefault(a => a.id == id && a.deleted_at == null);

                if (address == null)
                {
                    return Content(HttpStatusCode.NotFound, new ApiResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy địa chỉ."
                    });
                }

                // Validation
                var errors = new Dictionary<string, string[]>();

                if (string.IsNullOrWhiteSpace(request.RecipientName))
                    errors["recipient_name"] = new[] { "Tên người nhận không được để trống." };

                if (string.IsNullOrWhiteSpace(request.RecipientPhone))
                    errors["recipient_phone"] = new[] { "Số điện thoại không được để trống." };

                if (string.IsNullOrWhiteSpace(request.StreetAddress))
                    errors["street_address"] = new[] { "Địa chỉ không được để trống." };

                if (string.IsNullOrWhiteSpace(request.City))
                    errors["city"] = new[] { "Tỉnh/Thành phố không được để trống." };

                if (errors.Count > 0)
                {
                    return Content((HttpStatusCode)422, new ApiValidationResponse
                    {
                        Success = false,
                        Message = "Dữ liệu không hợp lệ.",
                        Errors = errors
                    });
                }

                // Nếu đây là địa chỉ mặc định, bỏ mặc định của các địa chỉ khác
                if (request.IsDefault && !address.is_default)
                {
                    var existingDefaults = _db.addresses
                        .Where(a => a.user_id == address.user_id && a.is_default && a.id != id && a.deleted_at == null)
                        .ToList();
                    foreach (var addr in existingDefaults)
                    {
                        addr.is_default = false;
                    }
                }

                // Cập nhật
                address.recipient_name = request.RecipientName.Trim();
                address.recipient_phone = request.RecipientPhone.Trim();
                address.street_address = request.StreetAddress.Trim();
                address.ward = !string.IsNullOrWhiteSpace(request.Ward) ? request.Ward.Trim() : null;
                address.district = !string.IsNullOrWhiteSpace(request.District) ? request.District.Trim() : null;
                address.city = request.City.Trim();
                address.is_default = request.IsDefault;
                address.updated_at = DateTime.Now;

                _db.SaveChanges();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Cập nhật địa chỉ thành công!",
                    Data = new AddressDto
                    {
                        Id = address.id,
                        UserId = address.user_id,
                        RecipientName = address.recipient_name,
                        RecipientPhone = address.recipient_phone,
                        StreetAddress = address.street_address,
                        Ward = address.ward,
                        District = address.district,
                        City = address.city,
                        IsDefault = address.is_default
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Update Address Error: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // ================================================
        // DELETE /api/addresses/{id}
        // Xóa địa chỉ (soft delete)
        // ================================================
        [HttpDelete]
        [Route("{id:long}")]
        [AllowAnonymous]
        public IHttpActionResult DeleteAddress(long id)
        {
            System.Diagnostics.Debug.WriteLine("========== API DELETE ADDRESS CALLED ==========");

            try
            {
                var address = _db.addresses.FirstOrDefault(a => a.id == id && a.deleted_at == null);

                if (address == null)
                {
                    return Content(HttpStatusCode.NotFound, new ApiResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy địa chỉ."
                    });
                }

                // Soft delete
                address.deleted_at = DateTime.Now;
                _db.SaveChanges();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Xóa địa chỉ thành công!"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Delete Address Error: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // ================================================
        // PUT /api/addresses/{id}/set-default
        // Đặt địa chỉ làm mặc định
        // ================================================
        [HttpPut]
        [Route("{id:long}/set-default")]
        [AllowAnonymous]
        public IHttpActionResult SetDefaultAddress(long id)
        {
            System.Diagnostics.Debug.WriteLine("========== API SET DEFAULT ADDRESS CALLED ==========");

            try
            {
                var address = _db.addresses.FirstOrDefault(a => a.id == id && a.deleted_at == null);

                if (address == null)
                {
                    return Content(HttpStatusCode.NotFound, new ApiResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy địa chỉ."
                    });
                }

                // Bỏ mặc định của các địa chỉ khác
                var existingDefaults = _db.addresses
                    .Where(a => a.user_id == address.user_id && a.is_default && a.id != id && a.deleted_at == null)
                    .ToList();
                foreach (var addr in existingDefaults)
                {
                    addr.is_default = false;
                }

                // Đặt địa chỉ này làm mặc định
                address.is_default = true;
                address.updated_at = DateTime.Now;

                _db.SaveChanges();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Đã đặt làm địa chỉ mặc định!"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Set Default Address Error: {ex.Message}");
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
