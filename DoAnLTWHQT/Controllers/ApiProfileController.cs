using System;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Security;
using System.Net;
using DoAnLTWHQT.Models;

namespace DoAnLTWHQT.Controllers
{
    /// <summary>
    /// API Controller for user profile management
    /// </summary>
    [RoutePrefix("api/profile")]
    public class ApiProfileController : ApiController
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
        // POST /api/profile/login
        // Login and create FormsAuthentication cookie
        // ========================================
        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Login] Attempting login for: {request?.Email}");

                if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return Ok(new
                    {
                        Success = false,
                        Message = "Vui long nhap email va mat khau"
                    });
                }

                var email = request.Email.Trim().ToLowerInvariant();
                var user = _db.users.FirstOrDefault(u =>
                    u.email.ToLower() == email &&
                    u.deleted_at == null &&
                    u.status == "active");

                if (user == null)
                {
                    return Ok(new
                    {
                        Success = false,
                        Message = "Email hoac mat khau khong dung"
                    });
                }

                // Verify password
                bool passwordValid = false;
                try
                {
                    passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.password);
                }
                catch
                {
                    // Try simple comparison for testing
                    passwordValid = (user.password == request.Password);
                }

                if (!passwordValid)
                {
                    return Ok(new
                    {
                        Success = false,
                        Message = "Email hoac mat khau khong dung"
                    });
                }

                // Create FormsAuthenticationTicket with role data
                var roleName = user.role?.name ?? "customer";
                var ticket = new FormsAuthenticationTicket(
                    1,                              // version
                    user.email,                     // username
                    DateTime.Now,                   // issue time
                    DateTime.Now.AddDays(30),       // expiration
                    true,                           // persistent
                    roleName,                       // user data (role)
                    FormsAuthentication.FormsCookiePath
                );

                // Encrypt the ticket
                var encryptedTicket = FormsAuthentication.Encrypt(ticket);

                // Create the cookie with proper settings for cross-origin
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                {
                    HttpOnly = true,
                    Path = "/",
                    Expires = ticket.Expiration,
                    // HTTPS required for SameSite=None
                    Secure = true,
                    SameSite = SameSiteMode.None
                };

                HttpContext.Current.Response.Cookies.Add(authCookie);
                System.Diagnostics.Debug.WriteLine($"[Login] Created auth cookie for: {user.email}");

                return Ok(new
                {
                    Success = true,
                    Message = "Dang nhap thanh cong",
                    Data = new
                    {
                        Id = user.id,
                        Name = user.name,
                        FullName = user.full_name,
                        Email = user.email,
                        PhoneNumber = user.phone_number,
                        RoleId = user.role_id,
                        RoleName = roleName
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Login] Error: {ex.Message}");
                return Ok(new
                {
                    Success = false,
                    Message = "Loi server: " + ex.Message
                });
            }
        }

        // ========================================
        // GET /api/profile/check-auth
        // Check authentication status
        // ========================================
        [HttpGet]
        [Route("check-auth")]
        public IHttpActionResult CheckAuth()
        {
            var identity = HttpContext.Current?.User?.Identity;
            var isAuth = identity?.IsAuthenticated ?? false;
            var userName = identity?.Name ?? "null";

            return Ok(new
            {
                Success = true,
                IsAuthenticated = isAuth,
                UserName = userName,
                HasCookie = HttpContext.Current?.Request?.Cookies[FormsAuthentication.FormsCookieName] != null
            });
        }

        // ========================================
        // GET /api/auth/profile
        // Get current user's profile
        // ========================================
        [HttpGet]
        [Route("profile")]
        public IHttpActionResult GetProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    // Return empty data instead of 401 to avoid CORS issues
                    return Ok(new
                    {
                        Success = false,
                        Message = "Chua dang nhap",
                        Data = (object)null,
                        IsAuthenticated = false
                    });
                }

                var user = _db.users
                    .Where(u => u.id == userId && u.deleted_at == null)
                    .Select(u => new
                    {
                        Id = u.id,
                        Name = u.name,
                        FullName = u.full_name,
                        Email = u.email,
                        PhoneNumber = u.phone_number,
                        Status = u.status,
                        RoleId = u.role_id,
                        RoleName = u.role != null ? u.role.name : "customer",
                        CreatedAt = u.created_at,
                        UpdatedAt = u.updated_at
                    })
                    .FirstOrDefault();

                if (user == null)
                {
                    return Ok(new
                    {
                        Success = false,
                        Message = "Khong tim thay nguoi dung",
                        Data = (object)null,
                        IsAuthenticated = false
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Lay thong tin thanh cong",
                    Data = user,
                    IsAuthenticated = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetProfile] Error: {ex.Message}");
                return Ok(new
                {
                    Success = false,
                    Message = "Loi server: " + ex.Message,
                    Data = (object)null,
                    IsAuthenticated = false
                });
            }
        }

        // ========================================
        // PUT /api/auth/profile
        // Update current user's profile
        // ========================================
        [HttpPut]
        [Route("profile")]
        public IHttpActionResult UpdateProfile([FromBody] ApiUpdateProfileRequest request)
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

            if (request == null)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    Success = false,
                    Message = "Du lieu khong hop le"
                });
            }

            try
            {
                var user = _db.users.FirstOrDefault(u => u.id == userId && u.deleted_at == null);
                if (user == null)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        Success = false,
                        Message = "Khong tim thay nguoi dung"
                    });
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(request.Name))
                    user.name = request.Name;

                if (!string.IsNullOrEmpty(request.FullName))
                    user.full_name = request.FullName;

                if (!string.IsNullOrEmpty(request.PhoneNumber))
                    user.phone_number = request.PhoneNumber;

                // Check email uniqueness if changing
                if (!string.IsNullOrEmpty(request.Email) && request.Email != user.email)
                {
                    var existingUser = _db.users.FirstOrDefault(u =>
                        u.email == request.Email && u.id != userId && u.deleted_at == null);

                    if (existingUser != null)
                    {
                        return Content(HttpStatusCode.BadRequest, new
                        {
                            Success = false,
                            Message = "Email da duoc su dung"
                        });
                    }
                    user.email = request.Email;
                }

                user.updated_at = DateTime.Now;
                _db.SaveChanges();

                return Ok(new
                {
                    Success = true,
                    Message = "Cap nhat thanh cong",
                    Data = new
                    {
                        Id = user.id,
                        Name = user.name,
                        FullName = user.full_name,
                        Email = user.email,
                        PhoneNumber = user.phone_number,
                        Status = user.status,
                        RoleId = user.role_id,
                        RoleName = user.role?.name ?? "customer",
                        UpdatedAt = user.updated_at
                    }
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // POST /api/auth/change-password
        // Change user's password
        // ========================================
        [HttpPost]
        [Route("change-password")]
        public IHttpActionResult ChangePassword([FromBody] ApiChangePasswordRequest request)
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

            if (request == null || string.IsNullOrEmpty(request.CurrentPassword) || 
                string.IsNullOrEmpty(request.NewPassword) || string.IsNullOrEmpty(request.ConfirmPassword))
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    Success = false,
                    Message = "Vui long nhap day du thong tin"
                });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    Success = false,
                    Message = "Mat khau xac nhan khong khop"
                });
            }

            if (request.NewPassword.Length < 6)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    Success = false,
                    Message = "Mat khau moi phai co it nhat 6 ky tu"
                });
            }

            try
            {
                var user = _db.users.FirstOrDefault(u => u.id == userId && u.deleted_at == null);
                if (user == null)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        Success = false,
                        Message = "Khong tim thay nguoi dung"
                    });
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.password))
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        Success = false,
                        Message = "Mat khau hien tai khong dung"
                    });
                }

                // Hash and update new password
                user.password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.updated_at = DateTime.Now;
                _db.SaveChanges();

                return Ok(new
                {
                    Success = true,
                    Message = "Doi mat khau thanh cong"
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // POST /api/auth/verify-password
        // Verify current password
        // ========================================
        [HttpPost]
        [Route("verify-password")]
        public IHttpActionResult VerifyPassword([FromBody] VerifyPasswordRequest request)
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

            if (request == null || string.IsNullOrEmpty(request.Password))
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    Success = false,
                    Message = "Vui long nhap mat khau"
                });
            }

            try
            {
                var user = _db.users.FirstOrDefault(u => u.id == userId && u.deleted_at == null);
                if (user == null)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        Success = false,
                        Message = "Khong tim thay nguoi dung"
                    });
                }

                var isValid = BCrypt.Net.BCrypt.Verify(request.Password, user.password);

                return Ok(new
                {
                    Success = true,
                    Message = isValid ? "Mat khau chinh xac" : "Mat khau khong dung",
                    Data = isValid
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // POST /api/auth/logout
        // Logout user
        // ========================================
        [HttpPost]
        [Route("logout")]
        public IHttpActionResult Logout()
        {
            try
            {
                // Clear Forms Authentication cookie
                FormsAuthentication.SignOut();

                // Clear session if exists
                if (HttpContext.Current?.Session != null)
                {
                    HttpContext.Current.Session.Abandon();
                }

                // Expire the auth cookie
                if (HttpContext.Current?.Response != null)
                {
                    var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, "")
                    {
                        Expires = DateTime.Now.AddDays(-1),
                        HttpOnly = true,
                        Path = FormsAuthentication.FormsCookiePath
                    };
                    HttpContext.Current.Response.Cookies.Add(authCookie);
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Dang xuat thanh cong"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Logout] Error: {ex.Message}");
                // Still return success even if there's an error
                return Ok(new
                {
                    Success = true,
                    Message = "Dang xuat thanh cong"
                });
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
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class VerifyPasswordRequest
    {
        public string Password { get; set; }
    }
}
