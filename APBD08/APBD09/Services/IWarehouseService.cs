namespace APBD08.Services;
using APBD08.Models;

public interface IWarehouseService
{
    Task<int> AddProductToWarehouseAsync(ProductWarehouse productWarehouse);
    Task<int> AddProductToWarehouseViaProcedureAsync(ProductWarehouse dto);

}