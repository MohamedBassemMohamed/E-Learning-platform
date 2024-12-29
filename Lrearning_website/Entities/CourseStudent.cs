﻿namespace ApiFinalProject.Entities;

public class CourseStudent
{

    public int? StudentId { get; set; }
    public Student Student { get; set; } = default!;
    public int? CourseId { get; set; }
    public Course Course { get; set; } = default!;
    public DateTime EnrollmentDate { get; set; }


}
