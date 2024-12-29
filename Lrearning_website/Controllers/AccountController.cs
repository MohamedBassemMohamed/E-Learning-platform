using ApiFinalProject.DTO.Account;
using ApiFinalProject.DTO.AuthDTO;
using ApiFinalProject.Entities;
using ApiFinalProject.persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.IO;


namespace ApiFinalProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public AccountController(UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager, ApplicationDbContext context,SignInManager<ApplicationUser>signInManager
            ,IConfiguration configuration,IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            this._signInManager = signInManager;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            //_webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }
        [HttpPost("register")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Register([FromForm] RegisterModel model)
        {
            if (ModelState.IsValid)
            {
               

                // Define folder based on role
                string imageUrl=default!;

                if (model.image != null)
                {
                string folderName = model.Role == "Teacher" ? "teacher-images" : "student-images";

                    if (string.IsNullOrEmpty(_webHostEnvironment.WebRootPath))
                    {
                        return BadRequest("Web root path is not configured.");
                    }

                    if (string.IsNullOrEmpty(folderName))
                    {
                        return BadRequest("Invalid role specified.");
                    }

                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath,folderName);
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder); // Create folder if not exists
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.image.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.image.CopyToAsync(fileStream);
                    }

                    imageUrl = "/" + folderName + "/" + uniqueFileName; // Set the relative image URL
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, PhoneNumber = model.PhoneNumber };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign role based on input
                    string role = model.Role;

                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        return BadRequest("Role does not exist.");
                    }

                    await _userManager.AddToRoleAsync(user, role);

                    if (role == "Teacher")
                    {
                        // Validate the SpecializationId
                        var specialization = await _context.Specializations.FindAsync(model.SpecializationId);
                        if (specialization == null)
                        {
                            return BadRequest("Invalid Specialization ID.");
                        }
                        if (!model.experienceAge.HasValue)
                        {
                            return BadRequest("Experience age is required for teachers.");
                        }
                        var teacher = new Instructor
                        {
                            ExperienceAge=model.experienceAge,
                            ApplicationUserId = user.Id,
                            Name = model.Name,
                            ImageUrl=imageUrl,
                            SpecializationId = model.SpecializationId
                        };

                        _context.Instructors.Add(teacher);
                    }
                    else if (role == "Student")
                    {
                        var student = new Student
                        {
                            ApplicationUserId = user.Id,
                            Name = model.Name,
                            ImageUrl = imageUrl
                            
                        };

                        _context.Students.Add(student);
                    }

                    // Save changes to the database
                    await _context.SaveChangesAsync();

                    return Ok(new { Message = "User registered successfully", Role = role });
                }

                return BadRequest(result.Errors);
            }

            return BadRequest("Invalid model");
        }






        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized();
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        private async Task <string> GenerateJwtToken(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id)
    };

            // Add role claims
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpirationMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
