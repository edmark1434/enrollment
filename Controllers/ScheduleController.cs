using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Mvc;
using EnrollmentSystem.Models;
using Npgsql;

namespace Fresh_University_Enrollment.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["Enrollment"].ConnectionString;

        public ActionResult Index()
        {
            var courseList = GetCourseFromDatabase();
            var scheduleList = GetScheduleFromDatabase();
            var sessionList = GetSessionFromDatabase();

            ViewBag.Course = courseList;
            ViewBag.Schedules = scheduleList;
            ViewBag.Sessions = sessionList;

            return View("~/Views/Program-Head/SetSchedules.cshtml");
        }

        // ✅ Load courses from database
        private List<Course> GetCourseFromDatabase()
        {
            var courseList = new List<Course>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT \"crs_code\", \"crs_title\" FROM \"course\"", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            courseList.Add(new Course
                            {
                                Crs_Code = reader.GetString(0),
                                Crs_Title = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            return courseList;
        }

        // ✅ Load schedules with descriptions (time/day)
        private List<Schedule> GetScheduleFromDatabase()
        {
            var schedList = new List<Schedule>();
            var sessionMap = new Dictionary<int, List<Session>>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();

                // Load all sessions and group by schd_id
                using (var sessionCmd = new NpgsqlCommand("SELECT session_id, tsl_start_time, tsl_end_time, tsl_day, schd_id FROM session", conn))
                using (var sessionReader = sessionCmd.ExecuteReader())
                {
                    while (sessionReader.Read())
                    {
                        var session = new Session
                        {
                            SsnId = sessionReader.GetInt32(0),
                            TslStartTime = sessionReader.GetTimeSpan(1),
                            TslEndTime = sessionReader.GetTimeSpan(2),
                            TslDay = sessionReader.GetInt32(3),
                            SchdId = sessionReader.GetInt32(4)
                        };

                        if (!sessionMap.ContainsKey(session.SchdId))
                            sessionMap[session.SchdId] = new List<Session>();

                        sessionMap[session.SchdId].Add(session);
                    }
                }

                // Load schedules with generated descriptions
                using (var cmd = new NpgsqlCommand("SELECT schd_id, crs_code, room, prof FROM schedule", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var schdId = reader.GetInt32(0);
                        var crsCode = reader.GetString(1);
                        var room = reader.GetString(2);
                        var prof = reader.IsDBNull(3) ? null : reader.GetString(3);

                        List<Session>? sessions;
                        if (sessionMap.TryGetValue(schdId, out sessions))
                        {
                            var description = GenerateScheduleDescription(sessions);
                            schedList.Add(new Schedule
                            {
                                SchdId = schdId,
                                CrsCode = crsCode,
                                Room = room,
                                Prof = prof,
                                Description = description
                            });
                        }
                        else
                        {
                            schedList.Add(new Schedule
                            {
                                SchdId = schdId,
                                CrsCode = crsCode,
                                Room = room,
                                Prof = prof,
                                Description = "No sessions assigned yet"
                            });
                        }
                    }
                }
            }

            return schedList;
        }

        // ✅ Generate readable schedule description
        private string GenerateScheduleDescription(List<Session> sessions)
        {
            if (sessions == null || sessions.Count == 0)
                return "No sessions assigned yet";

            var descriptions = new List<string>();

            foreach (var s in sessions)
            {
                var day = s.TslDay switch
                {
                    0 => "Sun",
                    1 => "Mon",
                    2 => "Tue",
                    3 => "Wed",
                    4 => "Thu",
                    5 => "Fri",
                    6 => "Sat",
                    _ => "?"
                };

                var start = s.TslStartTime.ToString(@"hh\:mm");
                var end = s.TslEndTime.ToString(@"hh\:mm");

                descriptions.Add($"{day} {start}-{end}");
            }

            return string.Join("<br/>", descriptions);
        }

        // ✅ Load all sessions
        private List<Session> GetSessionFromDatabase()
        {
            var sessionList = new List<Session>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT * FROM session", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sessionList.Add(new Session
                            {
                                SsnId = reader.GetInt32(0),
                                TslStartTime = reader.GetTimeSpan(1),
                                TslEndTime = reader.GetTimeSpan(2),
                                TslDay = reader.GetInt32(3),
                                SchdId = reader.GetInt32(4)
                            });
                        }
                    }
                }
            }

            return sessionList;
        }

        [HttpPost]
        public ActionResult Add(Schedule sched)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid input data." });
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                
                // 🔍 Check if a schedule with the same Room and Prof already exists
                using var checkCmd = new NpgsqlCommand(
                    @"SELECT COUNT(*) FROM schedule WHERE room = @room AND prof = @prof", conn);
                checkCmd.Parameters.AddWithValue("room", sched.Room.Trim());
                checkCmd.Parameters.AddWithValue("prof", sched.Prof?.Trim() ?? (object)DBNull.Value);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count > 0)
                {
                    return Json(new { success = false, message = "A schedule with the same Room and Professor already exists." });
                }

                // ✅ If no duplicate found, proceed with insertion
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO schedule (
                        crs_code, room, prof
                    ) VALUES (
                        @crsCode, @room, @prof
                    ) RETURNING schd_id";
                insertCmd.Parameters.AddWithValue("crsCode", sched.CrsCode.Trim());
                insertCmd.Parameters.AddWithValue("room", sched.Room.Trim());
                if (string.IsNullOrEmpty(sched.Prof))
                {
                    insertCmd.Parameters.AddWithValue("prof", DBNull.Value);
                }
                else
                {
                    insertCmd.Parameters.AddWithValue("prof", sched.Prof.Trim());
                }
                var newId = (int)(insertCmd.ExecuteScalar())!;
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        Id = newId,
                        CrsCode = sched.CrsCode.Trim(),
                        Room = sched.Room.Trim(),
                        Prof = sched.Prof?.Trim()
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Unexpected error", error = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult AddSession(Session session)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid input data." });

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

                // 🔍 Get the associated schedule to know which room we're dealing with
                using var getScheduleCmd = new NpgsqlCommand(
                    "SELECT room FROM schedule WHERE schd_id = @schdId", conn);
                getScheduleCmd.Parameters.AddWithValue("schdId", session.SchdId);
                object? result = getScheduleCmd.ExecuteScalar();

                if (result == null)
                {
                    return Json(new { success = false, message = "Invalid schedule ID. Schedule not found." });
                }

                string room = result.ToString()!;

                // 🔍 Check for overlapping sessions in the same room and same day
                using var checkOverlapCmd = new NpgsqlCommand(@"
                    SELECT COUNT(*) FROM session s
                    JOIN schedule sch ON s.schd_id = sch.schd_id
                    WHERE sch.room = @room
                      AND s.tsl_day = @day
                      AND (
                          (s.tsl_start_time < @endTime AND s.tsl_end_time > @startTime)
                      )", conn);

                checkOverlapCmd.Parameters.AddWithValue("room", room);
                checkOverlapCmd.Parameters.AddWithValue("day", session.TslDay);
                checkOverlapCmd.Parameters.AddWithValue("startTime", session.TslStartTime);
                checkOverlapCmd.Parameters.AddWithValue("endTime", session.TslEndTime);

                int overlapCount = Convert.ToInt32(checkOverlapCmd.ExecuteScalar());

                if (overlapCount > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Conflict in '{room}' on day {session.TslDay} between {session.TslStartTime} and {session.TslEndTime}.",
                        debug = new
                        {
                            room,
                            day = session.TslDay,
                            start = session.TslStartTime,
                            end = session.TslEndTime
                        }
                    });
                }

                // ✅ No overlap found, proceed to insert session
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO session (
                        tsl_start_time, tsl_end_time, tsl_day, schd_id
                    ) VALUES (
                        @tslStartTime, @tslEndTime, @tslDay, @schdId
                    ) RETURNING session_id";

                insertCmd.Parameters.AddWithValue("tslStartTime", session.TslStartTime);
                insertCmd.Parameters.AddWithValue("tslEndTime", session.TslEndTime);
                insertCmd.Parameters.AddWithValue("tslDay", session.TslDay);
                insertCmd.Parameters.AddWithValue("schdId", session.SchdId);

                var newId = (int)insertCmd.ExecuteScalar()!;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        Id = newId,
                        TslStartTime = session.TslStartTime,
                        TslEndTime = session.TslEndTime,
                        TslDay = session.TslDay,
                        SchdId = session.SchdId
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Unexpected error", error = ex.Message });
            }
        }

        // ✅ Delete a schedule (and its sessions via cascade)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteSchedule(int schdId)
        {
            if (schdId <= 0)
            {
                return Json(new { success = false, message = "Invalid schedule ID." });
            }

            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Check if schedule exists
                            int count;
                            using (var checkCmd = new NpgsqlCommand(
                                @"SELECT COUNT(*) FROM ""schedule"" WHERE ""schd_id"" = @id", conn, transaction))
                            {
                                checkCmd.Parameters.AddWithValue("id", schdId);
                                count = Convert.ToInt32(checkCmd.ExecuteScalar());
                            }

                            if (count == 0)
                            {
                                transaction.Rollback();
                                return Json(new
                                {
                                    success = false,
                                    message = $"Schedule '{schdId}' not found."
                                });
                            }

                            // Delete schedule (sessions are auto-deleted due to ON DELETE CASCADE)
                            using (var deleteCmd = new NpgsqlCommand(
                                @"DELETE FROM ""schedule"" WHERE ""schd_id"" = @id", conn, transaction))
                            {
                                deleteCmd.Parameters.AddWithValue("id", schdId);
                                deleteCmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            return Json(new
                            {
                                success = true,
                                message = $"Schedule '{schdId}' deleted successfully."
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Json(new
                            {
                                success = false,
                                message = "Error deleting schedule.",
                                error = ex.Message
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Unexpected error occurred.",
                    error = ex.Message
                });
            }
        }
    }
}