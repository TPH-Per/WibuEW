using System;
using System.Security.Principal;

namespace DoAnLTWHQT.Security   
{
    public class CustomIdentity : IIdentity
    {
        public CustomIdentity(string name, string role)
        {
            Name = name;
            Role = role;
        }

        public string AuthenticationType => "Forms";
        public bool IsAuthenticated => true;
        public string Name { get; }

        public string Role { get; }
    }

	    public class CustomPrincipal : IPrincipal
	    {
	        private readonly CustomIdentity _identity;
	
	        public CustomPrincipal(CustomIdentity identity)
	        {
	            _identity = identity;
	        }
	
	        public IIdentity Identity => _identity;
	
	        public bool IsInRole(string role)
	        {
	            // Admin có toàn quy?n truy c?p m?i role
	            if (string.Equals(_identity.Role, "admin", StringComparison.OrdinalIgnoreCase))
	            {
	                return true;
	            }
	
	            return string.Equals(_identity.Role, role, StringComparison.OrdinalIgnoreCase);
	        }
	
	        public string Role => _identity.Role;
	    }
}
