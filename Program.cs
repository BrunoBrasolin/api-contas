using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Reflection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient(x => new MySqlConnection(builder.Configuration.GetConnectionString("Default")));

WebApplication app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/api/cadastrar", (SalaryHistoryDTO dto, [FromServices] MySqlConnection connection) =>
{
    connection.Execute("INSERT INTO SALARY_HISTORY (person, salary) VALUES (@person, @salary)", dto);

});

app.MapGet("/api/conta", (double valor, [FromServices] MySqlConnection connection) =>
{
    string query = @"SELECT BRUNO.*
                        FROM (
                            SELECT *
                            FROM SALARY_HISTORY
                            WHERE person = 0
                            ORDER BY datetime_created DESC
                            LIMIT 1
                        ) BRUNO
                    UNION ALL
                    SELECT LETICIA.*
                        FROM (
                            SELECT *
                            FROM SALARY_HISTORY
                            WHERE person = 1
                            ORDER BY datetime_created DESC
                            LIMIT 1
                        ) LETICIA;";

    IEnumerable<SalaryHistoryMapper> salaryHistoryMapper = connection.Query<SalaryHistoryMapper>(query);

    double _salaryB = salaryHistoryMapper.Where(w => w.PERSON == Person.Bruno).Select(s => s.SALARY).First();
    double _salaryL = salaryHistoryMapper.Where(w => w.PERSON == Person.Leticia).Select(s => s.SALARY).First();

    double porcentagemB = CalculatePercentage("Bruno", _salaryB, _salaryL);
    double porcentageml = CalculatePercentage("Letícia", _salaryB, _salaryL);

    double valorB = CalculageValue(porcentagemB, valor);
    double valorL = CalculageValue(porcentageml, valor);

    return $"Bruno: {Double.Round(valorB, 2)} | Letícia: {Double.Round(valorL, 2)}";

});

app.Run();

static double CalculatePercentage(string pessoa, double salaryB, double salaryL)
{
    if (pessoa == "Bruno")
        return salaryB * 100 / (salaryB + salaryL);
    else
        return salaryL * 100 / (salaryB + salaryL);
}

static double CalculageValue(double porcentagem, double valor)
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