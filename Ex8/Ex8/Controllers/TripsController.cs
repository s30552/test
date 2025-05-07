namespace Ex8.Controllers;
using Ex8.Services;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly ITripService _tripService;
    public TripsController(ITripService tripService)
    {
        _tripService = tripService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetTrips()
    {
        var trips = await _tripService.GetTripsAsync();
        return Ok(trips);
    }
    
}