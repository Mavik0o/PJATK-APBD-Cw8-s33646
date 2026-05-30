using WebApplication8.DTOs;

namespace WebApplication8.Services;

public interface IPatientService
{
    Task<List<PatientDto>> GetPatientsAsync(string? search);
    Task<AssignBedResultDto> AssignBedToPatientAsync(string pesel, CreateBedAssignmentDto request);
}