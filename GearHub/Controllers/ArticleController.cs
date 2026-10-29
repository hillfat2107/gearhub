using System.Web.Mvc;

namespace GearHub.Controllers
{
    public class ArticleController : Controller
    {
        // Mở khu vực tin tức và bài viết công nghệ.
        public ActionResult Index()
        {
            return View();
        }
    }
}