using Ex8.Models;

namespace Ex8.Services;

public interface ITripService
{
    Task<List<Trip>> GetTripsAsync();
}