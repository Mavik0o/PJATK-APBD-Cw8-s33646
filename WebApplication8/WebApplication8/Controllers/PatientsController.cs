using Microsoft.AspNetCore.Mvc;
using WebApplication8.DTOs;
using WebApplication8.Services;

namespace WebApplication8.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPatients([FromQuery] string? search)
    {
        var patients = await _patientService.GetPatientsAsync(search);

        return Ok(patients);
    }

    [HttpPost("{pesel}/bedassignments")]
    public async Task<IActionResult> AssignBedToPatient(
        [FromRoute] string pesel,
        [FromBody] CreateBedAssignmentDto request)
    {
        var result = await _patientService.AssignBedToPatientAsync(pesel, request);

        if (!result.Success)
        {
            if (result.StatusCode == 400)
                return BadRequest(result.Message);

            if (result.StatusCode == 404)
                return NotFound(result.Message);

            return StatusCode(result.StatusCode, result.Message);
        }

        return Created(
            $"/api/patients/{pesel}/bedassignments/{result.AssignmentId}",
            new
            {
                Id = result.AssignmentId,
                PatientPesel = pesel,
                Message = result.Message
            });
    }
}