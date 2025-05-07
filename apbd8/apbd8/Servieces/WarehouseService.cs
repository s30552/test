using System.Data;
using apbd8.Models;
using apbd8.Servieces;
using Microsoft.Data.SqlClient;


namespace apbd8.Services
{
    public class WarehouseService : IWareHouseService
    {
        private readonly string _connString;

        public WarehouseService(IConfiguration config)
            => _connString = config.GetConnectionString("DefaultConnection")!;

        public async Task<int> AddProductToWarehouseAsync(AddToWarehouseDto dto)
        {
            try
            {
                await using var conn = new SqlConnection(_connString);
                await conn.OpenAsync();
                await using var tran = await conn.BeginTransactionAsync();

                // 1) Sprawdź, czy istnieje produkt i magazyn
                await using (var cmdCheck = new SqlCommand(@"
                    SELECT 
                      (SELECT COUNT(*) FROM Product WHERE IdProduct = @p),
                      (SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @w)
                ", conn, (SqlTransaction)tran))
                {
                    cmdCheck.Parameters.AddWithValue("@p", dto.ProductId);
                    cmdCheck.Parameters.AddWithValue("@w", dto.WarehouseId);

                    await using var r = await cmdCheck.ExecuteReaderAsync();
                    if (!await r.ReadAsync() ||
                        r.GetInt32(0) == 0 ||
                        r.GetInt32(1) == 0)
                    {
                        throw new KeyNotFoundException("Nie znaleziono produktu lub magazynu");
                    }
                }

                // 2) Pobierz zamówienie i zweryfikuj jego Amount i CreatedAt
                DateTime orderCreatedAt;
                int    orderAmount;

                await using (var cmdOrder = new SqlCommand(@"
                    SELECT CreatedAt, Amount
                      FROM [Order]
                     WHERE IdOrder = @o
                ", conn, (SqlTransaction)tran))
                {
                    cmdOrder.Parameters.AddWithValue("@o", dto.OrderId);
                    await using var r2 = await cmdOrder.ExecuteReaderAsync();

                    if (!await r2.ReadAsync())
                        throw new KeyNotFoundException($"Zamówienie o IdOrder={dto.OrderId} nie istnieje");

                    orderCreatedAt = r2.GetDateTime(0);
                    orderAmount    = r2.GetInt32(1);
                }

                if (orderAmount != dto.Amount)
                    throw new InvalidOperationException(
                        $"Niepoprawna ilość: w zamówieniu było {orderAmount}, próbujesz wstawić {dto.Amount}");

                if (orderCreatedAt > dto.CreatedAt)
                    throw new InvalidOperationException(
                        "Czas utworzenia rekordu w magazynie nie może być wcześniejszy niż utworzenie zamówienia");

                // 3) Sprawdź, czy to zamówienie już nie zostało obsłużone
                await using (var cmdDone = new SqlCommand(@"
                    SELECT COUNT(*) FROM Product_Warehouse WHERE IdOrder = @o
                ", conn, (SqlTransaction)tran))
                {
                    cmdDone.Parameters.AddWithValue("@o", dto.OrderId);
                    var doneCount = (int)await cmdDone.ExecuteScalarAsync();
                    if (doneCount > 0)
                        throw new InvalidOperationException("To zamówienie jest już zrealizowane");
                }

                // 4) Ustaw FulfilledAt w zamówieniu
                await using (var cmdUpd = new SqlCommand(@"
                    UPDATE [Order]
                       SET FulfilledAt = GETDATE()
                     WHERE IdOrder = @o
                ", conn, (SqlTransaction)tran))
                {
                    cmdUpd.Parameters.AddWithValue("@o", dto.OrderId);
                    await cmdUpd.ExecuteNonQueryAsync();
                }

                // 5) Wstaw do Product_Warehouse (Amount + Price + CreatedAt=GETDATE())
                int newId;
                await using (var cmdIns = new SqlCommand(@"
                    INSERT INTO Product_Warehouse
                      (IdProduct, IdWarehouse, IdOrder, Amount, Price, CreatedAt)
                    VALUES
                      (
                        @p, @w, @o, @amt,
                        (SELECT Price FROM Product WHERE IdProduct = @p) * @amt,
                        GETDATE()
                      );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                ", conn, (SqlTransaction)tran))
                {
                    cmdIns.Parameters.AddWithValue("@p", dto.ProductId);
                    cmdIns.Parameters.AddWithValue("@w", dto.WarehouseId);
                    cmdIns.Parameters.AddWithValue("@o", dto.OrderId);
                    cmdIns.Parameters.AddWithValue("@amt", dto.Amount);

                    newId = (int)await cmdIns.ExecuteScalarAsync();
                }

                await tran.CommitAsync();
                return newId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddProductToWarehouseAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<int> AddProductToWarehouseViaProcAsync(AddToWarehouseDto dto)
        {
            await using var conn = new SqlConnection(_connString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_AddProductToWarehouse", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // parametry muszą odpowiadać procowi w bazie
            cmd.Parameters.AddWithValue("@IdProduct",   dto.ProductId);
            cmd.Parameters.AddWithValue("@IdWarehouse", dto.WarehouseId);
            cmd.Parameters.AddWithValue("@IdOrder",     dto.OrderId);
            cmd.Parameters.AddWithValue("@Amount",      dto.Amount);
            cmd.Parameters.AddWithValue("@CreatedAt",   dto.CreatedAt);

            var outParam = new SqlParameter("@NewId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outParam);

            await cmd.ExecuteNonQueryAsync();
            return (int)outParam.Value;
        }
    }
}
