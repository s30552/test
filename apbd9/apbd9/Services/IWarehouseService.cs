using apbd9.Models;

namespace apbd9.Services;


public interface IWarehouseService
{
    Task<int> AddProductToWarehouseAsync(ProductWarehouse productWarehouse);
    Task<int> AddProductToWarehouseViaProcedureAsync(ProductWarehouse dto);

}