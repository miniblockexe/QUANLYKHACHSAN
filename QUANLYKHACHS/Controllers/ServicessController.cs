using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUANLYKHACHSAN.Data;
using QUANLYKHACHSAN.Models;

namespace QUANLYKHACHS.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ServicessController : ControllerBase
{
    private readonly HotelContext _context;

    public ServicessController(HotelContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Servicess>>> GetAll()
    {
        var services = await _context.Servicesses.ToListAsync();
        return Ok(services);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Servicess>> GetById(int id)
    {
        var service = await _context.Servicesses.FindAsync(id);
        if (service == null)
        {
            return NotFound();
        }
        return Ok(service);
    }

    [HttpPost]
    public async Task<ActionResult<Servicess>> Create([FromBody] ServicessCreateDto dto)
    {
        var service = new Servicess
        {
            Servicename = dto.Servicename,
            Unit = dto.Unit,
            Price = dto.Price
        };

        _context.Servicesses.Add(service);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = service.ServicessId }, service);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ServicessCreateDto dto)
    {
        var service = await _context.Servicesses.FindAsync(id);
        if (service == null)
        {
            return NotFound();
        }

        service.Servicename = dto.Servicename;
        service.Unit = dto.Unit;
        service.Price = dto.Price;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var service = await _context.Servicesses.FindAsync(id);
        if (service == null)
        {
            return NotFound();
        }

        _context.Servicesses.Remove(service);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    public class ServicessCreateDto
    {
        public string Servicename { get; set; } = null!;
        public string? Unit { get; set; }
        public decimal? Price { get; set; }
    }
}