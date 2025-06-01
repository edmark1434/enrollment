using System.Web.Mvc;
using EnrollmentSystem.Controllers.Service;

namespace  EnrollmentSystem.Controllers;

public class FetchController : Controller
{
    private readonly string _connectionString;
    private readonly IFetchService _fetchService;

    public FetchController(IFetchService fetchService)
    {
        _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString;
        _fetchService = fetchService;
    }
    [HttpGet]
    public ActionResult program()
    {
        var program = _fetchService.GetProgramsFromDatabase();
        return Json(new {program = program},JsonRequestBehavior.AllowGet);
    }

    [HttpGet]
    public ActionResult courses()
    {
        var courses = _fetchService.GetCoursesFromDatabase();
        return Json(new {courses = courses},JsonRequestBehavior.AllowGet);
    }

    [HttpGet]
    public ActionResult sections()
    {
        var sections = _fetchService.GetAllBlockSections();
        return Json(new {sections = sections},JsonRequestBehavior.AllowGet);
    }

    [HttpGet]
    public ActionResult enrollments()
    {
        var current = _fetchService.GetCurrentEnrollments();
        return Json(new {enrollments = current},JsonRequestBehavior.AllowGet);
    }

    [HttpGet]
    public ActionResult professor()
    {
        var professor = _fetchService.GetProfessorFromDatabase();
        return Json(new {professor = professor},JsonRequestBehavior.AllowGet);
    }

    [HttpGet]
    public ActionResult rooms()
    {
        var room = _fetchService.GetRoomsFromDatabase();
        return Json(new {room = room},JsonRequestBehavior.AllowGet);
    }
}