using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicineExpiration.Api.Data;
using MedicineExpiration.Api.Models;
using MedicineExpiration.Api.Services;
using System.Security.Claims;

namespace MedicineExpiration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicinesController(AppDbContext db, IDrugDatabaseService drugDb) : ControllerBase
{
    private string UserOid => User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var medicines = await db.Medicines
            .Where(m => m.UserOid == UserOid)
            .OrderBy(m => m.ExpireDate)
            .Select(m => new MedicineDto(m))
            .ToListAsync();

        return Ok(medicines);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var medicine = await db.Medicines.FirstOrDefaultAsync(m => m.Id == id && m.UserOid == UserOid);
        return medicine is null ? NotFound() : Ok(new MedicineDto(medicine));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertMedicineRequest request)
    {
        var medicine = new Medicine
        {
            UserOid = UserOid,
            Barcode = request.Barcode,
            Name = request.Name,
            ExpireDate = request.ExpireDate,
            Category = request.Category,
            Notes = request.Notes,
            NotifyDaysBefore = request.NotifyDaysBefore
        };

        db.Medicines.Add(medicine);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = medicine.Id }, new MedicineDto(medicine));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertMedicineRequest request)
    {
        var medicine = await db.Medicines.FirstOrDefaultAsync(m => m.Id == id && m.UserOid == UserOid);
        if (medicine is null) return NotFound();

        medicine.Barcode = request.Barcode;
        medicine.Name = request.Name;
        medicine.ExpireDate = request.ExpireDate;
        medicine.Category = request.Category;
        medicine.Notes = request.Notes;
        medicine.NotifyDaysBefore = request.NotifyDaysBefore;

        await db.SaveChangesAsync();
        return Ok(new MedicineDto(medicine));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var medicine = await db.Medicines.FirstOrDefaultAsync(m => m.Id == id && m.UserOid == UserOid);
        if (medicine is null) return NotFound();

        db.Medicines.Remove(medicine);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("lookup")]
    public async Task<IActionResult> Lookup([FromBody] LookupRequest request)
    {
        var info = await drugDb.LookupByBarcodeAsync(request.Barcode);
        return info is null ? NoContent() : Ok(info);
    }
}

public record MedicineDto(
    int Id, string? Barcode, string Name, DateOnly ExpireDate,
    DateTime AddedDate, string? Category, string? Notes, int NotifyDaysBefore,
    int DaysUntilExpiry)
{
    public MedicineDto(Medicine m) : this(
        m.Id, m.Barcode, m.Name, m.ExpireDate, m.AddedDate,
        m.Category, m.Notes, m.NotifyDaysBefore,
        (m.ExpireDate.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days) { }
}

public record UpsertMedicineRequest(
    string? Barcode,
    string Name,
    DateOnly ExpireDate,
    string? Category,
    string? Notes,
    int NotifyDaysBefore = 7);

public record LookupRequest(string Barcode);
