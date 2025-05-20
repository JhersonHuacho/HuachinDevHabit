using Asp.Versioning;
using FluentValidation;
using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.Habits;
using HuachinDevHabit.Api.Entities;
using HuachinDevHabit.Api.Middleware;
using HuachinDevHabit.Api.Services.Authentication;
using HuachinDevHabit.Api.Services.ContentNegotiation;
using HuachinDevHabit.Api.Services.DataShaping;
using HuachinDevHabit.Api.Services.Encryption;
using HuachinDevHabit.Api.Services.GitHub;
using HuachinDevHabit.Api.Services.Hateos;
using HuachinDevHabit.Api.Services.Sorting;
using HuachinDevHabit.Api.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Net.Http.Headers;
using System.Text;

namespace HuachinDevHabit.Api;

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
			formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.HateoasJson);
			formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.HateoasJsonV1);
			formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.HateoasJsonV2);
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
					new MediaTypeApiVersionReader(),
					new MediaTypeApiVersionReaderBuilder()
						.Template("application/vnd.dev-habit.hateos.{version}+json")
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

		builder.Services.AddDbContext<ApplicationIdentityDbContext>(options =>
		{
			options
				.UseNpgsql(
					builder.Configuration.GetConnectionString("Database"),
					npgsqlOptions => npgsqlOptions
						.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Identity))
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

		#region Authentication
		builder.Services.AddTransient<TokenProvider>();
		#endregion

		#region UserContext
		builder.Services.AddMemoryCache();
		builder.Services.AddTransient<UserContext>();
		#endregion

		#region Github
		builder.Services.AddScoped<GitHubAccessTokenService>();
		builder.Services.AddTransient<GitHubService>();
		builder.Services
			.AddHttpClient("github")
			.ConfigureHttpClient(client =>
			{
				client.BaseAddress = new Uri("https://api.github.com");

				client.DefaultRequestHeaders
					.UserAgent.Add(new ProductInfoHeaderValue("DevHabit", "1.0"));

				client.DefaultRequestHeaders
					.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
			});
		#endregion

		#region Cifrado
		builder.Services.Configure<EncryptionOptions>(builder.Configuration.GetSection("Encryption"));
		builder.Services.AddTransient<EncryptionService>();
		#endregion

		return builder;
	}

	public static WebApplicationBuilder AddAuthenticationServices(this WebApplicationBuilder builder)
	{
		// Este método AddAuthenticationServices se encargara de configurar los servicios de autenticación y autorización
		builder.Services
			.AddIdentity<IdentityUser, IdentityRole>()
			.AddEntityFrameworkStores<ApplicationIdentityDbContext>();

		builder.Services.Configure<JwtAuthOptions>(builder.Configuration.GetSection("Jwt"));

		JwtAuthOptions jwtAuthOptions = builder.Configuration
			.GetSection("Jwt")
			.Get<JwtAuthOptions>()!;

		builder.Services
			.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				options.TokenValidationParameters = new TokenValidationParameters
				{					
					ValidIssuer = jwtAuthOptions.Issuer,
					ValidAudience = jwtAuthOptions.Audience,
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAuthOptions.Key)),
					//LifetimeValidator = (notBefore, expires, token, parameters) => expires > DateTime.UtcNow
				};
			});

		builder.Services.AddAuthorization();

		return builder;
	}

	public static WebApplicationBuilder AddCorsPolicy(this WebApplicationBuilder builder)
	{
		Settings.CorsOptions corsOptions = builder.Configuration
			.GetSection(Settings.CorsOptions.SectionName)
			.Get<Settings.CorsOptions>()!;

		builder.Services.AddCors(options =>
		{
			options.AddPolicy(Settings.CorsOptions.PolicyName, policy =>
			{
				policy
					.WithOrigins(corsOptions.AllowedOrigins)
					.AllowAnyHeader()
					.AllowAnyMethod();
			});
		});

		return builder;
	}
}
