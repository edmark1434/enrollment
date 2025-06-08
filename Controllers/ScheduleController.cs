using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using EnrollmentSystem.Models;
using Npgsql;

namespace EnrollmentSystem.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString;

        
        [HttpPost]
        public ActionResult SaveSchedule( ScheduleRequest request)
        {
            if (request == null || request.ScheduleDetails == null || !request.ScheduleDetails.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "No schedule details provided."
                });
            }

            string errorMessage = null;

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();

                foreach (var detail in request.ScheduleDetails)
                {
                    var day = detail.Day.Trim().ToLower();
                    var start = ParseTime(detail.StartTime);
                    var end = ParseTime(detail.EndTime);

                    if (start >= end)
                    {
                        errorMessage = "Invalid time range: Start must be before end.";
                        break;
                    }

                    // Validate against existing data in DB
                    if (!ValidateSection(request.Section, day, start, end, request.MisCode, conn, out var sectionMessage))
                    {
                        errorMessage = sectionMessage;
                        break;
                    }

                    if (request.ProfId != 0 && !ValidateProfessor(request.ProfId, day, start, end, request.MisCode, conn, out var profMessage))
                    {
                        errorMessage = profMessage;
                        break;
                    }
                    // 🔁 NEW VALIDATION HERE
                    if (!string.IsNullOrEmpty(request.Course))
                    {
                        if (!ValidateCourseInSection(request.Section, request.Course, request.MisCode, conn, out var courseMessage))
                        {
                            errorMessage = courseMessage;
                            break;
                        }
                    }
                }

                if (errorMessage != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = errorMessage
                    });
                }

                // Delete old entries if editing
                if (!string.IsNullOrEmpty(request.MisCode))
                {
                    using (var deleteCmd = new NpgsqlCommand("DELETE FROM schedule WHERE mis_code = @MisCode", conn))
                    {
                        deleteCmd.Parameters.AddWithValue("MisCode", request.MisCode);
                        deleteCmd.ExecuteNonQuery();
                    }
                }

                // Insert all schedule rows
                foreach (var detail in request.ScheduleDetails)
                {
                    var day = detail.Day.Trim().ToLower();
                    var start = ParseTime(detail.StartTime);
                    var end = ParseTime(detail.EndTime);

                    using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO schedule(mis_code, crs_code, day, start_time, end_time, room_id, prof_id, blk_sec_id)
                        VALUES (@MisCode, @CrsCode, @Day, @StartTime, @EndTime, @RoomId, @ProfId, @BlkSecId)",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("@MisCode", request.MisCode);
                        cmd.Parameters.AddWithValue("@CrsCode", request.Course);
                        cmd.Parameters.AddWithValue("@Day", day);
                        cmd.Parameters.AddWithValue("@StartTime", start);
                        cmd.Parameters.AddWithValue("@EndTime", end);
                        cmd.Parameters.AddWithValue("@RoomId", request.RoomId);
                        cmd.Parameters.AddWithValue("@ProfId", request.ProfId);
                        cmd.Parameters.AddWithValue("@BlkSecId", request.Section);

                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            return Json(new
                            {
                                success = false,
                                message = $"Failed to save schedule: {ex.Message}"
                            });
                        }
                    }
                }
            }

            return Json(new
            {
                success = true,
                message = "Schedule saved successfully"
            });
        }

        private bool ValidateSection(int sectionId, string day, TimeSpan start, TimeSpan end, string misCode, NpgsqlConnection conn, out string message)
        {
            message = null;

            using (var cmd = new NpgsqlCommand(@"
                SELECT EXISTS (
                    SELECT 1 FROM schedule 
                    WHERE blk_sec_id = @BlkSecId AND day ILIKE @Day 
                      AND NOT (end_time <= @StartTime OR start_time >= @EndTime)
                      AND mis_code != @MisCode)",
                conn))
            {
                cmd.Parameters.AddWithValue("@BlkSecId", sectionId);
                cmd.Parameters.AddWithValue("@Day", day);
                cmd.Parameters.AddWithValue("@StartTime", start);
                cmd.Parameters.AddWithValue("@EndTime", end);
                cmd.Parameters.AddWithValue("@MisCode", string.IsNullOrEmpty(misCode) ? "" : misCode);

                var result = (bool)cmd.ExecuteScalar();
                if (result)
                {
                    message = $"Conflict detected in Block Section on {Capitalize(day)} between {start} - {end}";
                    return false;
                }
            }

            return true;
        }
        private bool ValidateCourseInSection(int sectionId, string courseCode, string misCode, NpgsqlConnection conn, out string message)
        {
            message = null;

            using (var cmd = new NpgsqlCommand(@"
        SELECT EXISTS (
            SELECT 1 FROM schedule 
            WHERE blk_sec_id = @BlkSecId AND crs_code = @CrsCode AND mis_code != @MisCode)",
                       conn))
            {
                cmd.Parameters.AddWithValue("@BlkSecId", sectionId);
                cmd.Parameters.AddWithValue("@CrsCode", courseCode);
                cmd.Parameters.AddWithValue("@MisCode", string.IsNullOrEmpty(misCode) ? "" : misCode);

                var result = (bool)cmd.ExecuteScalar();
                if (result)
                {
                    message = $"Course '{courseCode}' is already assigned to this section.";
                    return false;
                }
            }

            return true;
        }
        private bool ValidateProfessor(int profId, string day, TimeSpan start, TimeSpan end, string misCode, NpgsqlConnection conn, out string message)
        {
            message = null;

            using (var cmd = new NpgsqlCommand(@"
                SELECT EXISTS (
                    SELECT 1 FROM schedule
                    WHERE prof_id = @ProfId AND day ILIKE @Day 
                      AND NOT (end_time <= @StartTime OR start_time >= @EndTime)
                      AND mis_code != @MisCode)",
                conn))
            {
                cmd.Parameters.AddWithValue("@ProfId", profId);
                cmd.Parameters.AddWithValue("@Day", day);
                cmd.Parameters.AddWithValue("@StartTime", start);
                cmd.Parameters.AddWithValue("@EndTime", end);
                cmd.Parameters.AddWithValue("@MisCode", string.IsNullOrEmpty(misCode) ? "" : misCode);

                var result = (bool)cmd.ExecuteScalar();
                if (result)
                {
                    message = $"Professor conflict on {Capitalize(day)} between {start} - {end}";
                    return false;
                }
            }

            return true;
        }

        private TimeSpan ParseTime(string timeStr)
        {
            if (TimeSpan.TryParse(timeStr, out var time))
                return time;

            throw new ArgumentException($"Invalid time format: {timeStr}");
        }

        private string Capitalize(string str)
        {
            return char.ToUpper(str[0]) + str.Substring(1);
        }
        [HttpGet]
        public ActionResult GetAllSchedules()
        {
            var schedules = new List<ScheduleViewModel>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(@"
                    SELECT DISTINCT ON (s.mis_code, s.day, s.start_time, s.end_time)
                        s.schedule_id,
                        s.mis_code,
                        s.crs_code,
                        s.day,
                        s.start_time,
                        s.end_time,

                        -- room fields
                        r.room_code,

                        -- professor fields
                        p.prof_name,

                        -- block section fields
                        b.blk_sec_name,

                        -- curriculum_course fields
                        cc.cur_year_level,
                        cc.prog_code,
                        cc.cur_semester
                    FROM schedule s
                    LEFT JOIN room r ON s.room_id = r.room_id
                    LEFT JOIN professor p ON s.prof_id = p.prof_id
                    LEFT JOIN block_section b ON s.blk_sec_id = b.blk_sec_id
                    LEFT JOIN curriculum_course cc ON s.crs_code = cc.crs_code
                    ORDER BY s.mis_code, s.day, s.start_time", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            schedules.Add(new ScheduleViewModel
                            {
                                ScheduleId = reader.GetInt32(reader.GetOrdinal("schedule_id")),
                                MisCode = reader["mis_code"]?.ToString(),
                                Course = reader["crs_code"]?.ToString(),
                                Day = reader["day"]?.ToString(),
                                StartTime = reader["start_time"].ToString().Substring(0, 5),
                                EndTime = reader["end_time"].ToString().Substring(0, 5),

                                // Joined fields
                                RoomName = reader["room_code"]?.ToString(),     // From room table
                                ProfessorName = reader["prof_name"]?.ToString(),// From professor table
                                SectionName = reader["blk_sec_name"]?.ToString(),// From block_section table

                                YearLevel = reader["cur_year_level"]?.ToString(),
                                Program = reader["prog_code"]?.ToString(),
                                Semester = reader["cur_semester"] as int?
                            });
                        }
                    }
                }
            }

            return Json(schedules, JsonRequestBehavior.AllowGet);
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

        // Display these in table
        public string RoomName { get; set; }      // e.g., R101
        public string ProfessorName { get; set; } // e.g., Dr. John Doe
        public string SectionName { get; set; }   // e.g., CS-1A

        public string YearLevel { get; set; }    // "1st Year", "2nd Year"
        public string Program { get; set; }      // "BSCS", "BSIT"
        public int? Semester { get; set; }
    }
    
    

}