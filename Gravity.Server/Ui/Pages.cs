using OwinFramework.Pages.Core.Attributes;
using OwinFramework.Pages.Core.Enums;

namespace Gravity.Server.Ui
{
    /*
     * Home page
     */

    [IsPage("home")]
    [Route("/ui", Method.Get)]
    [PageTitle("Gravity")]
    [UsesLayout("page")]
    [RegionHtml("main", "app-title", "<h1>Gravity UI</h1>")]
    internal class HomePage { }

    /*
     * Dashboard page
     */

    [IsPage("dashboard")]
    [Route("/ui/dashboard", Method.Get)]
    [PageTitle("Gravity dashboard")]
    [UsesLayout("page")]
    [RegionComponent("main", "dashboard_diagram")]
    internal class DashboardPage { }

    /*
     * Common elements
     */

    [IsLayout("page", "header,main,footer")]
    [LayoutRegion("header", "header")]
    [LayoutRegion("main", "main")]
    [LayoutRegion("footer", "footer")]
    internal class HomePageLayout { }

    [IsRegion("header")]
    internal class HeaderRegion { }

    [IsRegion("main")]
    internal class MainRegion { }

    [IsRegion("footer")]
    internal class FooterRegion { }
}