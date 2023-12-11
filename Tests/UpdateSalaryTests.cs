using Moq;
using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using Dapper;
using api.contas.DTOs;
using api.contas.Enums;
using api.contas.Database.SALARIES;

namespace api.contas.Tests;

public class UpdateSalaryTests
{
    [Test]
    public void TestAtualizarSalarioEndpoint()
    {
        UpdateSalaryDTO dto = new()
        {
            Person = Person.Bruno,
            Salary = 5000.00
        };

        Mock<OracleConnection> mock = new();
        mock.Setup(s => s.Execute(It.IsAny<string>(), It.IsAny<object>(), null, null, null)).Returns(1);

        var result = SalariesQueries.UpdateSalary(mock.Object, dto);

        mock.Verify(v => v.Execute(It.IsAny<string>(), dto, null, null, null), Times.Once);
    }
}