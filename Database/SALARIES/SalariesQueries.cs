using api.contas.DTOs;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace api.contas.Database.SALARIES;

static class SalariesQueries
{
    public static bool UpdateSalary(OracleConnection connection, UpdateSalaryDTO dto)
    {
        connection.Execute("UPDATE SALARIES SET salary = :salary WHERE PERSON = :person", dto);

        return true;
    }

    public static IEnumerable<SalariesMapper> SelectAll(OracleConnection connection)
    {
        IEnumerable<SalariesMapper> result = connection.Query<SalariesMapper>("SELECT * FROM SALARIES");

        return result;
    }
}