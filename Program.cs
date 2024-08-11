using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Globalization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using System.Net.Mime;
using api.contas.Enums;
using api.contas.Database.SALARIES;
using Serilog;
using api.contas.DTOs;
using Serilog.Events;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
if (builder.Environment.EnvironmentName == "Production")
	builder.Configuration.SetBasePath("/app/config");
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

string connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddScoped(x => new OracleConnection(connectionString));
builder.Services.AddHealthChecks().AddOracle(connectionString);
builder.Services.AddSerilog();

Log.Logger = new LoggerConfiguration()
	.Enrich.WithProperty("ApplicationName", "API-Contas")
	.MinimumLevel.Warning()
	.MinimumLevel.Override("Program", LogEventLevel.Information)
	.WriteTo.Oracle(cfg => cfg.WithSettings(connectionString)
		.UseBurstBatch()
		.CreateSink())
	.CreateLogger();

WebApplication app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/healthcheck", new HealthCheckOptions
{
	ResponseWriter = async (context, report) =>
	{
		string appHealth = Enum.GetName(typeof(HealthStatus), HealthStatus.Healthy);

		var dependencies = report.Entries.Select(e =>
		{

			HealthStatus status = e.Value.Status;

			if (status != HealthStatus.Healthy)
				appHealth = Enum.GetName(typeof(HealthStatus), HealthStatus.Unhealthy);

			return new
			{
				key = e.Key,
				value = Enum.GetName(typeof(HealthStatus), e.Value.Status)
			};
		}).ToList();

		var result = JsonConvert.SerializeObject(new
		{
			status = appHealth,
			dependencies
		});
		context.Response.ContentType = MediaTypeNames.Application.Json;
		await context.Response.WriteAsync(result);
	}
});

app.MapPut("/atualizar-salario", (UpdateSalaryDto dto, [FromServices] OracleConnection connection, [FromServices] ILogger<Program> logger) =>
{
	SalariesQueries.UpdateSalary(connection, dto);
	logger.LogInformation("Nova atualizacao de salário de {pessoa} para {valor}", dto.Person, dto.Salary);
});

app.MapGet("/conta", (double valor, [FromServices] OracleConnection connection, [FromServices] ILogger<Program> logger) =>
{
	IEnumerable<SalariesMapper> salaryMapper = SalariesQueries.SelectAll(connection);

	double _salaryB = salaryMapper.Where(w => w.PERSON == Person.Bruno).Select(s => s.SALARY).First();
	double _salaryL = salaryMapper.Where(w => w.PERSON == Person.Leticia).Select(s => s.SALARY).First();

	double porcentagemB = CalculatePercentage(Person.Bruno, _salaryB, _salaryL);
	double porcentageml = CalculatePercentage(Person.Leticia, _salaryB, _salaryL);

	double valorB = CalculateValue(porcentagemB, valor);
	double valorL = CalculateValue(porcentageml, valor);

	CultureInfo customCulture = new("pt-BR");

	string valorFinalB = valorB.ToString("N2", customCulture);
	string valorFinalL = valorL.ToString("N2", customCulture);

	object result = new
	{
		Leticia = valorFinalL,
		Bruno = valorFinalB
	};

	logger.LogInformation("Nova consulta de porcentagem feita, resultado | Leticia: {valorFinalL}, Bruno: {valorFinalB}", valorFinalL, valorFinalB);
	return JsonConvert.SerializeObject(result);
});

app.Run();

static double CalculatePercentage(Person pessoa, double salaryB, double salaryL)
{
	if (pessoa == Person.Bruno)
		return salaryB * 100 / (salaryB + salaryL);
	else
		return salaryL * 100 / (salaryB + salaryL);
}

static double CalculateValue(double porcentagem, double valor)
{
	return porcentagem * valor / 100;
}
