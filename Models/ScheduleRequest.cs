using System.Collections.Generic;

public class ScheduleRequest
{
    public string MisCode { get; set; }
    public string Course { get; set; } // CRS_CODE
    public int ProfId { get; set; }   // 0 = TBA
    public int RoomId { get; set; }
    public int Section { get; set; }  // BLK_SEC_ID
    public List<ScheduleDetail> ScheduleDetails { get; set; } = new();
}

public class ScheduleDetail
{
    public string Day { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }
}