using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Shipping.SeeSharpShipUsps
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("Plugin.Shipping.SeeSharpShipUsps.Configure",
                 "Plugins/SeeSharpShipUsps/Configure",
                 new { controller = "SeeSharpShipUsps", action = "Configure" },
                 new[] { "Nop.Plugin.Shipping.SeeSharpShipUsps.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
