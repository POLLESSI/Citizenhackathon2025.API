using System;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Citizenhackathon2025.Domain.Interfaces;

namespace Citizenhackathon2025.Infrastructure.Repositories
{
    public class PlaceRepository : IPlaceRepository
    {
    #nullable disable
        private readonly System.Data.IDbConnection _connection;

        public PlaceRepository(System.Data.IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<Place?>> GetLatestPlaceAsync()
        {
            try
            {
                string sql = " SELECT * FROM Place Where Active = 1";

                var places = await _connection.QueryAsync<Place?>(sql);
                return [.. places];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving Place: {ex.Message}");
                return [];
            }
        }

        public async Task<Place> SavePlaceAsync(Place place)
        {
            try
            {
                string sql = "INSERT INTO Place (Name, Type, Indoor, Latitude, Longitude, Capacity, Tag)" +
                "VALUES (@Name, @Type, @Indoor, @Latitude, @Longitude, @Capacity, @Tag)";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Name", place.Name);
                parameters.Add("@Type", place.Type);
                parameters.Add("@Indoor", place.Indoor);
                parameters.Add("@Latitude", place.Latitude);
                parameters.Add("@Longitude", place.Longitude);
                parameters.Add("@Capacity", place.Capacity);
                parameters.Add("@Tag", place.Tag);

                int rowsAffected = await _connection.ExecuteAsync(sql, parameters);
                return place;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error adding Place: {ex.ToString()}");
                return null;
            }
        }
        public async Task<Place?> GetByIdAsync(int id)
        {
            try
            {
                const string sql = "SELECT * FROM Place WHERE Id = @Id AND Active = 1";

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id, DbType.Int64);

                var place = await _connection.QueryFirstOrDefaultAsync<Place?>(sql, parameters);

                return place;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error geting Place : {ex.ToString}");
                return null;
            }

        }
    }
}
