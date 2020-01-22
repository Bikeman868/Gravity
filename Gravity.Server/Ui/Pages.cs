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
    [ZoneHtml("main", "app-title", "<h1>Gravity UI</h1>")]
    internal class HomePage { }

    /*
     * Dashboard page
     */

    [IsPage("dashboard")]
    [Route("/ui/dashboard", Method.Get)]
    [PageTitle("Gravity dashboard")]
    [UsesLayout("page")]
    [ZoneComponent("main", "dashboard_diagram")]
    internal class DashboardPage { }

    /*
     * Node page
     */

    [IsPage("node")]
    [Route("/ui/node", Method.Get)]
    [PageTitle("Gravity node")]
    [UsesLayout("page")]
    [ZoneComponent("main", "node_diagram")]
    internal class NodePage { }

    /*
     * Common elements
     */

    [IsLayout("page", "header,main,footer")]
    [ZoneRegion("header", "header")]
    [ZoneRegion("main", "main")]
    [ZoneRegion("footer", "footer")]
    internal class HomePageLayout { }

    [IsRegion("header")]
    internal class HeaderRegion { }

    [IsRegion("main")]
    internal class MainRegion { }

    [IsRegion("footer")]
    internal class FooterRegion { }
}