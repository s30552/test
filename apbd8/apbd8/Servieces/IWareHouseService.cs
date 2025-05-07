using apbd8.Models;

namespace apbd8.Servieces;

public interface IWareHouseService
{
    Task<int> AddProductToWarehouseAsync(AddToWarehouseDto dto);
    Task<int> AddProductToWarehouseViaProcAsync(AddToWarehouseDto dto);
}
