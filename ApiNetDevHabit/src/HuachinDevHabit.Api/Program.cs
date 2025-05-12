using FluentValidation;
using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.Habits;
using HuachinDevHabit.Api.Entities;
using HuachinDevHabit.Api.Extensions;
using HuachinDevHabit.Api.Middleware;
using HuachinDevHabit.Api.Services.DataShaping;
using HuachinDevHabit.Api.Services.Sorting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Serialization;
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
.AddNewtonsoftJson(options =>
{
	options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
})
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
	//.UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri("http://devhabit.aspire-dashboard:18889"));

builder.Logging.AddOpenTelemetry(options =>
{
	options.IncludeScopes = true;
	options.IncludeFormattedMessage = true;
	options.ParseStateValues = true;
});
#endregion

#region Sorting and Data Shaping
// Sorting
builder.Services.AddTransient<SortMappingProvider>();
builder.Services.AddSingleton<ISortMappingDefinition, SortMappingDefinition<HabitDto, Habit>>(_ =>
	HabitMapping.SortMapping);
// Data Shaping
builder.Services.AddTransient<DataShapingService>();
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
