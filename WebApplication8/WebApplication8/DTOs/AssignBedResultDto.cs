namespace WebApplication8.DTOs;

public class AssignBedResultDto
{
    public bool Success { get; set; }
    public int? AssignmentId { get; set; }
    public string? Message { get; set; }
    public int StatusCode { get; set; }
}