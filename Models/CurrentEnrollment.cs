namespace EnrollmentSystem.Models;

using System;
public class CurrentEnrollment
{
    public int enrollment_season_id {get; set;}
    public string ay_code {get; set;}
    public int sem_id {get; set;}
    public bool is_open {get; set;}
    public CurrentEnrollment() { }
}