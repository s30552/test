namespace Ex8.Services;

using Ex8.Models;
using Microsoft.Data.SqlClient;
using System.Data;


public class TripService : ITripService
{
    private readonly string _connectionString = "Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True;Trust Server Certificate=True";

    public async Task<List<Trip>> GetTripsAsync()
    {
        var trips = new List<Trip>();
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            
            var query = @"
                SELECT t.Id, t.Name, t.Description, t.StartDate, t.EndDate, t.MaxParticipants, 
                       c.Id AS CountryId, c.Name AS CountryName
                FROM Trips t
                LEFT JOIN TripCountries tc ON t.Id = tc.TripId
                LEFT JOIN Countries c ON tc.CountryId = c.Id";
            using (var command = new SqlCommand(query, connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var tripId = reader.GetInt32(reader.GetOrdinal("Id"));
                        var trip = trips.FirstOrDefault(t => t.Id == tripId);

                        if (trip == null)
                        {
                            trip = new Trip
                            {
                                Id = tripId,
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                                EndDate = reader.GetDateTime(reader.GetOrdinal("EndDate")),
                                MaxParticipants = reader.GetInt32(reader.GetOrdinal("MaxParticipants")),
                            };
                            trips.Add(trip);
                        }

                        if (!reader.IsDBNull(6))
                        {
                            trip.Countries.Add(new Country
                            {
                                Id = reader.GetInt32(6),
                                Name = reader.GetString(7)
                            });
                        }
                    }
                }
            }
        }
        return trips;
    }
}