using Dapper;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Globalization;
using System.Text.Json;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped(x => new OracleConnection(builder.Configuration.GetConnectionString("Default")));

WebApplication app = builder.Build();
app.UseCors(builder => builder.WithOrigins("http://contas.gamidas.dev.br").AllowAnyHeader().AllowAnyMethod());
app.UseSwagger();
app.UseSwaggerUI();

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

    return JsonSerializer.Serialize(result);
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