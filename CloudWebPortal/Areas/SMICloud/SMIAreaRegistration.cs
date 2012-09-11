using System.Web.Mvc;

namespace CloudWebPortal.Areas.SMI
{
    public class SMIAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "SMI";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "SMI_default",
                "SMI/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
