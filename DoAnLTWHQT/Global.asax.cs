using DoAnLTWHQT.Security;
using System;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace DoAnLTWHQT
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            // Ðang ký global filters (HandleError, v.v.) n?u c?n
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            //WebApiConfig.Register(GlobalConfiguration.Configuration); // n?u có dùng Web API

            EnsureTestAdminAccount();
            EnsureTestWarehouseManagerAccount();
            EnsureTestBranchManagerAccount();
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            var context = HttpContext.Current;
            var origin = context.Request.Headers["Origin"];

            // Cho phép origin từ Vue dev server
            if (origin == "http://localhost:3000")
            {
                context.Response.AddHeader("Access-Control-Allow-Origin", origin);
                context.Response.AddHeader("Access-Control-Allow-Credentials", "true");
                context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            }

            // Xử lý preflight request (OPTIONS)
            if (context.Request.HttpMethod == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                context.Response.End();
            }
        }

        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[PostAuth] Application_PostAuthenticateRequest called");

            HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie == null)
            {
                System.Diagnostics.Debug.WriteLine($"[PostAuth] No auth cookie found. Cookie name: {FormsAuthentication.FormsCookieName}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[PostAuth] Auth cookie found: {FormsAuthentication.FormsCookieName}");

            FormsAuthenticationTicket ticket;
            try
            {
                ticket = FormsAuthentication.Decrypt(authCookie.Value);
                System.Diagnostics.Debug.WriteLine($"[PostAuth] Ticket decrypted successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PostAuth] Failed to decrypt ticket: {ex.Message}");
                return;
            }

            if (ticket == null || ticket.Expired)
            {
                System.Diagnostics.Debug.WriteLine($"[PostAuth] Ticket is null or expired");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[PostAuth] Ticket valid. User: {ticket.Name}, Role: {ticket.UserData}");

            // userData = roleName
            var role = ticket.UserData;
            var identity = new CustomIdentity(ticket.Name, role);
            var principal = new CustomPrincipal(identity);

            HttpContext.Current.User = principal;
            System.Threading.Thread.CurrentPrincipal = principal;

            System.Diagnostics.Debug.WriteLine($"[PostAuth] Principal set for user: {ticket.Name}");
        }

        private static void EnsureTestAdminAccount()
        {
            try
            {
                const string testEmail = "admin@test.local";
                const string testPassword = "Admin@123";

                using (var db = new Entities())
                {
                    var normalizedEmail = testEmail.ToLowerInvariant();
                    var existingUser = db.users.FirstOrDefault(u => u.email.ToLower() == normalizedEmail);

                    if (existingUser != null)
                    {
                        var hasStatusChange = existingUser.deleted_at != null ||
                                              !string.Equals(existingUser.status, "active", StringComparison.OrdinalIgnoreCase);
                        if (hasStatusChange)
                        {
                            existingUser.deleted_at = null;
                            existingUser.status = "active";
                            existingUser.updated_at = DateTime.UtcNow;
                            db.SaveChanges();
                        }

                        return;
                    }

                    var adminRole = db.roles.FirstOrDefault(r => r.name == "admin");
                    if (adminRole == null)
                    {
                        var now = DateTime.UtcNow;
                        adminRole = new role
                        {
                            name = "admin",
                            description = "Default admin (auto-generated for testing)",
                            created_at = now,
                            updated_at = now
                        };

                        db.roles.Add(adminRole);
                        db.SaveChanges();
                    }

                    var timestamp = DateTime.UtcNow;
                    db.users.Add(new user
                    {
                        name = "testadmin",
                        full_name = "Test Admin",
                        email = testEmail,
                        password = Crypto.HashPassword(testPassword),
                        status = "active",
                        role_id = adminRole.id,
                        created_at = timestamp,
                        updated_at = timestamp
                    });

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Startup] Unable to seed admin account: {ex}");
            }
        }

        private static void EnsureTestWarehouseManagerAccount()
        {
            try
            {
                const string testEmail = "warehouse@test.local";
                const string testPassword = "Warehouse@123";

                using (var db = new Entities())
                {
                    var normalizedEmail = testEmail.ToLowerInvariant();
                    var existingUser = db.users.FirstOrDefault(u => u.email.ToLower() == normalizedEmail);

                    if (existingUser != null)
                    {
                        var hasStatusChange = existingUser.deleted_at != null ||
                                              !string.Equals(existingUser.status, "active", StringComparison.OrdinalIgnoreCase);
                        if (hasStatusChange)
                        {
                            existingUser.deleted_at = null;
                            existingUser.status = "active";
                            existingUser.updated_at = DateTime.UtcNow;
                            db.SaveChanges();
                        }

                        return;
                    }

                    var warehouseRole = db.roles.FirstOrDefault(r => r.name == "warehouse_manager");
                    if (warehouseRole == null)
                    {
                        var now = DateTime.UtcNow;
                        warehouseRole = new role
                        {
                            name = "warehouse_manager",
                            description = "Default warehouse manager (auto-generated for testing)",
                            created_at = now,
                            updated_at = now
                        };

                        db.roles.Add(warehouseRole);
                        db.SaveChanges();
                    }

                    var timestamp = DateTime.UtcNow;
                    db.users.Add(new user
                    {
                        name = "testwarehouse",
                        full_name = "Test Warehouse Manager",
                        email = testEmail,
                        password = Crypto.HashPassword(testPassword),
                        status = "active",
                        role_id = warehouseRole.id,
                        created_at = timestamp,
                        updated_at = timestamp
                    });

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Startup] Unable to seed warehouse manager account: {ex}");
            }
        }

        private static void EnsureTestBranchManagerAccount()
        {
            try
            {
                const string testEmail = "branch@test.local";
                const string testPassword = "Branch@123";

                using (var db = new Entities())
                {
                    var normalizedEmail = testEmail.ToLowerInvariant();
                    var existingUser = db.users.FirstOrDefault(u => u.email.ToLower() == normalizedEmail);

                    if (existingUser != null)
                    {
                        var hasStatusChange = existingUser.deleted_at != null ||
                                              !string.Equals(existingUser.status, "active", StringComparison.OrdinalIgnoreCase);
                        if (hasStatusChange)
                        {
                            existingUser.deleted_at = null;
                            existingUser.status = "active";
                            existingUser.updated_at = DateTime.UtcNow;
                            db.SaveChanges();
                        }

                        return;
                    }

                    var branchRole = db.roles.FirstOrDefault(r => r.name == "branch_manager");
                    if (branchRole == null)
                    {
                        var now = DateTime.UtcNow;
                        branchRole = new role
                        {
                            name = "branch_manager",
                            description = "Default branch manager (auto-generated for testing)",
                            created_at = now,
                            updated_at = now
                        };

                        db.roles.Add(branchRole);
                        db.SaveChanges();
                    }

                    var timestamp = DateTime.UtcNow;
                    db.users.Add(new user
                    {
                        name = "testbranch",
                        full_name = "Test Branch Manager",
                        email = testEmail,
                        password = Crypto.HashPassword(testPassword),
                        status = "active",
                        role_id = branchRole.id,
                        created_at = timestamp,
                        updated_at = timestamp
                    });

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Startup] Unable to seed branch manager account: {ex}");
            }
        }
    }
}
