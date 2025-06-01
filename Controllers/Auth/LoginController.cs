using System.Web.Mvc;
using EnrollmentSystem.Controllers.Service;

namespace EnrollmentSystem.Controllers.Auth
{
    public class LoginController : Controller
    {
        private static IFetchService _fetchService;
        public LoginController(IFetchService fetchService)
        {
            _fetchService = fetchService;
        }

    
        // GET: /Login
        public ActionResult Login()
        {
            // View is located at Views/Auth/Login.cshtml
            return View("~/Views/Auth/Login.cshtml");
        }

        public ActionResult LoginTeacher()
        {
            return View("~/Views/Auth/LoginTeacher.cshtml");
        }

        public ActionResult LoginHead()
        {
            return View("~/Views/Auth/LoginProgramHead.cshtml");

        }

        public ActionResult LoginAdmin()
        {
            return View("~/Views/Auth/LoginAdmin.cshtml");
        }
        
        public ActionResult SignUp()
        {
            ViewBag.Program = _fetchService.GetProgramsFromDatabase();
            return View("~/Views/Auth/SignUp.cshtml");
        }
    }
}