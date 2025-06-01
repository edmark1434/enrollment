using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using EnrollmentSystem.Models;
using Npgsql;

namespace EnrollmentSystem.Controllers
{
    public class CurriculumCourseController : Controller
    {
        [HttpPost]
        public JsonResult AssignCourses(List<CurriculumCourse> courses)
        {
            if (courses == null || courses.Count == 0)
                return Json(new { success = false, message = "No course data provided." });

            var first = courses[0];
            var success = false;
            var message = "";

            try
            {
                using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Enrollment"]
                           .ConnectionString))
                {
                    conn.Open();

                    // Get existing course codes for this curriculum
                    var existingCourses = new HashSet<string>();
                    using (var cmd = new NpgsqlCommand(@"
                SELECT crs_code FROM curriculum_course
                WHERE prog_code = @prog_code AND cur_year_level = @year AND cur_semester = @semester AND ay_code = @ay",
                               conn))
                    {
                        cmd.Parameters.AddWithValue("@prog_code", first.ProgCode);
                        cmd.Parameters.AddWithValue("@year", first.CurYearLevel);
                        cmd.Parameters.AddWithValue("@semester", first.CurSemester);
                        cmd.Parameters.AddWithValue("@ay", first.AyCode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                existingCourses.Add(reader.GetString(0));
                            }
                        }
                    }

                    // Courses to insert
                    foreach (var course in courses)
                    {
                        if (!existingCourses.Contains(course.CrsCode))
                        {
                            using (var cmd = new NpgsqlCommand(@"
                            INSERT INTO curriculum_course (cur_code, crs_code, cur_year_level, cur_semester, ay_code, prog_code)
                             VALUES (@cur_code, @crs_code, @year, @semester, @ay, @prog_code)", conn))
                            {
                                cmd.Parameters.AddWithValue("@cur_code", course.CurCode);
                                cmd.Parameters.AddWithValue("@crs_code", course.CrsCode);
                                cmd.Parameters.AddWithValue("@year", course.CurYearLevel);
                                cmd.Parameters.AddWithValue("@semester", course.CurSemester);
                                cmd.Parameters.AddWithValue("@ay", course.AyCode);
                                cmd.Parameters.AddWithValue("@prog_code", course.ProgCode);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            return Json(new { success = false, message = "Course "+course.CrsCode+" already assigned in "+first.CurYearLevel+" "+first.CurSemester });
                        }
                    }

                    success = true;
                    message = "Courses updated successfully.";
                }
            }
            catch (Exception ex)
            {
                message = "An error occurred: " + ex.Message;
            }

            return Json(new { success, message });
        }


        [HttpGet]
        public JsonResult GetAssignedCourses(string progCode, string yearLevel, string semester, string ayCode)
        {
            var courses = new List<object>();
            using (var conn = new NpgsqlConnection(
                       ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString))
            {
                conn.Open();
                string sql = @"
            SELECT c.crs_code, cr.crs_title
            FROM curriculum_course c
            INNER JOIN course cr ON c.crs_code = cr.crs_code
            WHERE c.prog_code = @progCode AND c.cur_year_level = @yearLevel AND c.cur_semester = @semester AND c.ay_code = @ayCode";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@progCode", progCode);
                    cmd.Parameters.AddWithValue("@yearLevel", yearLevel);
                    cmd.Parameters.AddWithValue("@semester", semester);
                    cmd.Parameters.AddWithValue("@ayCode", ayCode);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            courses.Add(new
                            {
                                code = reader.GetString(0),
                                title = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            return Json(courses, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetAllAcademicYears()
        {
            var academicYears = new List<object>();
            try
            {
                using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString))
                {
                    conn.Open();
                    string sql = "SELECT ay_code, ay_start_year, ay_end_year FROM academic_year ORDER BY ay_start_year ";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                academicYears.Add(new
                                {
                                    AyCode = reader.GetString(0),
                                    DisplayText = $"{reader.GetInt32(1)}-{reader.GetInt32(2)}"
                                });
                            }
                        }
                    }
                }
                return Json(academicYears, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult GetAllPrograms()
        {
            var programs = new List<object>();
            try
            {
                using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString))
                {
                    conn.Open();
                    string sql = "SELECT prog_code, prog_title FROM program ORDER BY prog_title";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                programs.Add(new
                                {
                                    ProgCode = reader.GetString(0),
                                    ProgTitle = reader.GetString(1)
                                });
                            }
                        }
                    }
                }
                return Json(programs, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }
        
        [HttpGet]
public JsonResult GetFilteredCurriculum(string progCode, string semester, string ayCode)
{
    var result = new List<dynamic>();

    try
    {
        using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString))
        {
            conn.Open();
            string sql = @"
                SELECT 
                    c.prog_code, c.crs_code, c.cur_year_level, c.cur_semester, 
                    a.ay_start_year, a.ay_end_year, a.ay_code
                FROM curriculum_course c
                JOIN academic_year a ON c.ay_code = a.ay_code
                WHERE 1=1";

            if (!string.IsNullOrEmpty(progCode))
                sql += " AND c.prog_code = @progCode";
            if (!string.IsNullOrEmpty(semester))
                sql += " AND c.cur_semester = @semester";
            if (!string.IsNullOrEmpty(ayCode))
                sql += " AND c.ay_code = @ayCode";

            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                if (!string.IsNullOrEmpty(progCode))
                    cmd.Parameters.AddWithValue("@progCode", progCode);
                if (!string.IsNullOrEmpty(semester))
                    cmd.Parameters.AddWithValue("@semester", semester);
                if (!string.IsNullOrEmpty(ayCode))
                    cmd.Parameters.AddWithValue("@ayCode", ayCode);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new
                        {
                            progCode = reader.GetString(0),
                            crsCode = reader.GetString(1),
                            curYearLevel = reader.GetString(2),
                            curSemester = reader.GetString(3),
                            ayStartYear = reader.GetInt32(4),
                            ayEndYear = reader.GetInt32(5),
                            ayCode = reader.GetString(6),
                            ayDisplay = $"{reader.GetInt32(4)}-{reader.GetInt32(5)}"
                        });
                    }
                }
            }
        }

        return Json(result, JsonRequestBehavior.AllowGet);
    }
    catch (Exception ex)
    {
        // Log the error (optional): Log.Error(ex);
        return Json(new List<dynamic>(), JsonRequestBehavior.AllowGet);
    }
}
        [HttpGet]
        public JsonResult GetAllCurriculum()
        {
            var result = new List<dynamic>();
            try
            {
                using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString))
                {
                    conn.Open();
                    string sql = @"
                SELECT 
                    c.prog_code,
                    c.cur_year_level,
                    c.cur_semester,
                    a.ay_start_year,
                    a.ay_end_year,
                    STRING_AGG(cr.crs_title, ', ') AS course_titles
                FROM curriculum_course c
                JOIN academic_year a ON c.ay_code = a.ay_code
                JOIN course cr ON c.crs_code = cr.crs_code
                GROUP BY c.prog_code, c.cur_year_level, c.cur_semester, a.ay_start_year, a.ay_end_year
                ORDER BY a.ay_start_year DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new
                                {
                                    progCode = reader.GetString(0),
                                    yearLevel = reader.GetString(1),
                                    semester = reader.GetString(2),
                                    ayDisplay = $"{reader.GetInt32(3)}-{reader.GetInt32(4)}",
                                    courses = reader.GetString(5)
                                });
                            }
                        }
                    }
                }
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log exception if needed
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }
    }

}