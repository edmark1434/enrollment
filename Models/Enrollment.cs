namespace EnrollmentSystem.Models
{
    using System;
    public class Enrollment
    {
        public int Enrol_Id { get; set; }

        public string Enrol_Status { get; set; }

        public DateTime Enrol_Date { get; set; }

        public string Enrol_Yr_Level { get; set; }

        public string Enrol_Sem { get; set; }

        public int? Stud_Id { get; set; }

        public string Crs_Code { get; set; }

        public string Ay_Code { get; set; }
        
        public string Stud_Fname { get; set; }
        public string Stud_Lname { get; set; }
    }
}