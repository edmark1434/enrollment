namespace EnrollmentSystem.Models
{
    public class Prerequisite
    {
        public string Crs_Code { get; set; }         // Foreign Key to Course
        public string Preq_Crs_Code { get; set; } 
    }
}