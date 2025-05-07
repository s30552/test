using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace apbd8.Controllers;
using System.Data.SqlClient;
using apbd8.Models;
using apbd8.Servieces;

[ApiController]
[Route("api/[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly IWareHouseService _repo;
    public WarehouseController(IWareHouseService repo) => _repo = repo;
    
    
    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] AddToWarehouseDto dto)
    {
        try
        {
            var id = await _repo.AddProductToWarehouseAsync(dto); // Fixed method call
            return CreatedAtAction(null, new { id }, null);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch
        {
            return BadRequest();
        }
    }
    
    [HttpPost("add-via-proc")]
    public async Task<IActionResult> AddViaProc([FromBody] AddToWarehouseDto dto)
    {
        try
        {
            var id = await _repo.AddProductToWarehouseViaProcAsync(dto);
            return CreatedAtAction(null, new { id }, null);
        }
        catch (SqlException)
        {
            return BadRequest("Błąd w procedurze składowanej");
        }
    }
}
