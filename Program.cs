using Microsoft.EntityFrameworkCore;
using StockFlow.Data;


var builder = WebApplication.CreateBuilder(args);



builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));



builder.Services.AddControllersWithViews();



builder.Services.AddSession();



var app = builder.Build();



if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}



app.UseHttpsRedirection();


app.UseStaticFiles();


app.UseRouting();


app.UseSession();



app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);



app.Run();