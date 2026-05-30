using Microsoft.EntityFrameworkCore;
using WebApplication8.Data;
using WebApplication8.DTOs;
using WebApplication8.Models;

namespace WebApplication8.Services;

public class PatientService : IPatientService
{
    private readonly HospitalDbContext _context;

    public PatientService(HospitalDbContext context)
    {
        _context = context;
    }

    public async Task<List<PatientDto>> GetPatientsAsync(string? search)
    {
        var query = _context.Patients
            .Include(p => p.Admissions)
                .ThenInclude(a => a.Ward)
            .Include(p => p.BedAssignments)
                .ThenInclude(ba => ba.Bed)
                    .ThenInclude(b => b.BedType)
            .Include(p => p.BedAssignments)
                .ThenInclude(ba => ba.Bed)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.Ward)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";

            query = query.Where(p =>
                EF.Functions.Like(p.FirstName, pattern) ||
                EF.Functions.Like(p.LastName, pattern));
        }

        return await query
            .Select(p => new PatientDto
            {
                Pesel = p.Pesel,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Age = p.Age,
                Sex = p.Sex ? "Male" : "Female",

                Admissions = p.Admissions.Select(a => new AdmissionDto
                {
                    Id = a.Id,
                    AdmissionDate = a.AdmissionDate,
                    DischargeDate = a.DischargeDate,
                    Ward = new WardDto
                    {
                        Id = a.Ward.Id,
                        Name = a.Ward.Name,
                        Description = a.Ward.Description
                    }
                }).ToList(),

                BedAssignments = p.BedAssignments.Select(ba => new BedAssignmentDto
                {
                    Id = ba.Id,
                    From = ba.From,
                    To = ba.To,
                    Bed = new BedDto
                    {
                        Id = ba.Bed.Id,
                        BedType = new BedTypeDto
                        {
                            Id = ba.Bed.BedType.Id,
                            Name = ba.Bed.BedType.Name,
                            Description = ba.Bed.BedType.Description
                        },
                        Room = new RoomDto
                        {
                            Id = ba.Bed.Room.Id,
                            HasTv = ba.Bed.Room.HasTv,
                            Ward = new WardDto
                            {
                                Id = ba.Bed.Room.Ward.Id,
                                Name = ba.Bed.Room.Ward.Name,
                                Description = ba.Bed.Room.Ward.Description
                            }
                        }
                    }
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<AssignBedResultDto> AssignBedToPatientAsync(
        string pesel,
        CreateBedAssignmentDto request)
    {
        if (string.IsNullOrWhiteSpace(pesel))
        {
            return new AssignBedResultDto
            {
                Success = false,
                StatusCode = 400,
                Message = "PESEL pacjenta jest wymagany."
            };
        }

        if (string.IsNullOrWhiteSpace(request.BedType))
        {
            return new AssignBedResultDto
            {
                Success = false,
                StatusCode = 400,
                Message = "Typ łóżka jest wymagany."
            };
        }

        if (string.IsNullOrWhiteSpace(request.Ward))
        {
            return new AssignBedResultDto
            {
                Success = false,
                StatusCode = 400,
                Message = "Nazwa oddziału jest wymagana."
            };
        }

        if (request.To.HasValue && request.To <= request.From)
        {
            return new AssignBedResultDto
            {
                Success = false,
                StatusCode = 400,
                Message = "Data końcowa przypisania łóżka musi być późniejsza niż data początkowa."
            };
        }

        var patientExists = await _context.Patients
            .AnyAsync(p => p.Pesel == pesel);

        if (!patientExists)
        {
            return new AssignBedResultDto
            {
                Success = false,
                StatusCode = 404,
                Message = $"Nie znaleziono pacjenta o numerze PESEL: {pesel}."
            };
        }

        var wardExists = await _context.Wards
            .AnyAsync(w => w.Name == request.Ward);

        if (!wardExists)
        {
            return new AssignBedResultDto
            {
                Success = false,
                StatusCode = 404,
                Message = $"Nie znaleziono oddziału o nazwie: {request.Ward}."
            };
        }

        var bedTypeExists = await _context.BedTypes
            .AnyAsync(bt => bt.Name == request.BedType);

        if (!bedTypeExists)
        {
            return new AssignBedResultDto
            {
                Success = false,
                StatusCode = 404,
                Message = $"Nie znaleziono typu łóżka o nazwie: {request.BedType}."
            };
        }

        var requestedFrom = request.From;
        var requestedTo = request.To ?? DateTime.MaxValue;

        var freeBed = await _context.Beds
            .Include(b => b.Room)
                .ThenInclude(r => r.Ward)
            .Include(b => b.BedType)
            .Include(b => b.BedAssignments)
            .Where(b =>
                b.BedType.Name == request.BedType &&
                b.Room.Ward.Name == request.Ward)
            .Where(b => !b.BedAssignments.Any(ba =>
                ba.From < requestedTo &&
                (ba.To == null || ba.To > requestedFrom)))
            .FirstOrDefaultAsync();

        if (freeBed == null)
        {
            return new AssignBedResultDto
            {
                Success = false,
                StatusCode = 404,
                Message = $"Brak wolnego łóżka typu '{request.BedType}' na oddziale '{request.Ward}' w podanym okresie."
            };
        }

        var assignment = new BedAssignment
        {
            PatientPesel = pesel,
            BedId = freeBed.Id,
            From = request.From,
            To = request.To
        };

        _context.BedAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        return new AssignBedResultDto
        {
            Success = true,
            StatusCode = 201,
            AssignmentId = assignment.Id,
            Message = "Pacjent został przypisany do łóżka."
        };
    }
}