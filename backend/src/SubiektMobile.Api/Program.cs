using SubiektMobile.Application;
using SubiektMobile.Infrastructure;
using SubiektMobile.Infrastructure.Persistence;
using SubiektMobile.Infrastructure.Persistence.Application;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<SubiektDbContext>("subiekt-db")
    .AddDbContextCheck<ApplicationDbContext>("application-db");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Subiekt Mobile API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Subiekt Mobile API";
    });
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");
app.UseCors("Frontend");

app.MapControllers();

app.Run();
