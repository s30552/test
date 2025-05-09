using APBD09.Models;
using APBD09.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APBD09.Controllers

{
    
    
    
    [ApiController]
    [Route("api/[controller]")]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _service;

        public WarehouseController(IWarehouseService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> AddStock([FromBody] ProductWarehouse dto)
        {
            try
            {
                var newId = await _service.AddProductToWarehouseAsync(dto);
                return Ok(new { id = newId });
            }
            catch (Exception nf)
            {
                return NotFound(new { error = nf.Message });
            }
           
        }
        [HttpPost("via-proc")]
        public async Task<IActionResult> AddStockViaProcedure([FromBody] ProductWarehouse dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var newId = await _service.AddProductToWarehouseViaProcedureAsync(dto);
                return Ok(new { id = newId });
            }
            catch (SqlException sqlEx)
            {
                return BadRequest(new { error = sqlEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}


    
    
    
    

