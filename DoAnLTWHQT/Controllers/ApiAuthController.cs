using DoAnLTWHQT.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Security;
using BCryptNet = BCrypt.Net.BCrypt;

namespace DoAnLTWHQT.Controllers
{
    [RoutePrefix("api/auth")]
    // CORS được xử lý trong Global.asax.cs để tránh duplicate headers
    public class ApiAuthController : ApiController
    {
        private readonly Entities _db = new Entities();

        // ================================================
        // POST /api/auth/login
        // ================================================
        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public IHttpActionResult Login([FromBody] ApiLoginRequest request)
        {
            System.Diagnostics.Debug.WriteLine("========== API LOGIN CALLED ==========");

            try
            {
                // 1. Validation cơ bản
                if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return Content(HttpStatusCode.BadRequest, new ApiResponse
                    {
                        Success = false,
                        Message = "Vui lòng nhập email và mật khẩu."
                    });
                }

                // 2. Normalize email
                var normalizedEmail = request.Email.Trim().ToLowerInvariant();
                System.Diagnostics.Debug.WriteLine($"API Login - Email: {normalizedEmail}");

                // 3. Tìm user trong database
                var dbUser = _db.users
                    .Include(u => u.role)
                    .FirstOrDefault(u => u.email.ToLower() == normalizedEmail && u.deleted_at == null);

                if (dbUser == null)
                {
                    System.Diagnostics.Debug.WriteLine("API Login - User not found");
                    return Content(HttpStatusCode.Unauthorized, new ApiResponse
                    {
                        Success = false,
                        Message = "Email hoặc mật khẩu không chính xác."
                    });
                }

                // 4. Verify password với BCrypt
                if (!VerifyPassword(request.Password, dbUser.password))
                {
                    System.Diagnostics.Debug.WriteLine("API Login - Password mismatch");
                    return Content(HttpStatusCode.Unauthorized, new ApiResponse
                    {
                        Success = false,
                        Message = "Email hoặc mật khẩu không chính xác."
                    });
                }

                // 5. Kiểm tra trạng thái tài khoản
                if (!string.Equals(dbUser.status, "active", StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine("API Login - Account locked");
                    return Content(HttpStatusCode.Forbidden, new ApiResponse
                    {
                        Success = false,
                        Message = "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên."
                    });
                }

                // 6. Tạo FormsAuthentication cookie để xác thực cho các API request tiếp theo
                var userRole = dbUser.role?.name ?? "customer";
                var rememberMe = request.RememberMe;
                
                var ticket = new FormsAuthenticationTicket(
                    1,                                              // version
                    dbUser.email,                                   // user name (email)
                    DateTime.Now,                                   // issue time
                    DateTime.Now.AddDays(rememberMe ? 7 : 1),       // expiration
                    rememberMe,                                     // persistent
                    userRole,                                       // user data (role)
                    FormsAuthentication.FormsCookiePath             // cookie path
                );

                // Encrypt the ticket
                var encryptedTicket = FormsAuthentication.Encrypt(ticket);

                // Create cookie
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                {
                    HttpOnly = true,
                    Secure = FormsAuthentication.RequireSSL,
                    Path = FormsAuthentication.FormsCookiePath,
                    // Cho phép cross-site cookie (SameSite=None yêu cầu Secure=true)
                    // Tuy nhiên trong dev localhost, ta có thể dùng SameSite=Lax
                };

                if (rememberMe)
                {
                    authCookie.Expires = ticket.Expiration;
                }

                // Add cookie to response
                HttpContext.Current.Response.Cookies.Add(authCookie);
                System.Diagnostics.Debug.WriteLine($"API Login - Auth cookie created: {FormsAuthentication.FormsCookieName}");

                // 7. Thành công
                System.Diagnostics.Debug.WriteLine($"API Login - Success: {dbUser.email}");
                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Đăng nhập thành công!",
                    Data = new UserDto
                    {
                        Id = dbUser.id,
                        Name = dbUser.name,
                        FullName = dbUser.full_name,
                        Email = dbUser.email,
                        PhoneNumber = dbUser.phone_number,
                        RoleId = dbUser.role_id,
                        RoleName = userRole
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Login Error: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // ================================================
        // POST /api/auth/register
        // ================================================
        [HttpPost]
        [Route("register")]
        [AllowAnonymous]
        public IHttpActionResult Register([FromBody] ApiRegisterRequest request)
        {
            System.Diagnostics.Debug.WriteLine("========== API REGISTER CALLED ==========");

            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest, new ApiResponse { Success = false, Message = "Dữ liệu không hợp lệ." });
                }

                // Validation logic
                var errors = new Dictionary<string, string[]>();

                if (string.IsNullOrWhiteSpace(request.Name))
                    errors["name"] = new[] { "Tên đăng nhập không được để trống." };

                if (string.IsNullOrWhiteSpace(request.FullName))
                    errors["full_name"] = new[] { "Họ và tên không được để trống." };

                if (string.IsNullOrWhiteSpace(request.Email))
                    errors["email"] = new[] { "Email không được để trống." };

                if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
                    errors["password"] = new[] { "Mật khẩu phải có ít nhất 6 ký tự." };

                if (request.Password != request.PasswordConfirmation)
                    errors["password_confirmation"] = new[] { "Mật khẩu xác nhận không khớp." };

                if (errors.Count > 0)
                {
                    return Content((HttpStatusCode)422, new ApiValidationResponse
                    {
                        Success = false,
                        Message = "Dữ liệu không hợp lệ.",
                        Errors = errors
                    });
                }

                var normalizedEmail = request.Email.Trim().ToLowerInvariant();
                var normalizedUsername = request.Name.Trim().ToLowerInvariant();

                // Kiểm tra tồn tại
                if (_db.users.Any(u => u.email.ToLower() == normalizedEmail && u.deleted_at == null))
                {
                    return Content((HttpStatusCode)422, new ApiValidationResponse
                    {
                        Success = false,
                        Message = "Email đã được sử dụng.",
                        Errors = new Dictionary<string, string[]> { { "email", new[] { "Email này đã được sử dụng." } } }
                    });
                }

                if (_db.users.Any(u => u.name.ToLower() == normalizedUsername && u.deleted_at == null))
                {
                    return Content((HttpStatusCode)422, new ApiValidationResponse
                    {
                        Success = false,
                        Message = "Tên đăng nhập đã được sử dụng.",
                        Errors = new Dictionary<string, string[]> { { "name", new[] { "Tên đăng nhập này đã được sử dụng." } } }
                    });
                }

                // Hash & Save
                var hashedPassword = BCryptNet.HashPassword(request.Password, BCryptNet.GenerateSalt(12));
                var newUser = new user
                {
                    name = normalizedUsername,
                    full_name = request.FullName.Trim(),
                    email = normalizedEmail,
                    phone_number = !string.IsNullOrWhiteSpace(request.PhoneNumber) ? request.PhoneNumber.Trim() : null,
                    password = hashedPassword,
                    role_id = 4,
                    status = "active",
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                };

                _db.users.Add(newUser);
                _db.SaveChanges();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Đăng ký thành công! Vui lòng đăng nhập.",
                    Data = new { Id = newUser.id }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Register Error: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // ================================================
        // GET /api/auth/check
        // ================================================
        [HttpGet]
        [Route("check")]
        [AllowAnonymous]
        public IHttpActionResult CheckAuth()
        {
            return Ok(new ApiResponse { Success = true, Message = "API is working" });
        }

        // ================================================
        // Helper: Verify Password (supports plain text, BCrypt, and ASP.NET Crypto)
        // ================================================
        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(inputPassword))
            {
                return false;
            }

            // 1. Plain text comparison (for testing/legacy accounts)
            if (string.Equals(storedHash, inputPassword))
            {
                return true;
            }

            // 2. BCrypt hash (starts with $2)
            if (storedHash.StartsWith("$2"))
            {
                try
                {
                    return BCryptNet.Verify(inputPassword, storedHash);
                }
                catch
                {
                    return false;
                }
            }

            // 3. ASP.NET Crypto hash
            try
            {
                return System.Web.Helpers.Crypto.VerifyHashedPassword(storedHash, inputPassword);
            }
            catch
            {
                return false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}