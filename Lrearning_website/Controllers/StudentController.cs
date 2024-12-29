using ApiFinalProject.Entities;
using ApiFinalProject.persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApiFinalProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext context = context;

        [Authorize(Roles = "Student")]
        [HttpPost("enroll")]
        public async Task<IActionResult> EnrollInCourse([FromBody] int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var student = await context.Students
                            .Include(s => s.StudentCourses)
                            .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (student == null)
            {
                return BadRequest("Student not found.");
            }

            var course = await context.Courses.FindAsync(courseId);
            if (course == null)
            {
                return BadRequest("Course not found.");
            }

            // Check if already enrolled
            if (student.StudentCourses.Any(sc => sc.CourseId == courseId))
            {
                return BadRequest("Student is already enrolled in this course.");
            }

            // Enroll the student
            var studentCourse = new CourseStudent
            {
                StudentId = student.Id,
                CourseId = courseId,
                EnrollmentDate = DateTime.Now
            };

            context.CourseStudents.Add(studentCourse);
            await context.SaveChangesAsync();

            return Ok("Enrollment successful.");
        }

    }
}
