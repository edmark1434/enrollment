using System;
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
            ViewBag.Enrollment = _fetchService.GetCurrentEnrollments();
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
            ViewBag.Program = _fetchService.GetProgramsFromDatabase();
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
                        return Json(new { success = true, message = "Current Enrollment Updated Successfully" ,redirectUrl = "AdminControl"},JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = false, message = "Error Updating Current Enrollment" },JsonRequestBehavior.AllowGet);
                }
            }
        }
        [HttpPost]
    public JsonResult AddAcademicYear(int startYear, int endYear)
    {
        try
        {
            string ayCode = $"AY{startYear}-{endYear}";

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    INSERT INTO academic_year (ay_code, ay_start_year, ay_end_year)
                    VALUES (:AyCode, :StartYear, :EndYear)";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("AyCode", ayCode);
                    cmd.Parameters.AddWithValue("StartYear", startYear);
                    cmd.Parameters.AddWithValue("EndYear", endYear);

                    conn.Open();
                    cmd.ExecuteNonQuery();
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
    public JsonResult DeleteAcademicYear(string id)
    {
        return DeleteRecord("academic_year", "ay_code", id);
    }

    [HttpPost]
    public JsonResult AddProgram(string ProgCode, string ProgTitle)
    {
        try
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    INSERT INTO program (prog_code, prog_title)
                    VALUES (:ProgCode, :ProgTitle)";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("ProgCode", ProgCode);
                    cmd.Parameters.AddWithValue("ProgTitle", ProgTitle);

                    conn.Open();
                    cmd.ExecuteNonQuery();
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
    public JsonResult UpdateProgram(string ProgCode, string ProgTitle)
    {
        try
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    UPDATE program
                    SET prog_title = :ProgTitle
                    WHERE prog_code = :ProgCode";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("ProgCode", ProgCode);
                    cmd.Parameters.AddWithValue("ProgTitle", ProgTitle);

                    conn.Open();
                    cmd.ExecuteNonQuery();
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
    public JsonResult DeleteProgram(string id)
    {
        return DeleteRecord("program", "prog_code", id);
    }

    [HttpPost]
    public JsonResult AddRoom(string RoomCode, string ProgCode)
    {
        try
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    INSERT INTO room (room_code, prog_code)
                    VALUES (:RoomCode, :ProgCode)";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("RoomCode", RoomCode);
                    cmd.Parameters.AddWithValue("ProgCode", ProgCode);

                    conn.Open();
                    cmd.ExecuteNonQuery();
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
    public JsonResult UpdateRoom(int RoomId, string RoomCode, string ProgCode)
    {
        try
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    UPDATE room
                    SET room_code = :RoomCode,
                        prog_code = :ProgCode
                    WHERE room_id = :RoomId";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("RoomId", RoomId);
                    cmd.Parameters.AddWithValue("RoomCode", RoomCode);
                    cmd.Parameters.AddWithValue("ProgCode", ProgCode);

                    conn.Open();
                    cmd.ExecuteNonQuery();
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
    public JsonResult DeleteRoom(int id)
    {
        return DeleteRecord("room", "room_id", id.ToString());
    }
    
    [HttpPost]
    public JsonResult AddProfessor(string ProfName, string ProgCode)
    {
        try
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    INSERT INTO professor (prof_name, prog_code)
                    VALUES (:ProfName, :ProgCode)";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("ProfName", ProfName);
                    cmd.Parameters.AddWithValue("ProgCode", ProgCode);

                    conn.Open();
                    cmd.ExecuteNonQuery();
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
    public JsonResult UpdateProfessor(int ProfId, string ProfName, string ProgCode)
    {
        try
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    UPDATE professor
                    SET prof_name = :ProfName,
                        prog_code = :ProgCode
                    WHERE prof_id = :ProfId";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("ProfId", ProfId);
                    cmd.Parameters.AddWithValue("ProfName", ProfName);
                    cmd.Parameters.AddWithValue("ProgCode", ProgCode);

                    conn.Open();
                    cmd.ExecuteNonQuery();
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
    public JsonResult DeleteProfessor(int id)
    {
        return DeleteRecord("professor", "prof_id", id.ToString());
    }


    [HttpPost]
    public JsonResult Delete(string type, string id)
    {
        try
        {
            string table = "";
            string keyColumn = "";

            switch (type)
            {
                case "AcademicYear":
                    table = "academic_year";
                    keyColumn = "ay_code";
                    break;
                case "Program":
                    table = "program";
                    keyColumn = "prog_code";
                    break;
                case "Room":
                    table = "room";
                    keyColumn = "room_id";
                    break;
                case "Professor":
                    table = "professor";
                    keyColumn = "prof_id";
                    break;
                default:
                    return Json(new { success = false, message = "Invalid delete type" });
            }

            string query = $"DELETE FROM {table} WHERE {keyColumn} = :Id";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    if (keyColumn == "room_id" || keyColumn == "prof_id")
                        cmd.Parameters.AddWithValue("Id", int.Parse(id));
                    else
                        cmd.Parameters.AddWithValue("Id", id);

                    conn.Open();
                    int rows = cmd.ExecuteNonQuery();

                    if (rows == 0)
                        return Json(new { success = false, message = "No record found." });

                    return Json(new { success = true });
                }
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    

    public JsonResult DeleteRecord(string table, string keyColumn, string id)
    {
        try
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                string query = $"DELETE FROM {table} WHERE {keyColumn} = :Id";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    object parsedId = id;
                    if (keyColumn == "room_id" || keyColumn == "prof_id")
                        parsedId = int.Parse(id);

                    cmd.Parameters.AddWithValue("Id", parsedId);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                        return Json(new { success = false, message = "No record found." });
                }
            }

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    }
}