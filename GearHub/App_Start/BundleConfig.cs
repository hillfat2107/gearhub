using System.Web;
using System.Web.Optimization;

namespace GearHub
{
    public class BundleConfig
    {
        // Bundle giúp gom CSS và JavaScript trước khi gửi các tệp đó tới trình duyệt.
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Modernizr giúp kiểm tra tính năng trình duyệt trong lúc phát triển giao diện.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new Bundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js"));

            // Nạp CSS chung và file Site.css chứa giao diện của GearHub.
            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/Site.css"));
        }
    }
}
