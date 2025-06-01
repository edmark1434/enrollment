using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using EnrollmentSystem.Models;
using Npgsql;

namespace EnrollmentSystem.Controllers
{
    public class StudentEnrollmentController : Controller
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString;

        // 📋 Show list of pending enrollments (for Program Head)
        public ActionResult ManageEnrollments()
        {
            var pendingEnrollments = GetPendingEnrollments();
            return View("~/Views/Program-Head/EnrollmentApproval.cshtml", pendingEnrollments);
        }
        
        public ActionResult StudentProfile()
        {
            var sessionStudCode = Session["Stud_Code"];
            if (sessionStudCode == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int studCode;
            if (!int.TryParse(sessionStudCode.ToString(), out studCode))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var student = GetStudentById(studCode);
                if (student == null)
                {
                    ViewBag.ErrorMessage = "Student not found.";
                    return View("~/Views/Shared/Error.cshtml");
                }

                // Optional: Fetch Program Title from DB
                string programTitle = "N/A";
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                               @"SELECT p.prog_title 
                  FROM student s
                  JOIN program p ON s.prog_code = p.prog_code
                  WHERE s.stud_code = @studCode", conn))
                    {
                        cmd.Parameters.AddWithValue("studCode", studCode);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            programTitle = result.ToString();
                        }
                    }
                }

                ViewBag.ProgramTitle = programTitle;

                return View("~/Views/Main/StudentProfile.cshtml", student);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading student data: {ex.Message}";
                return View("~/Views/Shared/Error.cshtml");
            }
        }
        
        [HttpPost]
        public ActionResult Enroll(FormCollection form)
        {
            try
            {
                // Log all form data for debugging
                System.Diagnostics.Debug.WriteLine("=== ENROLL METHOD CALLED ===");
                System.Diagnostics.Debug.WriteLine($"Form keys count: {form.AllKeys.Length}");
                
                foreach (string key in form.AllKeys)
                {
                    System.Diagnostics.Debug.WriteLine($"Form[{key}] = '{form[key]}'");
                }

                // Check session
                var sessionStudCode = Session["Stud_Code"];
                System.Diagnostics.Debug.WriteLine($"Session Stud_Code: {sessionStudCode}");
                
                if (sessionStudCode == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Session Stud_Code is null - redirecting to login");
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                int studCode;
                if (!int.TryParse(sessionStudCode.ToString(), out studCode))
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Cannot parse Stud_Code from session");
                    TempData["ErrorMessage"] = "Invalid session data. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                System.Diagnostics.Debug.WriteLine($"Parsed Student Code: {studCode}");

                // Extract form data
                string yearLevel = form["yearLevel"];
                string semester = form["enrollmentSemester"];
                string ayCode = form["Academic Year"]; // Note the space in the key name
                string selectedCourseCodesString = form["selectedCourseCodes"];

                System.Diagnostics.Debug.WriteLine($"Extracted form data:");
                System.Diagnostics.Debug.WriteLine($"  yearLevel: '{yearLevel}'");
                System.Diagnostics.Debug.WriteLine($"  semester: '{semester}'");
                System.Diagnostics.Debug.WriteLine($"  ayCode: '{ayCode}'");
                System.Diagnostics.Debug.WriteLine($"  selectedCourseCodes: '{selectedCourseCodesString}'");

                // Validate required fields
                if (string.IsNullOrEmpty(yearLevel))
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Year Level is missing");
                    TempData["ErrorMessage"] = "Year Level is required.";
                    return RedirectToAction("Student_Enrollment");
                }

                if (string.IsNullOrEmpty(semester))
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Semester is missing");
                    TempData["ErrorMessage"] = "Semester is required.";
                    return RedirectToAction("Student_Enrollment");
                }

                if (string.IsNullOrEmpty(ayCode))
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Academic Year is missing");
                    TempData["ErrorMessage"] = "Academic Year is required.";
                    return RedirectToAction("Student_Enrollment");
                }

                // Parse selected course codes
                List<string> selectedCourseCodes = new List<string>();
                if (!string.IsNullOrEmpty(selectedCourseCodesString))
                {
                    selectedCourseCodes = selectedCourseCodesString.Split(',')
                        .Where(code => !string.IsNullOrWhiteSpace(code))
                        .Select(code => code.Trim())
                        .ToList();
                }

                System.Diagnostics.Debug.WriteLine($"Parsed {selectedCourseCodes.Count} course codes: [{string.Join(", ", selectedCourseCodes)}]");

                if (selectedCourseCodes.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: No subjects selected");
                    TempData["ErrorMessage"] = "Please select at least one subject.";
                    return RedirectToAction("Student_Enrollment");
                }

                // Database operations
                System.Diagnostics.Debug.WriteLine("Starting database operations...");
                
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    System.Diagnostics.Debug.WriteLine("Database connection opened successfully");

                    // Get student information
                    string fname = "";
                    string lname = "";
                    int actualStudId = 0;

                    string getStudentQuery = "SELECT stud_id, stud_fname, stud_lname FROM student WHERE stud_code = @studCode";
                    System.Diagnostics.Debug.WriteLine($"Executing query: {getStudentQuery} with studCode = {studCode}");

                    using (var getStudentCmd = new NpgsqlCommand(getStudentQuery, conn))
                    {
                        getStudentCmd.Parameters.AddWithValue("studCode", studCode);
                        using (var reader = getStudentCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                actualStudId = Convert.ToInt32(reader["stud_id"]);
                                System.Diagnostics.Debug.WriteLine($"Found student: ID={actualStudId}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("ERROR: Student not found in database");
                                TempData["ErrorMessage"] = "Student record not found.";
                                return RedirectToAction("Student_Enrollment");
                            }
                        }
                    }

                    // Insert enrollment records
                    int successfulInserts = 0;
                    string insertQuery = @"INSERT INTO enrollment (
                        enrol_status, enrol_date, enrol_yr_level, enrol_sem, 
                        stud_id, crs_code, ay_code
                    ) VALUES (
                        @status, @enrolDate, @yearLevel, @semester, 
                        @studId, @crsCode, @ayCode
                    )";

                    System.Diagnostics.Debug.WriteLine($"Insert query: {insertQuery}");

                    foreach (var crsCode in selectedCourseCodes)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inserting enrollment for course: '{crsCode}'");
                        
                        using (var cmd = new NpgsqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("status", "Pending");
                            cmd.Parameters.AddWithValue("enrolDate", DateTime.Now.Date);
                            cmd.Parameters.AddWithValue("yearLevel", yearLevel);
                            cmd.Parameters.AddWithValue("semester", semester);
                            cmd.Parameters.AddWithValue("studId", actualStudId);
                            cmd.Parameters.AddWithValue("crsCode", crsCode);
                            cmd.Parameters.AddWithValue("ayCode", ayCode);

                            // Log parameters
                            System.Diagnostics.Debug.WriteLine("Parameters:");
                            foreach (NpgsqlParameter param in cmd.Parameters)
                            {
                                System.Diagnostics.Debug.WriteLine($"  {param.ParameterName} = '{param.Value}'");
                            }

                            int rowsAffected = cmd.ExecuteNonQuery();
                            System.Diagnostics.Debug.WriteLine($"Rows affected: {rowsAffected}");
                            
                            if (rowsAffected > 0)
                            {
                                successfulInserts++;
                            }
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Successfully inserted {successfulInserts} out of {selectedCourseCodes.Count} enrollment records");
                    TempData["SuccessMessage"] = "Enrollment Successful.";
                    return RedirectToAction("Student_Enrollment");
                }
                return RedirectToAction("Student_Enrollment");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EXCEPTION in Enroll method:");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                TempData["ErrorMessage"] = $"Error submitting enrollment: {ex.Message}";
                return RedirectToAction("Student_Enrollment");
            }
        }

        [HttpGet]
        public JsonResult GetAvailableSubjects(string cur_year_level, string cur_semester, string prog_code)
        {
            try
            {
                var subjects = new List<SubjectViewModel>();

                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                    SELECT 
                        s.crs_code,
                        c.crs_title,
                        se.tsl_start_time,
                        se.tsl_end_time,
                        se.tsl_day,
                        c.crs_units,
                        s.room
                    FROM schedule s
                    JOIN course c ON s.crs_code = c.crs_code
                    JOIN session se ON s.schd_id = se.schd_id
                    JOIN curriculum_course cc ON cc.crs_code = s.crs_code
                    WHERE cc.cur_year_level = @cur_year_level
                      AND cc.cur_semester = @cur_semester
                      AND cc.prog_code = @prog_code";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@cur_year_level", cur_year_level);
                        cmd.Parameters.AddWithValue("@cur_semester", cur_semester);
                        cmd.Parameters.AddWithValue("@prog_code", prog_code);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var dayNum = Convert.ToInt32(reader["tsl_day"]);
                                var dayStr = dayNum switch
                                {
                                    1 => "M",
                                    2 => "T",
                                    3 => "W",
                                    4 => "Th",
                                    5 => "F",
                                    _ => "N/A"
                                };

                                var startTime = reader["tsl_start_time"] == DBNull.Value ? "" : reader["tsl_start_time"].ToString();
                                var endTime = reader["tsl_end_time"] == DBNull.Value ? "" : reader["tsl_end_time"].ToString();
                                var room = reader["room"] == DBNull.Value ? "N/A" : reader["room"].ToString();

                                subjects.Add(new SubjectViewModel
                                {
                                    CourseCode = reader["crs_code"]?.ToString(),
                                    Title = reader["crs_title"]?.ToString(),
                                    Time = $"{startTime} - {endTime}",
                                    Days = dayStr,
                                    Room = room,
                                    Units = Convert.ToInt32(reader["crs_units"])
                                });
                            }
                        }
                    }
                }

                return Json(subjects, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace }, JsonRequestBehavior.AllowGet);
            }
        }

        // 🧑‍🎓 Load student enrollment form
        public ActionResult Student_Enrollment()
        {
            var sessionStudCode = Session["Stud_Code"];

            if (sessionStudCode == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int studCode;
            if (!int.TryParse(sessionStudCode.ToString(), out studCode))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Load student data
                var student = GetStudentById(studCode);
                if (student == null)
                {
                    ViewBag.ErrorMessage = "Student not found.";
                    return View("~/Views/Shared/Error.cshtml");
                }

                // Load programs for dropdown
                var programs = GetProgramsFromDatabase();
                ViewBag.Programs = programs;

                var academicYears = GetAcademicYears();
                ViewBag.AcademicYears = academicYears;

                // Return the view with student model and programs in ViewBag
                return View("~/Views/Main/StudentEnroll.cshtml", student);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading student data: {ex.Message}";
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        // 🔍 Get student by ID from database
        private Student GetStudentById(int studCode)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
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
                            return new Student
                            {
                                Stud_Id = reader.GetInt32(reader.GetOrdinal("stud_id")),
                                Stud_Lname = reader["stud_lname"]?.ToString(),
                                Stud_Fname = reader["stud_fname"]?.ToString(),
                                Stud_Mname = reader["stud_mname"]?.ToString(),
                                Stud_Dob = Convert.ToDateTime(reader["stud_dob"]),
                                Stud_Contact = reader["stud_contact"]?.ToString(),
                                Stud_Email = reader["stud_email"]?.ToString(),
                                Stud_Address = reader["stud_address"]?.ToString(),
                                Stud_Code = Convert.ToInt32(reader["stud_code"])
                            };
                        }
                    }
                }
            }

            return null;
        }

        // 🔍 Load programs from database
        private List<Program> GetProgramsFromDatabase()
        {
            var programs = new List<Program>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT \"prog_code\", \"prog_title\" FROM \"program\"", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            programs.Add(new Program
                            {
                                ProgCode = reader.GetString(0),
                                ProgTitle = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return programs;
        }

        // 🔍 Load academic years from database
        private List<AcademicYear> GetAcademicYears()
        {
            var academicYears = new List<AcademicYear>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT ay_code, ay_start_year, ay_end_year FROM academic_year", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            academicYears.Add(new AcademicYear
                            {
                                AyCode = reader.GetString(0),
                                AyStartYear = reader.GetInt32(1),
                                AyEndYear = reader.GetInt32(2)
                            });
                        }
                    }
                }
            }

            return academicYears;
        }

        // 📥 Submit selected courses for approval
        [HttpPost]
        public JsonResult SubmitSelectedSubjects(List<string> selectedCourseCodes)
        {
            var sessionStudCode = Session["Stud_Code"];
            if (sessionStudCode == null || selectedCourseCodes == null || !selectedCourseCodes.Any())
            {
                return Json(new { success = false, message = "Invalid request." });
            }

            int studId;
            if (!int.TryParse(sessionStudCode.ToString(), out studId))
            {
                return Json(new { success = false, message = "Student not logged in." });
            }

            string yearLevel = Request.Form["yearLevel"];
            string semester = Request.Form["enrollmentSemester"];
            string ayCode = Request.Form["Academic Year"];

            if (string.IsNullOrEmpty(yearLevel) || string.IsNullOrEmpty(semester) || string.IsNullOrEmpty(ayCode))
            {
                return Json(new { success = false, message = "Missing required fields." });
            }

            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();

                    foreach (var crsCode in selectedCourseCodes)
                    {
                        using (var cmd = new NpgsqlCommand(
                            @"INSERT INTO enrollment (
                                enrol_status,
                                enrol_yr_level,
                                enrol_sem,
                                stud_id,
                                crs_code,
                                ay_code
                            ) VALUES (
                                @status,
                                @yearLevel,
                                @semester,
                                @studId,
                                @crsCode,
                                @ayCode
                            )", conn))
                        {
                            cmd.Parameters.AddWithValue("status", "Pending");
                            cmd.Parameters.AddWithValue("yearLevel", yearLevel);
                            cmd.Parameters.AddWithValue("semester", semester);
                            cmd.Parameters.AddWithValue("studId", studId);
                            cmd.Parameters.AddWithValue("crsCode", crsCode);
                            cmd.Parameters.AddWithValue("ayCode", ayCode);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                return Json(new { success = true, message = "Requests submitted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error submitting requests.", error = ex.Message });
            }
        }

        // 🔍 Get all pending enrollments
        private List<Enrollment> GetPendingEnrollments()
        {
            var enrollments = new List<Enrollment>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"
                SELECT * FROM enrollment 
                WHERE enrol_status = 'Pending' 
                ORDER BY enrol_date DESC";

                using (var cmd = new NpgsqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        enrollments.Add(new Enrollment
                        {
                            Enrol_Id = Convert.ToInt32(reader["enrol_id"]),
                            Enrol_Status = reader["enrol_status"]?.ToString(),
                            Enrol_Date = Convert.ToDateTime(reader["enrol_date"]),
                            Enrol_Yr_Level = reader["enrol_yr_level"]?.ToString(),
                            Enrol_Sem = reader["enrol_sem"]?.ToString(),
                            Stud_Id = reader.IsDBNull(reader.GetOrdinal("stud_id")) ? (int?)null : Convert.ToInt32(reader["stud_id"]),
                            Crs_Code = reader["crs_code"]?.ToString(),
                            Ay_Code = reader["ay_code"]?.ToString()
                        });
                    }
                }
            }

            return enrollments;
        }

        // ✅ Approve a specific enrollment
        [HttpPost]
        public JsonResult ApproveEnrollment(int enrolId)
        {
            return ProcessEnrollment(enrolId, "Completed");
        }

        // ❌ Reject a specific enrollment
        [HttpPost]
        public JsonResult RejectEnrollment(int enrolId)
        {
            return ProcessEnrollment(enrolId, "Rejected");
        }

        // 🛠️ Shared method to approve or reject
        private JsonResult ProcessEnrollment(int enrolId, string status)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var cmd = new NpgsqlCommand(
                        "UPDATE enrollment SET enrol_status = @status WHERE enrol_id = @enrolId",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("status", status);
                        cmd.Parameters.AddWithValue("enrolId", enrolId);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Json(new { success = true, message = $"Enrollment {status} successfully!" });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Enrollment not found." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error processing enrollment.", error = ex.Message });
            }
        }
    }
}