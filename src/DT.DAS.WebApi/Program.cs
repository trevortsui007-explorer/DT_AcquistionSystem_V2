using System.Reflection;
using DT.DAS.Application;
using DT.DAS.Infrastructure;
using DT.DAS.WebApi.Middleware;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddHealthChecks();
builder.Services
    .AddDasApplication()
    .AddDasInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

if (app.Configuration.GetValue<bool>("DAS:Hangfire:Enabled") &&
    !string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString(app.Configuration["DAS:Hangfire:ConnectionName"] ?? "BaseDb")))
{
    app.UseHangfireDashboard("/hangfire");
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;
