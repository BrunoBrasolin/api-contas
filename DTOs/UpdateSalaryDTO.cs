using api.contas.Enums;

namespace api.contas.DTOs;

class UpdateSalaryDTO
{
    public Person Person { get; set; }
    public double Salary { get; set; }
}