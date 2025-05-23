using HuachinDevHabit.Api;
using HuachinDevHabit.Api.Extensions;
using HuachinDevHabit.Api.Middleware;
using HuachinDevHabit.Api.Settings;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
	.AddApiServices()
	.AddErrorHandling()
	.AddDatabase()
	.AddSwagger()
	.AddObservability()
	.AddAuthenticationServices()
	.AddCorsPolicy();

builder.AddApplicationServices();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

	await app.ApplyMigrationsAsync();

	await app.SeedInitialDataAsync();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseCors(CorsOptions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ETagMiddleware>();

app.MapControllers();

await app.RunAsync();
