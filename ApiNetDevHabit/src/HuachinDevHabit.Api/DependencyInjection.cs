using Asp.Versioning;
using FluentValidation;
using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.Habits;
using HuachinDevHabit.Api.Entities;
using HuachinDevHabit.Api.Middleware;
using HuachinDevHabit.Api.Services.ContentNegotiation;
using HuachinDevHabit.Api.Services.DataShaping;
using HuachinDevHabit.Api.Services.Hateos;
using HuachinDevHabit.Api.Services.Sorting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Serialization;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HuachinDevHabit.Api
{
	public static class DependencyInjection
	{
		public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
		{
			#region Configuración de Controladores
			builder.Services.AddControllers(options =>
				{
					options.ReturnHttpNotAcceptable = true;
				})
				.AddNewtonsoftJson(options =>
				{
					options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
				})
				.AddXmlSerializerFormatters();
			#endregion

			#region Configuración de Serializadores: Agregar el nuevo Media Type para HATEOS
			builder.Services.Configure<MvcOptions>(options =>
			{
				NewtonsoftJsonOutputFormatter formatter = options.OutputFormatters
					.OfType<NewtonsoftJsonOutputFormatter>()
					.First();

				// Esto agrega Media Type application/vnd.dev-habit.hateos+json de forma global
				formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.JsonV1);
				formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.JsonV2);
				formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.HateosJson);
				formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.HateosJsonV1);
				formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.HateosJsonV2);
			});
			#endregion

			#region Configuración de Versionado de API
			builder.Services
				.AddApiVersioning(options =>
				{
					options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1.0);
					options.AssumeDefaultVersionWhenUnspecified = true;
					options.ReportApiVersions = true;
					//options.ApiVersionSelector = new CurrentImplementationApiVersionSelector(options);
					options.ApiVersionSelector = new DefaultApiVersionSelector(options);

					//options.ApiVersionReader = new UrlSegmentApiVersionReader();
					options.ApiVersionReader = ApiVersionReader.Combine(
						new MediaTypeApiVersionReader("application/vnd.dev-habit.hateos.{version}+json"),
						new MediaTypeApiVersionReaderBuilder()
							.Template("")
							.Build());
				})
				.AddMvc();
			#endregion

			return builder;
		}

		public static WebApplicationBuilder AddSwagger(this WebApplicationBuilder builder)
		{
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			return builder;
		}

		public static WebApplicationBuilder AddErrorHandling(this WebApplicationBuilder builder)
		{
			builder.Services.AddProblemDetails(options =>
			{
				options.CustomizeProblemDetails = context =>
				{
					context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
				};
			});

			builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
			builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

			return builder;
		}

		public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
		{
			builder.Services.AddDbContext<ApplicationDbContext>(options =>
			{
				options
					.UseNpgsql(
						builder.Configuration.GetConnectionString("Database"),
						npgsqlOptions => npgsqlOptions
							.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Application))
					.UseSnakeCaseNamingConvention();
			});

			return builder;
		}

		public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
		{
			// Configuration OpenTelemetry
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

			return builder;
		}

		public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
		{
			#region FluentValidation
			//builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);
			builder.Services.AddValidatorsFromAssemblyContaining<Program>();
			#endregion

			#region Sorting and Data Shaping
			// Sorting
			builder.Services.AddTransient<SortMappingProvider>();
			builder.Services.AddSingleton<ISortMappingDefinition, SortMappingDefinition<HabitDto, Habit>>(_ =>
				HabitMapping.SortMapping);
			// Data Shaping
			builder.Services.AddTransient<DataShapingService>();
			#endregion

			// Registramos AddHttpContextAccessor para poder usar IHttpContextAccessor en el servicio de HATEOS
			builder.Services.AddHttpContextAccessor();

			#region HATEOS
			builder.Services.AddTransient<LinkService>();
			#endregion

			return builder;
		}
	}
}
