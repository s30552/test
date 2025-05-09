using APBD08.Services;

namespace APBD08;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        
        
        builder.Services.AddScoped<IWarehouseService, WarehouseService>();
        builder.Services.AddControllers();

        // Add services to the container..
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

       

        app.UseHttpsRedirection();

        app.UseAuthorization();
        app.MapControllers();





        app.Run();
    }
}