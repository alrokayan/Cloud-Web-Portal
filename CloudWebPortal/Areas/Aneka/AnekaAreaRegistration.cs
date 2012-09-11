using System.Web.Mvc;

namespace CloudWebPortal.Areas.Aneka
{
    public class AnekaAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Aneka";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "Aneka_default",
                "Aneka/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
