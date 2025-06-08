using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using EnrollmentSystem.Models;
using Microsoft.Ajax.Utilities;
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
            using (var cmd = new NpgsqlCommand("SELECT blk_sec_id, prog_code, blk_sec_name FROM block_section", conn))
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

        public List<BlkSec> GetSectionByProgram(string program, string yr_level)
        {
            var sections = new List<BlkSec>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();

                string command;
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;

                    if (string.IsNullOrEmpty(yr_level))
                    {
                        // Only filter by program
                        command = "SELECT * FROM block_section WHERE prog_code = @program";
                        cmd.CommandText = command;
                        cmd.Parameters.AddWithValue("@program", program);
                    }
                    else
                    {
                        // Filter by program and year level
                        command = "SELECT * FROM block_section WHERE prog_code = @program AND yr_level = @yr_level";
                        cmd.CommandText = command;
                        cmd.Parameters.AddWithValue("@program", program);
                        cmd.Parameters.AddWithValue("@yr_level", yr_level);
                    }

                    var result = cmd.ExecuteReader();
                    while (result.Read())
                    {
                        sections.Add(new BlkSec
                        {
                            BlkSecId = result.GetInt32(0),
                            ProgCode = result.GetString(1),
                            BlkSecCode = result.GetString(3)
                        });
                    }
                }
            }
            return sections;
        }
        public List<Course> GetAllCourseByProgram(string program)
        {
            var Courses = new List<Course>();
            var enrollment = GetCurrentEnrollments();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var cmd = new NpgsqlCommand(
                    "SELECT C.crs_code, C.crs_title, C.crs_units, C.crs_lec, C.crs_lab, C.ctg_code, C.preq_id " +
                    "FROM curriculum_course AS CC " +
                    "INNER JOIN course AS C ON C.crs_code = CC.crs_code " +
                    "WHERE CC.prog_code = @program AND cc.ay_code = @ay AND cc.cur_semester = @sem", conn);

                cmd.Parameters.AddWithValue("@program", program);
                cmd.Parameters.AddWithValue("@ay", enrollment.ay_code);
                cmd.Parameters.AddWithValue("@sem", enrollment.sem_id);
                conn.Open();

                var result = cmd.ExecuteReader();
                while (result.Read())
                {
                    Courses.Add(new Course
                    {
                        Crs_Code = result["crs_code"].ToString(),
                        Crs_Title = result["crs_title"].ToString(),
                        Crs_Units = result.GetDecimal(result.GetOrdinal("crs_units")),
                        Crs_Lec = result.GetInt32(result.GetOrdinal("crs_lec")),
                        Crs_Lab = result.GetInt32(result.GetOrdinal("crs_lab")),
                        Ctg_Code = result["ctg_code"] != DBNull.Value ? result["ctg_code"].ToString() : null,
                        Preq_Crs_Code = result["preq_id"] != DBNull.Value ? result["preq_id"].ToString() : null,
                        Ctg_Name = null // Optional: set this later if needed via another join or service
                    });
                }
            }

            return Courses;
        }

        public List<Professor> GetAllProfessorsByProgram(string program)
        {
            var professors = new List<Professor>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand(
                    "SELECT prof_id, prof_name, prog_code " +
                    "FROM professor " +
                    "WHERE prog_code = @program", conn);
                cmd.Parameters.AddWithValue("@program", program);
                var result = cmd.ExecuteReader();
                while (result.Read())
                {
                    professors.Add(new Professor
                    {
                        prof_id = result.GetInt32(0),
                        prof_name = result.GetString(1),
                        prog_code = result.GetString(2)
                    });
                }
            }
            return professors;
        }
        public List<Room> GetAllRooms(string program)
        {
            var rooms = new List<Room>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var cmd = new NpgsqlCommand("SELECT room_id, room_code, prog_code FROM room where prog_code = @program", conn);
                conn.Open();
                cmd.Parameters.AddWithValue("@program", program);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    rooms.Add(new Room
                    {
                        room_id = reader.GetInt32(reader.GetOrdinal("room_id")),
                        room_code = reader["room_code"] != DBNull.Value ? reader["room_code"].ToString() : null,
                        prog_code = reader["prog_code"] != DBNull.Value ? reader["prog_code"].ToString() : null
                    });
                }
            }

            return rooms;
        }

        public List<BlkSec> GetAllBlockSectionsByProgram(string program, string crs_code)
        {
            var sections = new List<BlkSec>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();

                var cmd = new NpgsqlCommand(@"
            SELECT DISTINCT b.blk_sec_id, b.blk_sec_name, b.prog_code
            FROM curriculum_course AS cc
            INNER JOIN block_section AS b 
                ON b.prog_code = cc.prog_code AND b.yr_level = cc.cur_year_level
            WHERE cc.prog_code = @program AND cc.crs_code = @crs_code", conn);

                cmd.Parameters.AddWithValue("@program", program);
                cmd.Parameters.AddWithValue("@crs_code", crs_code);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    sections.Add(new BlkSec
                    {
                        BlkSecId = reader.GetInt32(reader.GetOrdinal("blk_sec_id")),
                        BlkSecCode = reader["blk_sec_name"].ToString(),
                        ProgCode = reader["prog_code"].ToString()
                    });
                }
            }

            return sections;
        }
        public List<ScheduleViewModel> GetSchedulesBySection(int sectionId)
        {
            var result = new List<ScheduleViewModel>();

            using (var conn = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand(@"
        SELECT 
            s.mis_code,
            s.crs_code,
            c.crs_title AS course_name,
            s.day,
            s.start_time,
            s.end_time,
            r.room_code AS room,
            p.prof_name AS professor,
            b.blk_sec_name AS section,
            (c.crs_lec + c.crs_lab) AS total_units
        FROM schedule s
        LEFT JOIN course c ON s.crs_code = c.crs_code
        LEFT JOIN room r ON s.room_id = r.room_id
        LEFT JOIN professor p ON s.prof_id = p.prof_id
        LEFT JOIN block_section b ON s.blk_sec_id = b.blk_sec_id
        WHERE s.blk_sec_id = @sectionId
        ORDER BY s.day, s.start_time", conn))
            {
                cmd.Parameters.AddWithValue("sectionId", sectionId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ScheduleViewModel
                        {
                            MisCode = reader["mis_code"]?.ToString(),
                            Course = reader["crs_code"]?.ToString(),
                            CourseName = reader["course_name"]?.ToString(),
                            Day = reader["day"]?.ToString(),
                            StartTime = reader["start_time"]?.ToString(),
                            EndTime = reader["end_time"]?.ToString(),
                            Room = reader["room"]?.ToString(),
                            Professor = reader["professor"]?.ToString(),
                            Section = reader["section"]?.ToString(),
                            Units = reader["total_units"] is int u ? u : 3
                        });
                    }
                }
            }

            return result;
        }
        public List<ScheduleViewModel> GetAllSecondSemesterSchedules()
        {
            var schedules = new List<ScheduleViewModel>();

            string query = @"
                SELECT 
                    s.mis_code,
                    s.crs_code,
                    c.crs_title AS course_name,
                    s.day,
                    s.start_time,
                    s.end_time,
                    r.room_code AS room,
                    p.prof_name AS professor,
                    b.blk_sec_name AS section,
                    (c.crs_lec + c.crs_lab) as total_units
                FROM schedule s
                LEFT JOIN course c ON s.crs_code = c.crs_code
                LEFT JOIN room r ON s.room_id = r.room_id
                LEFT JOIN professor p ON s.prof_id = p.prof_id
                LEFT JOIN block_section b ON s.blk_sec_id = b.blk_sec_id
                WHERE EXISTS (
                    SELECT 1 FROM curriculum_course cc
                    WHERE cc.crs_code = s.crs_code AND cc.cur_semester = 2
                )
                ORDER BY s.day, s.start_time";

            using (var conn = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        schedules.Add(new ScheduleViewModel
                        {
                            MisCode = reader["mis_code"]?.ToString(),
                            Course = reader["crs_code"]?.ToString(),
                            CourseName = reader["course_name"]?.ToString(),
                            Day = reader["day"]?.ToString(),
                            StartTime = reader["start_time"]?.ToString(),
                            EndTime = reader["end_time"]?.ToString(),
                            Room = reader["room"]?.ToString(),
                            Professor = reader["professor"]?.ToString(),
                            Section = reader["section"]?.ToString(),
                            Units = reader["total_units"] is int u ? u : 3
                        });
                    }
                }
            }

            return schedules;
        }
        
        

    }
    public class ScheduleViewModel
    {
        public int ScheduleId { get; set; }
        public string MisCode { get; set; }
        public string Course { get; set; }
        public string Day { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }

        // Display fields
        public string CourseName { get; set; }
        public string Professor { get; set; }
        public string Room { get; set; }
        public string Section { get; set; }

        // Unit info
        public int Units { get; set; } = 3;
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
        List<BlkSec> GetSectionByProgram(string program,string yr_level);
        List<BlkSec> GetAllBlockSectionsByProgram(string program, string crs_code);
        List<Course> GetAllCourseByProgram(string program);
        List<Professor> GetAllProfessorsByProgram(string program);
        List<Room> GetAllRooms(string program);
        List<ScheduleViewModel> GetAllSecondSemesterSchedules();
        List<ScheduleViewModel> GetSchedulesBySection(int sectionId);
    }
}