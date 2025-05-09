using System;
using System.Data;
using System.Threading.Tasks;
using APBD09.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace APBD09.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly string _connectionString;

        public WarehouseService(IConfiguration configuration)
        {
            _connectionString = configuration
                .GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Brak connection stringa 'DefaultConnection' w konfiguracji.");
        }

     
        public async Task<int> AddProductToWarehouseAsync(ProductWarehouse dto)
        {
            const string getPrice =
                "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            const string checkWarehouseQuery =
                "SELECT IdWarehouse FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            const string checkOrderQuery =
                @"SELECT amount, createdAt 
                  FROM [Order] 
                  WHERE IdOrder = @IdOrder
                    AND IdProduct = @IdProduct
                    AND Amount = @Amount";
            const string checkFulfilledQuery =
                "SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder";
            const string updateOrderQuery =
                @"UPDATE [Order]
                  SET FulfilledAt = @CreatedAt
                  WHERE IdOrder = @IdOrder;";
            const string insertPw =
                @"INSERT INTO Product_Warehouse
                    (IdProduct, IdWarehouse, Amount, Price, IdOrder, CreatedAt)
                  OUTPUT inserted.IdProductWarehouse
                  VALUES
                    (@IdProduct, @IdWarehouse, @Amount, @Price, @IdOrder, @CreatedAt)";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;

            try
            {
                // 1. Pobranie ceny produktu
                cmd.CommandText = getPrice;
                cmd.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
                var priceObj = await cmd.ExecuteScalarAsync();
                if (priceObj == null)
                    throw new Exception("Product not found");
                decimal unitPrice = (decimal)priceObj;

                // 2. Sprawdzenie magazynu
                cmd.Parameters.Clear();
                cmd.CommandText = checkWarehouseQuery;
                cmd.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
                var whObj = await cmd.ExecuteScalarAsync();
                if (whObj == null)
                    throw new Exception("Warehouse not found");

                // 3. Walidacja ilości
                if (dto.Amount <= 0)
                    throw new Exception("Amount must be greater than 0");

                // 4. Sprawdzenie zamówienia
                cmd.Parameters.Clear();
                cmd.CommandText = checkOrderQuery;
                cmd.Parameters.AddWithValue("@IdOrder", dto.IdOrder);
                cmd.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
                cmd.Parameters.AddWithValue("@Amount", dto.Amount);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    throw new Exception($"Order ID {dto.IdOrder} not found or mismatch.");
                var orderCreatedAt = reader.GetDateTime(1);
                await reader.CloseAsync();

                if (orderCreatedAt >= dto.CreatedAt)
                    throw new Exception(
                        "Request date must be later than the order creation date.");

                // 5. Sprawdzenie, czy nie jest już zrealizowane
                cmd.Parameters.Clear();
                cmd.CommandText = checkFulfilledQuery;
                cmd.Parameters.AddWithValue("@IdOrder", dto.IdOrder);
                var fulObj = await cmd.ExecuteScalarAsync();
                if (fulObj != null)
                    throw new Exception("Order already fulfilled");

                // 6. Oznaczenie zamówienia jako zrealizowanego
                cmd.Parameters.Clear();
                cmd.CommandText = updateOrderQuery;
                cmd.Parameters.AddWithValue("@IdOrder", dto.IdOrder);
                cmd.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);
                await cmd.ExecuteNonQueryAsync();

                // 7. Wstawienie rekordu do Product_Warehouse
                decimal totalPrice = unitPrice * dto.Amount;
                cmd.Parameters.Clear();
                cmd.CommandText = insertPw;
                cmd.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
                cmd.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
                cmd.Parameters.AddWithValue("@Amount", dto.Amount);
                cmd.Parameters.AddWithValue("@Price", totalPrice);
                cmd.Parameters.AddWithValue("@IdOrder", dto.IdOrder);
                cmd.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);

                var newId = (int)await cmd.ExecuteScalarAsync();
                tx.Commit();
                return newId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        
        public async Task<int> AddProductToWarehouseViaProcedureAsync(ProductWarehouse dto)
        {
            await using var conn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand("[dbo].[AddProductToWarehouse]", conn) { CommandType = CommandType.StoredProcedure };


            cmd.Parameters.AddWithValue("@IdProduct",   dto.IdProduct);
            cmd.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
            cmd.Parameters.AddWithValue("@Amount",      dto.Amount);
            cmd.Parameters.AddWithValue("@CreatedAt",   dto.CreatedAt);

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }
}
