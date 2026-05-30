namespace WebApplication8.DTOs;

public class PatientDto
{
    public string Pesel { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public int Age { get; set; }
    public string Sex { get; set; } = null!;
    public List<AdmissionDto> Admissions { get; set; } = [];
    public List<BedAssignmentDto> BedAssignments { get; set; } = [];
}