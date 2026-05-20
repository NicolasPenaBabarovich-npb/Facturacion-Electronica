using Facturacion_Electronica.Data;
using Facturacion_Electronica.Data.FelRepository;
using Facturacion_Electronica.MiddleWare;
using Facturacion_Electronica.Services.Gsuite;
using Facturacion_Electronica.Services.IConstruye;
using Facturacion_Electronica.Services.Token;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



var conString = builder.Configuration.GetConnectionString("ConnectionFEL");
builder.Services.AddDbContext<FELDbContext>(options =>
{
    options.UseSqlServer(conString);
    options.EnableSensitiveDataLogging();

});

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("CorsRule", rule =>
    {
        rule.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
    });
});


//---------------Interfaces
//---------------Interfaces
builder.Services.AddScoped<IFelRepository, FelRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IIConstruye, IConstruye>();
builder.Services.AddScoped<IGsuite, Gsuite>();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(
              options =>
              {
                  options.RequireHttpsMetadata = false;
                  options.SaveToken = true;
                  options.TokenValidationParameters = new TokenValidationParameters
                  {
                      ValidateIssuerSigningKey = true,
                      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Token:Key"])),
                      ValidIssuer = builder.Configuration["Token:Issuer"],
                      ValidateIssuer = true,
                      ValidateAudience = false
                  };
              });

// Agregar sesiones (antes de builder.Build())
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews(); // asegurarse de tener esto

var app = builder.Build();

// Agregar middleware de sesión (antes de app.MapControllers)
app.UseSession();
app.UseStaticFiles();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=AdminLogin}/{action=Login}/{id?}");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseStatusCodePagesWithReExecute("/errors", "?code={0}");

app.UseMiddleware<ExceptionMiddleWare>();


app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("CorsRule");


//---------------Es importante el orden para token
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();


var supportedCultures = new[]
{
 new CultureInfo("en-CL"),
 
};
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-CL"),
    // Formatting numbers, dates, etc.
    SupportedCultures = supportedCultures,
    // UI strings that we have localized.
    SupportedUICultures = supportedCultures
});


app.Run();
