using APBD09.Models;

namespace APBD09.Services;
using APBD09.Models;

public interface IWarehouseService
{
    Task<int> AddProductToWarehouseAsync(ProductWarehouse productWarehouse);
    Task<int> AddProductToWarehouseViaProcedureAsync(ProductWarehouse dto);

}