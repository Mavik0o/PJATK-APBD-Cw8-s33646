using WebApplication8.DTOs;
using WebApplication8.Models;

namespace WebApplication8.Services;

public interface IPatientService
{
    Task<List<PatientDto>> GetPatientsAsync(string? search);
    Task<AssignBedResultDto> AssignBedToPatientAsync(string pesel, CreateBedAssignmentDto request);
}