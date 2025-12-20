using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Branch
{
    public class BranchAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Branch";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "Branch_default",
                "Branch/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
