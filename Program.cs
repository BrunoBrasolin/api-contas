using Dapper;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Globalization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using System.Net.Mime;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
string connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddScoped(x => new OracleConnection(connectionString));
builder.Services.AddHealthChecks().AddOracle(connectionString);

WebApplication app = builder.Build();
app.UseCors(builder => builder.WithOrigins("http://contas.gamidas.dev.br").AllowAnyHeader().AllowAnyMethod());
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

app.MapPost("/api/atualizar-salario", (SalaryHistoryDTO dto, [FromServices] OracleConnection connection) =>
{
    connection.Execute("UPDATE SALARIES SET salary = :salary WHERE PERSON = :person", dto);

});

app.MapGet("/api/conta", (double valor, [FromServices] OracleConnection connection) =>
{
    IEnumerable<SalaryHistoryMapper> salaryHistoryMapper = connection.Query<SalaryHistoryMapper>("SELECT * FROM SALARIES");

    double _salaryB = salaryHistoryMapper.Where(w => w.PERSON == Person.Bruno).Select(s => s.SALARY).First();
    double _salaryL = salaryHistoryMapper.Where(w => w.PERSON == Person.Leticia).Select(s => s.SALARY).First();

    double porcentagemB = CalculatePercentage("Bruno", _salaryB, _salaryL);
    double porcentageml = CalculatePercentage("Letícia", _salaryB, _salaryL);

    double valorB = CalculateValue(porcentagemB, valor);
    double valorL = CalculateValue(porcentageml, valor);

    CultureInfo customCulture = new("pt-BR");

    object result = new
    {
        Leticia = valorL.ToString("N2", customCulture),
        Bruno = valorB.ToString("N2", customCulture)
    };

    return JsonConvert.SerializeObject(result);
});

app.Run();

static double CalculatePercentage(string pessoa, double salaryB, double salaryL)
{
    if (pessoa == "Bruno")
        return salaryB * 100 / (salaryB + salaryL);
    else
        return salaryL * 100 / (salaryB + salaryL);
}

static double CalculateValue(double porcentagem, double valor)
{
    return porcentagem * valor / 100;
}

class SalaryHistoryDTO
{
    public Person Person { get; set; }
    public double Salary { get; set; }
}

class SalaryHistoryMapper
{
    public int ID { get; set; }
    public Person PERSON { get; set; }
    public double SALARY { get; set; }
    public DateTime DATETIME_CREATED { get; set; }
}

enum Person
{
    Leticia = 0,
    Bruno = 1
}