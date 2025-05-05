using FluentValidation;
using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.Extensions;
using HuachinDevHabit.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => 
{
	options.ReturnHttpNotAcceptable = true;
})
.AddNewtonsoftJson()
.AddXmlSerializerFormatters();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region FluentValidation
//builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
#endregion

builder.Services.AddProblemDetails(options =>
{
	options.CustomizeProblemDetails = context =>
	{
		context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
	};
});

builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

#region Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
	options
		.UseNpgsql(
			builder.Configuration.GetConnectionString("Database"),
			npgsqlOptions => npgsqlOptions
				.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Application))
		.UseSnakeCaseNamingConvention();
});
#endregion

#region Configuration OpenTelemetry
builder.Services.AddOpenTelemetry()
	.ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
	.WithTracing(tracing => tracing
		.AddHttpClientInstrumentation()
		.AddAspNetCoreInstrumentation()
		.AddNpgsql())
	.WithMetrics(metrics => metrics
		.AddHttpClientInstrumentation()
		.AddAspNetCoreInstrumentation()
		.AddRuntimeInstrumentation())
	.UseOtlpExporter();

builder.Logging.AddOpenTelemetry(options =>
{
	options.IncludeScopes = true;
	options.IncludeFormattedMessage = true;
});
#endregion

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

	await app.ApplyMigrationsAsync();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.MapControllers();

await app.RunAsync();
