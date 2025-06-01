using System;
using System.Configuration;
using System.Web.Mvc;
using EnrollmentSystem.Controllers.Service;
using EnrollmentSystem.Models;
using Npgsql;

namespace EnrollmentSystem.Controllers
{
    public class MainController : Controller
    {
        private string connString = ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString;        // GET: /Login
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

         return View("~/Views/Main/StudentProfile.cshtml",student);
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
            return View("~/Views/Main/ClassSchedule.cshtml");
        }
    }
}