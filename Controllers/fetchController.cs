using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EnrollmentSystem.Controllers.Service;
using EnrollmentSystem.Models;
using Npgsql;

namespace  EnrollmentSystem.Controllers;

public class fetchController : Controller
{
    private readonly string _connectionString;
    private readonly IFetchService _fetchService;

    public fetchController(IFetchService fetchService)
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

    [HttpGet]
    public ActionResult sectionProgram(string program,string yr_level)
    {
        var section = _fetchService.GetSectionByProgram(program,yr_level);
        return Json(new {section = section},JsonRequestBehavior.AllowGet);
    }
    [HttpGet]
    public ActionResult BlockSections(string program, string crs_code)
    {
        var sections = _fetchService.GetAllBlockSectionsByProgram(program, crs_code);
        return Json(new { sections }, JsonRequestBehavior.AllowGet);
    }

    [HttpGet]
    public ActionResult CoursesByProg(string program)
    {
        var courses = _fetchService.GetAllCourseByProgram(program);
        return Json(new { courses }, JsonRequestBehavior.AllowGet);
    }

    [HttpGet]
    public ActionResult Professors(string program)
    {
        var professors = _fetchService.GetAllProfessorsByProgram(program);
        return Json(new { professors }, JsonRequestBehavior.AllowGet);
    }

    [HttpGet]
    public ActionResult RoomsByProg(string program)
    {
        var rooms = _fetchService.GetAllRooms(program);
        return Json(new { rooms }, JsonRequestBehavior.AllowGet);
    }
    [HttpGet]
    public JsonResult GetSchedulesBySection(int sectionId)
    {
        var schedules = _fetchService.GetSchedulesBySection(sectionId);
        return Json(new { schedules }, JsonRequestBehavior.AllowGet);
    }
    [HttpGet]
    public JsonResult GetAllSecondSemesterSchedules()
    {
        int studCode = int.Parse(Session["Stud_Code"].ToString());
        var schedules = _fetchService.GetAllSecondSemesterSchedules(studCode);
        return Json(new { schedules }, JsonRequestBehavior.AllowGet);
    }
    [HttpGet]
public JsonResult GetAllEnrollments()
{
    var result = new List<EnrollmentViewModel>();
    var currentSeason = GetCurrentEnrollmentSeason();

    if (currentSeason == null || string.IsNullOrEmpty(currentSeason.AyCode))
        return Json(result, JsonRequestBehavior.AllowGet);

    using (var conn = new NpgsqlConnection(_connectionString))
    using (var cmd = new NpgsqlCommand(@"
        SELECT 
            e.mis_code,
            e.crs_code,
            c.crs_title AS course_name,
            e.enrol_status,
            e.enrol_yr_level,
            e.ay_code,
            e.enrol_sem,
            s.stud_code,
            s.prog_code,
            s.stud_fname || 
          CASE 
            WHEN s.stud_mname IS NOT NULL AND s.stud_mname <> '' THEN ' ' || s.stud_mname 
            ELSE '' 
          END || 
          ' ' || s.stud_lname AS stud_name,  -- assuming column name is full_name
            e.enrol_date
        FROM enrollment e
        LEFT JOIN course c ON e.crs_code = c.crs_code
        LEFT JOIN student s ON e.stud_code = s.stud_code
        WHERE e.ay_code = @ayCode AND e.enrol_sem = @semId", conn))
    {
        cmd.Parameters.AddWithValue("@ayCode", currentSeason.AyCode);
        cmd.Parameters.AddWithValue("@semId", currentSeason.SemId);

        conn.Open();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                result.Add(new EnrollmentViewModel
                {
                    MisCode = reader["mis_code"]?.ToString(),
                    CrsCode = reader["crs_code"]?.ToString(),
                    CourseName = reader["course_name"]?.ToString(),
                    EnrollStatus = reader["enrol_status"]?.ToString(),
                    EnrollYrLevel = reader["enrol_yr_level"]?.ToString(),
                    AyCode = reader["ay_code"]?.ToString(),
                    SemId = reader.IsDBNull(reader.GetOrdinal("enrol_sem")) ? 0 : reader.GetInt32(reader.GetOrdinal("enrol_sem")),
                    StudCode = reader.IsDBNull(reader.GetOrdinal("stud_code")) ? 0 : reader.GetInt32(reader.GetOrdinal("stud_code")),
                    StudName = reader["stud_name"]?.ToString(),
                    EnrollDate = reader["enrol_date"].ToString(),
                    ProgCode = reader["prog_code"]?.ToString(),
                });
            }
        }
    }

    var grouped = result
        .GroupBy(e => e.StudCode)
        .Select(g => g.First())
        .ToList();

    return Json(grouped, JsonRequestBehavior.AllowGet);
}

    [HttpGet]
    public JsonResult GetAllSchedulesByStudent(int studCode)
    {
        var result = new List<dynamic>();
        var currentSeason = GetCurrentEnrollmentSeason();
        using (var conn = new NpgsqlConnection(_connectionString))
        using (var cmd = new NpgsqlCommand(@"
        SELECT 
            s.mis_code,
            s.crs_code,
            c.crs_title AS course_name,
            r.room_code,
            p.prof_name,
            e.enrol_evaluation, 
            s.day,
            TO_CHAR(s.start_time, 'HH24:MI') AS start_time,
            TO_CHAR(s.end_time, 'HH24:MI') AS end_time,
            b.blk_sec_name AS section_code
        FROM schedule s
        JOIN course c ON s.crs_code = c.crs_code
        JOIN room r ON s.room_id = r.room_id
        JOIN professor p ON s.prof_id = p.prof_id
        JOIN block_section b ON s.blk_sec_id = b.blk_sec_id
        JOIN enrollment e ON s.mis_code = e.mis_code AND stud_code = @studCode AND ay_code = @ay AND enrol_sem = @sem
        WHERE s.mis_code IN (
            SELECT mis_code FROM enrollment WHERE stud_code = @studCode AND ay_code = @ay AND enrol_sem = @sem
        )", conn))
        {
            cmd.Parameters.AddWithValue("@studCode", studCode);
            cmd.Parameters.AddWithValue("@ay", currentSeason.AyCode);
            cmd.Parameters.AddWithValue("@sem", currentSeason.SemId);
            conn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(new
                    {
                        misCode = reader["mis_code"]?.ToString(),
                        crs_code = reader["crs_code"]?.ToString(),
                        course_name = reader["course_name"]?.ToString(),
                        room_code = reader["room_code"]?.ToString(),
                        prof_name = reader["prof_name"]?.ToString(),
                        day = reader["day"]?.ToString(),
                        start_time = reader["start_time"]?.ToString(),
                        end_time = reader["end_time"]?.ToString(),
                        section_code = reader["section_code"]?.ToString(),
                        evaluation = reader["enrol_evaluation"]?.ToString(),
                    });
                }
            }
        }

        return Json(result, JsonRequestBehavior.AllowGet);
    }

        [HttpPost]
        public JsonResult UpdateEnrollmentStatus(int StudCode, string Status)
        {
            var currentSeason = GetCurrentEnrollmentSeason();

            if (currentSeason == null || string.IsNullOrEmpty(currentSeason.AyCode))
                return Json(new { success = false, message = "Invalid academic year or semester." });

            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                using (var cmd = new NpgsqlCommand(@"
                    UPDATE enrollment SET enrol_status = @status
                    WHERE stud_code = @studCode AND ay_code = @ayCode AND enrol_sem = @semId AND enrol_status = 'Pending'", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@status", Status);
                    cmd.Parameters.AddWithValue("@studCode", StudCode);
                    cmd.Parameters.AddWithValue("@ayCode", currentSeason.AyCode);
                    cmd.Parameters.AddWithValue("@semId", currentSeason.SemId);
                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private EnrollmentSeasonViewModel GetCurrentEnrollmentSeason()
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand(@"
                SELECT ay_code, sem_id FROM enrollment_season
                WHERE is_open = TRUE LIMIT 1", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new EnrollmentSeasonViewModel
                        {
                            AyCode = reader["ay_code"]?.ToString(),
                            SemId = Convert.ToInt32(reader["sem_id"])
                        };
                    }
                }
            }
            return null;
        }
        [HttpPost]
        public JsonResult UpdateEvaluationStatus(string StudCode, string MisCode, string Evaluation)
        {
            var studCode = int.Parse(StudCode);
            var enrollment = GetCurrentEnrollmentSeason();
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new NpgsqlCommand(
                        @"UPDATE enrollment SET enrol_evaluation = @eval 
                  WHERE stud_code = @stud AND mis_code = @mis AND ay_code = @ay AND enrol_sem = @sem",
                        conn);
                    cmd.Parameters.AddWithValue("@eval", Evaluation);
                    cmd.Parameters.AddWithValue("@stud", studCode);
                    cmd.Parameters.AddWithValue("@mis", MisCode);
                    cmd.Parameters.AddWithValue("@ay", enrollment.AyCode);
                    cmd.Parameters.AddWithValue("@sem", enrollment.SemId);
                    cmd.ExecuteNonQuery();
                }

                if (Evaluation == "Failed")
                {
                    var conn1 = new NpgsqlConnection(_connectionString);
                    var cmd1 = new NpgsqlCommand(
                        @"UPDATE STUDENT SET STUD_STATUS = 'Irregular' WHERE STUD_CODE = @stud", conn1);
                        cmd1.Parameters.AddWithValue("@stud", studCode);
                        cmd1.ExecuteNonQuery();
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

    public class EnrollmentSeasonViewModel
    {
        public string AyCode { get; set; }
        public int SemId { get; set; }
    }
    
}