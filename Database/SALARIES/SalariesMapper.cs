using api.contas.Enums;

namespace api.contas.Database.SALARIES;

class SalariesMapper
{
    public int ID { get; set; }
    public Person PERSON { get; set; }
    public double SALARY { get; set; }
    public DateTime DATETIME_CREATED { get; set; }
}