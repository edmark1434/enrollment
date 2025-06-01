using System.Web.Mvc;
using System.Collections.Generic;
using System.Configuration;
using EnrollmentSystem.Controllers.Service;
using EnrollmentSystem.Models;
using Npgsql;
namespace EnrollmentSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly string _connectionString;
        private readonly IFetchService _fetchService;

        public AdminController(IFetchService fetchService)
        {
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString;
            _fetchService = fetchService;
        }

        public ActionResult MainAdmin()
        {
            List<int> statList = getDashboardStat();
            ViewBag.statList = statList;
            return View("~/Views/Admin/Dashboard.cshtml");
        }
        
        public ActionResult Admin_Curriculum()
        {
            var programs = _fetchService.GetProgramsFromDatabase();
            var yearSemesterOptions = _fetchService.GetAcademicYearsFromDatabase();

            // Create a ViewModel or use ViewBag to pass both to the vieww
            ViewBag.AcademicYears = yearSemesterOptions;

            return View("~/Views/Admin/Curriculum.cshtml", programs);
        }
        

        public ActionResult Admin_Course()
        {
            // Redirect to CourseController.Index instead of duplicating logic
            return RedirectToAction("Course", "Course");
        }

        public ActionResult Admin_AddCourse()
        {
            return RedirectToAction("Index", "AddProgram"); // Use AddProgramController
        }

        public ActionResult Admin_Schedule()
        {
            return View("~/Views/Admin/SetSchedules.cshtml");
        }

        
        
        public List<int> getDashboardStat()
        {
            var statList = new List<int>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                statList.Add(getStat(conn, "SELECT COUNT(*) FROM student"));
                statList.Add(getStat(conn, "SELECT COUNT(*) FROM Course"));
            }
            return statList;
        }

        public int getStat(NpgsqlConnection conn, string query)
        {
            int stat = 0;
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        stat = reader.GetInt32(0);
                    }
                }   
            }
            return stat;
        }

        public ActionResult AdminControl()
        {
            ViewBag.AcademicYears = _fetchService.GetAcademicYearsFromDatabase();
            ViewBag.Programs = _fetchService.GetProgramsFromDatabase();
            ViewBag.Semester = _fetchService.GetSemesterFromDatabase();
            ViewBag.CurrentEnrollement = _fetchService.GetCurrentEnrollments();
            ViewBag.Room = _fetchService.GetRoomsFromDatabase();
            ViewBag.Professor = _fetchService.GetProfessorFromDatabase();
            return View();
        }

        public ActionResult EnrollmentApproval()
        {
            return View();
        }

        public ActionResult ScheduleManagement()
        {
            return View();
        }
        [HttpPost]
        public ActionResult updateCurrentEnrollments(CurrentEnrollment current)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                using (var cmd = new NpgsqlCommand(
                           "UPDATE enrollment_season SET ay_code = @ay, sem_id = @sem , is_open = @open where enroll_season_id = 1",
                           conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@ay", current.ay_code);
                    cmd.Parameters.AddWithValue("@sem", current.sem_id);
                    cmd.Parameters.AddWithValue("@open", current.is_open);
                    var result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        return Json(new { success = true, message = "Current Enrollment Updated Successfully" ,redirectUrl = "Admin/AdminControl"},JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = false, message = "Error Updating Current Enrollment" },JsonRequestBehavior.AllowGet);
                }
            }
        }
    }
}