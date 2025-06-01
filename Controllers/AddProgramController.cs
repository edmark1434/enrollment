using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Policy;
using System.Web.Mvc;
using EnrollmentSystem.Models;
using Npgsql;

namespace EnrollmentSystem.Controllers
{
    public class AddProgramController : Controller
    {
        private readonly ICourseRepository _courseRepository;

        public AddProgramController()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString;
            _courseRepository = new CourseRepository(connectionString);
        }

        // GET: /AddProgram/
        public ActionResult Index()
        {
            try
            {
                ViewBag.CourseCategories = _courseRepository.GetCourseCategories();
                ViewBag.CoursesForPrereq = _courseRepository.GetAllCourses();
                return View("~/Views/Admin/AddProgram.cshtml");
            }
            catch (Exception ex)
            {
                // Log error here
                return View("Error");
            }
        }

        public JsonResult GetAllCourses()
        {
            var course = _courseRepository.GetAllCourses();
            return Json(new { data = course }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AddCourseAjax(Course course)
        {
            try
            {
                // Validate input
                if (course == null)
                {
                    return Json(new { success = false, message = "Course data is null." });
                }

                // Validate required fields
                var errors = new Dictionary<string, string>();

                if (string.IsNullOrWhiteSpace(course.Crs_Code))
                    errors.Add("Crs_Code", "Course code is required");

                if (string.IsNullOrWhiteSpace(course.Crs_Title))
                    errors.Add("Crs_Title", "Course title is required");

                if (string.IsNullOrWhiteSpace(course.Ctg_Code))
                    errors.Add("Ctg_Code", "Category code is required");

                if (errors.Any())
                    return Json(new { success = false, errors });

                // Handle null prerequisite
                course.Preq_Crs_Code = string.IsNullOrWhiteSpace(course.Preq_Crs_Code) ? null : course.Preq_Crs_Code;

                // Save to database
                var result = _courseRepository.AddCourse(course);
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddCourseAjax: {ex}");
                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred.",
                    error = ex.Message
                });
            }
        }
    }

    public interface ICourseRepository
    {
        JsonResult AddCourse(Course course);
        List<CourseCategory> GetCourseCategories();
        List<Course> GetAllCourses();
    }

    public class CourseRepository : ICourseRepository
    {
        private readonly string _connectionString;

        public CourseRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public JsonResult AddCourse(Course course)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Check for duplicate course code
                        if (CourseCodeExists(conn, course.Crs_Code))
                        {
                            return new JsonResult
                            {
                                Data = new { 
                                    success = false, 
                                    message = "A course with this code already exists." 
                                }
                            };
                        }

                        // Check for duplicate course title
                        if (CourseTitleExists(conn, course.Crs_Title))
                        {
                            return new JsonResult
                            {
                                Data = new { 
                                    success = false, 
                                    message =  "A course with this title already exists." 
                                }
                            };
                        }
                        InsertMainCourse(conn, course);

                        transaction.Commit();
                        return new JsonResult
                        {
                            Data = new { success = true, message = "Course added successfully!", redirectUrl = "/Admin/Admin_Course"
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return new JsonResult
                        {
                            Data = new { 
                                success = false, 
                                message = "An error occurred while saving the course.",
                                error = ex.Message 
                            }
                        };
                    }
                }
            }
        }

        private bool CourseCodeExists(NpgsqlConnection conn, string code)
        {
            const string sql = "SELECT 1 FROM COURSE WHERE CRS_CODE = @code";
            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("code", code);
                return cmd.ExecuteScalar() != null;
            }
        }

        private bool CourseTitleExists(NpgsqlConnection conn, string title)
        {
            const string sql = "SELECT 1 FROM COURSE WHERE CRS_TITLE = @title";
            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("title", title);
                return cmd.ExecuteScalar() != null;
            }
        }

        private void InsertMainCourse(NpgsqlConnection conn, Course course)
{
    const string insertCourseSql = @"
        INSERT INTO COURSE (
            CRS_CODE, 
            CRS_TITLE, 
            CTG_CODE, 
            PREQ_ID, 
            CRS_UNITS, 
            CRS_LEC, 
            CRS_LAB
        ) VALUES (
            @code, 
            @title, 
            @ctgCode, 
            @prereq, 
            @units, 
            @lec, 
            @lab)";

    // Join multiple prerequisites with comma for the PREQ_ID field
    string prereqString = course.Preq_Crs_Code != null ? 
        string.Join(",", course.Preq_Crs_Code) : 
        null;

    using (var cmd = new NpgsqlCommand(insertCourseSql, conn))
    {
        cmd.Parameters.AddWithValue("code", course.Crs_Code);
        cmd.Parameters.AddWithValue("title", course.Crs_Title);
        cmd.Parameters.AddWithValue("ctgCode", course.Ctg_Code);
        cmd.Parameters.AddWithValue("prereq", prereqString ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("units", course.Crs_Units);
        cmd.Parameters.AddWithValue("lec", course.Crs_Lec);
        cmd.Parameters.AddWithValue("lab", course.Crs_Lab);
        cmd.ExecuteNonQuery();
    }
    
    // Insert each prerequisite separately
    if (course.Preq_Crs_Code != null && course.Preq_Crs_Code.Length > 0)
    {
        const string insertPrereqSql = @"
            INSERT INTO PREREQUISITE (
                CRS_CODE, 
                PREQ_CRS_CODE
            ) VALUES (
                @crsCode, 
                @preqCode)";

        
           
                using (var cmd = new NpgsqlCommand(insertPrereqSql, conn))
                {
                    cmd.Parameters.AddWithValue("crsCode", course.Crs_Code);     
                    cmd.Parameters.AddWithValue("preqCode", course.Preq_Crs_Code); 
                    cmd.ExecuteNonQuery();
                }
            
        
    }
}

        

        public List<CourseCategory> GetCourseCategories()
        {
            const string sql = "SELECT CTG_CODE, CTG_NAME FROM COURSE_CATEGORY";
            return ExecuteQuery<CourseCategory>(sql, reader => new CourseCategory
            {
                Ctg_Code = reader["CTG_CODE"]?.ToString(),
                Ctg_Name = reader["CTG_NAME"]?.ToString()
            });
        }

        public List<Course> GetAllCourses()
        {
            const string sql = "SELECT CRS_CODE, CRS_TITLE FROM COURSE ORDER BY CRS_CODE";
            return ExecuteQuery<Course>(sql, reader => new Course
            {
                Crs_Code = reader["CRS_CODE"]?.ToString(),
                Crs_Title = reader["CRS_TITLE"]?.ToString()
            });
        }

        private List<T> ExecuteQuery<T>(string sql, Func<NpgsqlDataReader, T> mapper)
        {
            var results = new List<T>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(mapper(reader));
                        }
                    }
                }
            }

            return results;
        }
    }
}