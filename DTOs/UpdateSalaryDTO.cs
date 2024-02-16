using api.contas.Enums;
using System.Text.Json.Serialization;

namespace api.contas.DTOs;

class UpdateSalaryDTO
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
    public Person Person { get; set; }
	public double Salary { get; set; }
}