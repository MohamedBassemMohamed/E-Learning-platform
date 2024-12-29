
using ApiFinalProject.Entities;
using ApiFinalProject.persistence;
using ApiFinalProject.Services.Chapter;
using ApiFinalProject.Services.Course;
using ApiFinalProject.Services.dashbord;
using ApiFinalProject.Services.Specalazation;
using ApiFinalProject.Services.Video;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;

namespace Lrearning_website
{
    // Add a custom operation filter for file handling
    public class SwaggerFileOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParams = context.MethodInfo
                .GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile) || p.ParameterType == typeof(IEnumerable<IFormFile>));

            foreach (var param in fileParams)
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties =
                            {
                                [param.Name] = new OpenApiSchema { Type = "string", Format = "binary" }
                            }
                            }
                        }
                    }
                };
            }
        }
        public class Program
        {

            // Method to ensure roles exist in the database
            static async Task EnsureRolesExist(RoleManager<IdentityRole> roleManager)
            {
                if (!await roleManager.RoleExistsAsync("Student"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Student"));
                }
                if (!await roleManager.RoleExistsAsync("Teacher"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Teacher"));
                }
                // Check if the "Admin" role exists, create if not
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Admin"));
                }
            }
            public static async Task Main(string[] args)
            {
                var builder = WebApplication.CreateBuilder(args);

                // Add services to the container.
                builder.Services.AddDbContext<ApplicationDbContext>(options =>

                options.UseSqlServer(builder.Configuration.GetConnectionString("cs"))
                );
                builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                 .AddEntityFrameworkStores<ApplicationDbContext>();

                //builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
                //    .AddEntityFrameworkStores<ApplicationDbContext>();

                builder.Services.AddControllers();
                builder.Services.AddScoped<IDashbord, DashbordService>();
                builder.Services.AddScoped<IChapterService, ChapterService>();
                builder.Services.AddScoped<ICourseService, CourseService>();
                builder.Services.AddScoped<ISpecalazation, SpecalazationService>();

                builder.Services.AddScoped<IVideoService, VideoService>();
                // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                //allow cors policy
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowAll", builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
                });

                builder.Services.AddSwaggerGen(c =>
                {
                    c.OperationFilter<SwaggerFileOperationFilter>(); // Add custom operation filter for file handling
                });

                // JWT Authentication configuration
                builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                };
            });





                //builder.Services.AddSwaggerGen();

                builder.Services.AddSwaggerGen(swagger =>
                {
                    //This is to generate the Default UI of Swagger Documentation    
                    swagger.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "ASP.NET 5 Web API",
                        Description = " ITI Projrcy"
                    });
                    // To Enable authorization using Swagger (JWT)    
                    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\"",
                    });
                    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                    });
                });
                var app = builder.Build();
                //add two role student and teacher
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    try
                    {
                        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                        await EnsureRolesExist(roleManager);
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions as needed, such as logging
                        Console.WriteLine($"Error creating roles: {ex.Message}");
                    }
                }
                //builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
                //builder.Logging.AddConsole();
                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }
                app.UseDeveloperExceptionPage(); // Enable detailed error logging

                app.UseCors("AllowAll");
                // Enable serving of static files (e.g., for videos)
                app.UseStaticFiles();
                app.UseRouting();
                app.UseAuthorization();
                //app.MapIdentityApi<ApplicationUser>();


                app.MapControllers();

                app.Run();
            }
        }
    }
}
