using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using EnrollmentSystem.Models;
using Npgsql;

namespace EnrollmentSystem.Controllers
{
    public class CourseController : Controller
    {
        private readonly string _connectionString;

        public CourseController()
        {
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString;
        }

        public ActionResult Course()
        {
            var courses = GetCoursesFromDatabase(); 
            return View("~/Views/Admin/Courses.cshtml", courses);
        }
        [HttpPost]
        public JsonResult DeleteCourse(string id)
        {
            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Invalid course ID." });

            try
            {
                using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("DELETE FROM course WHERE crs_code = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return Json(new { success = true, message = "Course deleted successfully." });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Course not found." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting course: " + ex.Message });
            }
        }
        [HttpGet]
        public JsonResult GetAllCoursesForDropdown()
        {
            var courses = new List<dynamic>();

            using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT crs_code, crs_title FROM course ORDER BY crs_code", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            courses.Add(new
                            {
                                id = reader.GetString(0),
                                text = $"{reader.GetString(0)} - {reader.GetString(1)}"
                            });
                        }
                    }
                }
            }

            return Json(courses, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult UpdateCourse(string crsCode, string crsTitle, string preqCrsCode, decimal crsUnits, int crsLec, int crsLab)
        {
            try
            {
                using (var conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(@"
                UPDATE course SET 
                    crs_title = @title,
                    preq_crs_code = @prereq,
                    crs_units = @units,
                    crs_lec = @lec,
                    crs_lab = @lab
                WHERE crs_code = @code", conn))
                    {
                        cmd.Parameters.AddWithValue("@code", crsCode);
                        cmd.Parameters.AddWithValue("@title", crsTitle);
                        cmd.Parameters.AddWithValue("@prereq", string.IsNullOrEmpty(preqCrsCode) ? (object)DBNull.Value : preqCrsCode);
                        cmd.Parameters.AddWithValue("@units", crsUnits);
                        cmd.Parameters.AddWithValue("@lec", crsLec);
                        cmd.Parameters.AddWithValue("@lab", crsLab);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Json(new { success = true, message = "Course updated." });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Course not found." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // GET: /Course/Create
        public ActionResult Create()
        {
            return View("~/Views/Admin/AddProgram.cshtml");
        }

        // GET: /Course/Edit/{id}
        public ActionResult Edit(string id)
        {
            var course = GetCourseById(id);
            if (course == null) return HttpNotFound();

            return View("~/Views/Admin/EditProgram.cshtml", course);
        }

        // GET: /Course/GetAllCourses
        [HttpGet]
        public JsonResult GetAllCourses(string progCode = null, string ayCode = null)
        {
            var courses = new List<dynamic>();
            try
            {
                using (var db = new NpgsqlConnection(_connectionString))
                {
                    db.Open();
                    using (var cmd = new NpgsqlCommand(@"
                        SELECT 
                            c.crs_code, 
                            c.crs_title, 
                            COALESCE(cat.ctg_name, 'General') AS category,
                            COALESCE(p.preq_crs_code, 'None') AS prerequisite,
                            c.crs_units, 
                            c.crs_lec, 
                            c.crs_lab
                        FROM course c
                        LEFT JOIN course_category cat ON c.ctg_code = cat.ctg_code
                        LEFT JOIN prerequisite p ON p.crs_code = c.crs_code
                    ", db))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                courses.Add(new
                                {
                                    code = reader["crs_code"].ToString(),
                                    title = reader["crs_title"].ToString(),
                                    category = reader["category"].ToString(),
                                    prerequisite = reader["prerequisite"].ToString(),
                                    units = reader["crs_units"] != DBNull.Value ? Convert.ToDecimal(reader["crs_units"]) : 0,
                                    lec = reader["crs_lec"] != DBNull.Value ? Convert.ToInt32(reader["crs_lec"]) : 0,
                                    lab = reader["crs_lab"] != DBNull.Value ? Convert.ToInt32(reader["crs_lab"]) : 0
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception or handle as needed
                Console.WriteLine($"Error fetching courses: {ex.Message}");
            }

            return Json(courses, JsonRequestBehavior.AllowGet);
        }

        private List<Course> GetCoursesFromDatabase()
        {
            var courses = new List<Course>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT 
                        c.crs_code, 
                        c.crs_title, 
                        COALESCE(cat.ctg_name, 'General') AS category,
                        p.preq_crs_code,
                        c.crs_units, 
                        c.crs_lec, 
                        c.crs_lab
                    FROM course c
                    LEFT JOIN course_category cat ON c.ctg_code = cat.ctg_code
                    LEFT JOIN prerequisite p ON p.crs_code = c.crs_code", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            courses.Add(new Course
                            {
                                Crs_Code = reader["crs_code"]?.ToString(),
                                Crs_Title = reader["crs_title"]?.ToString(),
                                Ctg_Name = reader["category"]?.ToString(),
                                Preq_Crs_Code = reader["preq_crs_code"]?.ToString(),
                                Crs_Units = reader["crs_units"] != DBNull.Value ? Convert.ToDecimal(reader["crs_units"]) : 0,
                                Crs_Lec = reader["crs_lec"] != DBNull.Value ? Convert.ToInt32(reader["crs_lec"]) : 0,
                                Crs_Lab = reader["crs_lab"] != DBNull.Value ? Convert.ToInt32(reader["crs_lab"]) : 0
                            });
                        }
                    }
                }
            }

            return courses;
        }

        private object GetCourseById(string id)
        {
            // Implement logic to fetch single course by ID
            // For now, just returning null
            return null;
        }
        
    }
}