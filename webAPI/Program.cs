
using Microsoft.AspNetCore.Builder;
using System.Security.Cryptography.X509Certificates;

namespace webAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            //MINIMAL API 


            /*app.MapGet("/", () =>
            {
                return "Welcome to Minimal API ";
            });*/
            /*app.MapGet("/employee", () =>
            {
                return new
                {
                    Id = 101,
                    Name = "Anubhav Gargmukh",
                    salary = 993453485734
                };
            });

            app.MapGet("/employee/{id}", (int id) =>
            {
                return $"Employee Id = {id}";
                
            });*/


            //Employee Model



            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            List<Employee> employees = new List<Employee>()
            {
                new Employee() { Id = 1, Name = "Anubhav Gargmukh", Salary = 3939839 },
                new Employee() { Id = 2, Name = "Rana saksham Singh", Salary = 95885999999939839 },
                new Employee() { Id = 3, Name = "Prashant Lawaniya", Salary = 9888998488943483867}
                };


            app.MapGet("/emps", () =>
            {
                return Results.Ok(employees);
            });

            app.MapGet("/employees/{id}", (int id) =>
            {
                var emp = employees.FirstOrDefault(e => e.Id == id);
                if (emp == null)
                {
                    return Results.NotFound("employees not Found");
                }
                return Results.Ok(emp);
            });

            app.MapPut("employees/{id}", (int id, Employee updatedEmployee) =>
            {
                //search employee by ID
                var employee = employees.FirstOrDefault(e => e.Id == id);
                if (employee == null)
                {
                    return Results.NotFound("Employee Not Found");
                }
                //uppdate employee
                employee.Name = updatedEmployee.Name;
                employee.Salary = updatedEmployee.Salary;
                return Results.Ok(employee);
            });

            app.MapDelete("/employees/{id}", (int id) =>
            {
                var employee = employees.FirstOrDefault(e => e.Id == id);
                if (employee == null)
                {
                    return Results.NotFound("Employee Not Found");
                }
                employees.Remove(employee);
                return Results.Ok("EMPLOYEE DELETED SUCCESSFULLY");

            });
            


            app.UseHttpsRedirection();

            app.UseAuthorization();

            var summaries = new[]
            {
                "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            };

            app.MapGet("/weatherforecast", (HttpContext httpContext) =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = summaries[Random.Shared.Next(summaries.Length)]
                    })
                    .ToArray();
                return forecast;
            })
            .WithName("GetWeatherForecast")
            .WithOpenApi();

            app.Run();
        }

             public class Employee
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Salary { get; set; }

        }
    }
    
}
