using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using EnrollmentSystem.Models;
using Npgsql;

namespace EnrollmentSystem.Controllers.Service
{
    public class FetchService : IFetchService
    {
        private readonly string _connectionString;

        public FetchService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString;
        }
        [HttpGet]
        public List<Course> GetCoursesFromDatabase()
        {
            var courses = new List<Course>();

            using (var conn = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand(@"
                SELECT 
                    c.CRS_CODE, 
                    c.CRS_TITLE, 
                    COALESCE(cat.CTG_NAME, 'General') AS Category,
                    p.PREQ_CRS_CODE,
                    c.CRS_UNITS, 
                    c.CRS_LEC, 
                    c.CRS_LAB
                FROM COURSE c
                LEFT JOIN COURSE_CATEGORY cat ON c.CTG_CODE = cat.CTG_CODE
                LEFT JOIN PREREQUISITE p ON p.CRS_CODE = c.CRS_CODE", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        courses.Add(new Course
                        {
                            Crs_Code = reader["CRS_CODE"]?.ToString(),
                            Crs_Title = reader["CRS_TITLE"]?.ToString(),
                            Ctg_Name = reader["Category"]?.ToString(),
                            Preq_Crs_Code = reader["PREQ_CRS_CODE"]?.ToString(),
                            Crs_Units = reader["CRS_UNITS"] != DBNull.Value ? Convert.ToDecimal(reader["CRS_UNITS"]) : 0,
                            Crs_Lec = reader["CRS_LEC"] != DBNull.Value ? Convert.ToInt32(reader["CRS_LEC"]) : 0,
                            Crs_Lab = reader["CRS_LAB"] != DBNull.Value ? Convert.ToInt32(reader["CRS_LAB"]) : 0
                        });
                    }
                }
            }

            return courses;
        }
        [HttpGet]
        public List<Program> GetProgramsFromDatabase()
        {
            var programs = new List<Program>();

            using (var conn = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand("SELECT \"prog_code\", \"prog_title\" FROM \"program\"", conn))
            {
                conn.Open();
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

            return programs;
        }
        [HttpGet]
        public List<BlkSec> GetAllBlockSections()
        {
            var sections = new List<BlkSec>();

            using (var conn = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand("SELECT blk_sec_id, prog_code, blk_sec_code FROM blk_sec", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sections.Add(new BlkSec
                        {
                            BlkSecId = reader.GetInt32(0),
                            ProgCode = reader.GetString(1),
                            BlkSecCode = reader.GetString(2)
                        });
                    }
                }
            }

            return sections;
        }
        [HttpGet]
        public Student GetStudentInfo(int studentId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand(@"
                SELECT 
                    stud_id, stud_lname, stud_fname, stud_mname, 
                    stud_dob, stud_contact, stud_email, stud_address, 
                    stud_password, created_at, stud_code 
                FROM student
                WHERE stud_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", studentId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Student
                        {
                            Stud_Id = reader.GetInt32(0),
                            Stud_Lname = reader.GetString(1),
                            Stud_Fname = reader.GetString(2),
                            Stud_Mname = reader.GetString(3),
                            Stud_Dob = reader.GetDateTime(4),
                            Stud_Contact = reader.GetString(5),
                            Stud_Email = reader.GetString(6),
                            Stud_Address = reader.GetString(7),
                            Stud_Password = reader.GetString(8),
                            Created_At = reader.GetDateTime(9),
                            Stud_Code = reader.GetInt32(10)
                        };
                    }
                }
            }

            return null;
        }
        [HttpGet]
        public List<AcademicYear> GetAcademicYearsFromDatabase()
        {
            var academicYears = new List<AcademicYear>();

            using (var conn = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand("SELECT ay_code, ay_start_year, ay_end_year FROM academic_year", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        academicYears.Add(new AcademicYear
                        {
                            AyCode = reader.GetString(0),
                            AyStartYear = reader.GetInt16(1),
                            AyEndYear = reader.GetInt16(2),
                        });
                    }
                }
            }

            return academicYears;
        }
        [HttpGet]
        public List<Semester> GetSemesterFromDatabase()
        {
            var semesters = new List<Semester>();

            using (var conn = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand("SELECT \"sem_id\", \"sem_name\" FROM \"semester\"", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        semesters.Add(new Semester
                        {
                            SemId = reader.GetInt16(0),
                            SemName = reader.GetString(1)
                        });
                    }
                }
            }

            return semesters;
        }
        [HttpGet]
        public CurrentEnrollment GetCurrentEnrollments()
        {
            var currentEnrollments = new CurrentEnrollment();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                using (var cmd = new NpgsqlCommand("SELECT * FROM enrollment_season", conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            currentEnrollments.enrollment_season_id = reader.GetInt32(0);
                            currentEnrollments.ay_code = reader.GetString(1);
                            currentEnrollments.sem_id = reader.GetInt16(2);
                            currentEnrollments.is_open = reader.GetBoolean(3);
                        }
                    }
                }
            }

            return currentEnrollments;
        }
        [HttpGet]
        public List<string> GetYearSemesterOptions()
        {
            var academicYears = GetAcademicYearsFromDatabase();
            var semesters = GetSemesterFromDatabase();

            var result = new List<string>();
                    
            foreach (var ay in academicYears)
            {
                foreach (var sem in semesters)
                {
                    result.Add($"{ay.AyCode} - {sem.SemName}");
                }
            }

            return result;
        }
        [HttpGet]
        public List<Room> GetRoomsFromDatabase()
        {
            var rooms = new List<Room>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                using (var cmd = new NpgsqlCommand("SELECT * FROM room", conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rooms.Add(new Room
                            {
                                room_id = reader.GetInt32(0),
                                room_code = reader.GetString(1),
                                prog_code = reader.GetString(2),
                            });
                        }
                    }
                }
            }
           return rooms;
        }
        [HttpGet]
        public List<Professor> GetProfessorFromDatabase()
        {
            var professor = new List<Professor>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                using (var cmd = new NpgsqlCommand("SELECT * FROM professor", conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            professor.Add(new Professor
                            {
                                prof_id = reader.GetInt32(0),
                                prof_name = reader.GetString(1),
                                prog_code = reader.GetString(2)
                            });
                        }
                    }
                }
            }
            return professor;
        }
        
    }
   

    public interface IFetchService
    {
        List<Course> GetCoursesFromDatabase();
        List<Program> GetProgramsFromDatabase();
        List<BlkSec> GetAllBlockSections();
        Student GetStudentInfo(int studentId);
        List<AcademicYear> GetAcademicYearsFromDatabase();
        List<Semester> GetSemesterFromDatabase();
        List<string> GetYearSemesterOptions();
        CurrentEnrollment GetCurrentEnrollments();
        List<Room> GetRoomsFromDatabase();
        List<Professor> GetProfessorFromDatabase();
    }
}