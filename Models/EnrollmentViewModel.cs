using System;

namespace EnrollmentSystem.Models;

public class EnrollmentViewModel
{
    public string MisCode { get; set; }
    public string CrsCode { get; set; }
    public string CourseName { get; set; }
    public string EnrollStatus { get; set; }
    public string EnrollYrLevel { get; set; }
    public string AyCode { get; set; }
    public int SemId { get; set; }
    public int StudCode { get; set; }
    public string Section { get; set; }
    public string StudName { get; set; }
    public string EnrollDate { get; set; }
    public string ProgCode { get; set; }
}