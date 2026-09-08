using Microsoft.AspNetCore.Mvc;

namespace Corvano.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => Problem(statusCode: 500, title: "Beklenmeyen bir hata oluştu.");
}
