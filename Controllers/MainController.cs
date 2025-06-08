using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using EnrollmentSystem.Controllers.Service;
using EnrollmentSystem.Models;
using Npgsql;

namespace EnrollmentSystem.Controllers
{
    public class MainController : Controller
    {
        private string connString = ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString; 
        private readonly IFetchService _fetchService;

        public MainController(IFetchService fetchService)
        {
            _fetchService = fetchService;
        }
        // GET: /Login
        public ActionResult MainHome()
        {
            // View is located at Views/Auth/Login.cshtml
            return View("~/Views/Main/Home.cshtml");
        }
        public ActionResult Student_Profile()
{
    var sessionStudCode = Session["Stud_Code"];

    if (sessionStudCode == null)
    {
        return RedirectToAction("Login", "Account");
    }

    int studCode = Convert.ToInt32(sessionStudCode);
    var student = new Student();

    try
    {
        using (var conn = new NpgsqlConnection(connString))
        {
            conn.Open();
            string query = "SELECT * FROM STUDENT WHERE STUD_CODE = @studCode";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@studCode", studCode);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        student.Stud_Id = reader.GetInt32(reader.GetOrdinal("stud_id"));
                        student.Stud_Lname = reader["stud_lname"]?.ToString();
                        student.Stud_Fname = reader["stud_fname"]?.ToString();
                        student.Stud_Mname = reader["stud_mname"]?.ToString();
                        student.Stud_Dob = Convert.ToDateTime(reader["stud_dob"]);
                        student.Stud_Contact = reader["stud_contact"]?.ToString();
                        student.Stud_Email = reader["stud_email"]?.ToString();
                        student.Stud_Address = reader["stud_address"]?.ToString();
                        student.Stud_Code = Convert.ToInt32(reader["stud_code"]);
                        student.ProgCode = reader["prog_code"]?.ToString();
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Student not found.";
                        return View("~/Views/Shared/Error.cshtml");
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        ViewBag.ErrorMessage = $"Error loading student data: {ex.Message}";
        return View("~/Views/Shared/Error.cshtml");
    }

    ViewBag.Profile = student;
    ViewBag.Enrollment = _fetchService.GetCurrentEnrollments(); // optional

    return View("~/Views/Main/StudentProfile.cshtml");
}

        
        
            public ActionResult Student_Enrollment()
        {
            var sessionStudCode = Session["Stud_Code"];

            if (sessionStudCode == null)
            {
                return RedirectToAction("Login", "Account");
            }
            ViewBag.Programs = TempData["Programs"];
            ViewBag.AcademicYears = TempData["AcademicYears"];
            int studCode = Convert.ToInt32(sessionStudCode);
            var student = new Student();
            var section = new List<BlkSec>();
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string query = "SELECT * FROM STUDENT WHERE STUD_CODE = @studCode";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@studCode", studCode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                student.Stud_Id = reader.GetInt32(reader.GetOrdinal("stud_id"));
                                student.Stud_Lname = reader["stud_lname"]?.ToString();
                                student.Stud_Fname = reader["stud_fname"]?.ToString();
                                student.Stud_Mname = reader["stud_mname"]?.ToString();
                                student.Stud_Dob = Convert.ToDateTime(reader["stud_dob"]);
                                student.Stud_Contact = reader["stud_contact"]?.ToString();
                                student.Stud_Email = reader["stud_email"]?.ToString();
                                student.Stud_Address = reader["stud_address"]?.ToString();
                                student.Stud_Code = Convert.ToInt32(reader["stud_code"]);
                                student.ProgCode = reader["prog_code"].ToString();
                            }
                            else
                            {
                                ViewBag.ErrorMessage = "Student not found.";
                                return View("~/Views/Shared/Error.cshtml");
                            }
                        }
                    }

                    using (var cmd1 = new NpgsqlCommand(@"SELECT * FROM BLOCK_SECTION WHERE PROG_CODE = @prog", conn))
                    {
                        cmd1.Parameters.AddWithValue("@prog",student.ProgCode);
                        var result = cmd1.ExecuteReader();
                        while (result.Read())
                        {
                            section.Add(new BlkSec
                            {
                                BlkSecId = Convert.ToInt32(result["blk_sec_id"]),
                                BlkSecCode = result["blk_sec_name"].ToString(),
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading student data: {ex.Message}";
                return View("~/Views/Shared/Error.cshtml");
            }
        
            ViewBag.Enrollment = _fetchService.GetCurrentEnrollments();
            ViewBag.Sections = section;
            // Use full path to ensure it finds the correct view
            return View("~/Views/Main/StudentEnroll.cshtml", student);
        }
    
        public ActionResult Student_Grade()
        {
            // View is located at Views/Auth/Login.cshtml
            return View("~/Views/Main/ViewGrades.cshtml");
        }
        public ActionResult Student_Schedule()
        {
            // View is located at Views/Auth/Login.cshtml
            var stud_code = Session["Stud_Code"];
            var program = "";
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand(@"SELECT prog_title from program where prog_code = (SELECT prog_code from student where stud_code = @code)", conn);
                cmd.Parameters.AddWithValue("@code", stud_code);
                var result = cmd.ExecuteReader();
                if (result.Read())
                {
                    program = result["prog_title"].ToString();
                }
            }
            ViewBag.Program = program;
            return View("~/Views/Main/ClassSchedule.cshtml");
        }
        [HttpPost]
        public ActionResult SaveEnrollments(List<Enrollment> enrollments)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    foreach (var enr in enrollments)
                    {
                        using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO enrollment (
                        mis_code, 
                        crs_code, 
                        ay_code, 
                        enrol_sem, 
                        stud_code, 
                        enrol_status, 
                        enrol_yr_level, 
                        enrol_date
                    ) VALUES (
                        @mis_code, 
                        @crs_code, 
                        @ay_code, 
                        @enrol_sem, 
                        @stud_code, 
                        @enrol_status, 
                        @enrol_yr_level, 
                        @enrol_date
                    )", conn))
                        {
                            cmd.Parameters.AddWithValue("mis_code", enr.mis_code);
                            cmd.Parameters.AddWithValue("crs_code", enr.crs_code);
                            cmd.Parameters.AddWithValue("ay_code", enr.ay_code);
                            cmd.Parameters.AddWithValue("enrol_sem", enr.enrol_sem);
                            cmd.Parameters.AddWithValue("stud_code", enr.stud_code);
                            cmd.Parameters.AddWithValue("enrol_status", enr.enrol_status);
                            cmd.Parameters.AddWithValue("enrol_yr_level", enr.enrol_yr_level);
                            cmd.Parameters.AddWithValue("enrol_date", DateTime.Today);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult isEnrollment(EnrollmentCheck enrollmentCheck)
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand(@"SELECT 1 FROM ENROLLMENT WHERE AY_CODE = @ay AND ENROL_SEM = @sem AND STUD_CODE = @stud", conn);
                cmd.Parameters.AddWithValue("@ay", enrollmentCheck.ay_code);
                cmd.Parameters.AddWithValue("@sem", enrollmentCheck.sem);
                cmd.Parameters.AddWithValue("@stud", enrollmentCheck.studentId);
                var result = cmd.ExecuteReader();
                if (result.Read())
                {
                    return Json(new { submitted = true },JsonRequestBehavior.AllowGet);
                }
            }
            return Json(new { submitted = false },JsonRequestBehavior.AllowGet);
        }

        
        [HttpGet]
        public JsonResult GetFilteredSchedule(string year)
        {
            var studentId = Session["Stud_Code"];
            var enrollment = _fetchService.GetCurrentEnrollments();
            List<ScheduleViewModels> scheduleList;

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand(@"SELECT 
                        e.mis_code,
                        c.crs_code,
                        c.crs_title,
                        s.start_time,
                        s.end_time,
                        s.day,
                        e.enrol_sem, -- ⬅️ Make sure we include this field
                        c.crs_lec,
                        c.crs_lab,
                        c.crs_units,
                        r.room_code,
                        p.prof_name
                    FROM enrollment AS e
                    INNER JOIN schedule AS s ON e.mis_code = s.mis_code
                    INNER JOIN room AS r ON r.room_id = s.room_id    
                    INNER JOIN professor AS p ON p.prof_id = s.prof_id
                    INNER JOIN course AS c ON s.crs_code = c.crs_code
                    WHERE e.stud_code = @studCode 
                      AND e.enrol_yr_level = @year AND e.enrol_status = 'Approved'", conn);

                cmd.Parameters.AddWithValue("@studCode", studentId);
                cmd.Parameters.AddWithValue("@year", year);

                using (var reader = cmd.ExecuteReader())
                {
                    scheduleList = new List<ScheduleViewModels>();
                    while (reader.Read())
                    {
                        scheduleList.Add(new ScheduleViewModels
                        {
                            MisCode = reader["mis_code"].ToString(),
                            CourseCode = reader["crs_code"].ToString(),
                            Title = reader["crs_title"].ToString(),
                            StartTime = reader["start_time"].ToString(),
                            EndTime = reader["end_time"].ToString(),
                            Day = reader["day"].ToString(),
                            Semester = Convert.ToInt32(reader["enrol_sem"]), // ⬅️ Read semester value
                            Lec = reader["crs_lec"].ToString(),
                            Lab = reader["crs_lab"].ToString(),
                            Units = reader["crs_units"].ToString(),
                            Room = reader["room_code"].ToString(),
                            Instructor = reader["prof_name"].ToString()
                        });
                    }
                }
            }

            var grouped = scheduleList
                .GroupBy(x => x.MisCode)
                .Select(g => new
                {
                    MisCode = g.Key,
                    CourseCode = g.First().CourseCode,
                    Title = g.First().Title,
                    StartTime = string.Join(", ", g.Select(x => x.StartTime)),
                    EndTime = string.Join(", ", g.Select(x => x.EndTime)),
                    Day = string.Join(", ", g.Select(x => x.Day)),
                    Semester = g.First().Semester, // Include semester
                    Lec = g.First().Lec,
                    Lab = g.First().Lab,
                    Units = g.First().Units,
                    Room = g.First().Room,
                    Instructor = g.First().Instructor
                })
                .ToList();

            return Json(grouped, JsonRequestBehavior.AllowGet);
        } 
    }

    public class EnrollmentCheck
    {
        public string ay_code { get; set; }
        public int sem { get; set; }
        public int studentId { get; set; }
    }
    public class ScheduleViewModels
    {
        public string MisCode { get; set; }
        public string CourseCode { get; set; }
        public string Title { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Day { get; set; }
        public string Lec { get; set; }
        public string Lab { get; set; }
        public string Units { get; set; }
        public string Room { get; set; }
        public string Instructor { get; set; }
        public int Semester { get; set; }
    }
}