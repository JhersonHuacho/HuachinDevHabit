using HuachinDevHabit.Api;
using HuachinDevHabit.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
	.AddApiServices()
	.AddErrorHandling()
	.AddDatabase()
	.AddSwagger()
	.AddObservability()
	.AddAuthenticationServices();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
