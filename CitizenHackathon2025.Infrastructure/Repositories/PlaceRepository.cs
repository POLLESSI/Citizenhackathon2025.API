using System;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Citizenhackathon2025.Domain.Interfaces;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Numerics;
using Microsoft.Extensions.Logging;
using CitizenHackathon2025.Domain.Entities;

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
                string sql = " SELECT * FROM Place WHERE Active = 1";

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
        public async Task<Place?> GetPlaceByIdAsync(int id)
        {
            try
            {
                const string sql = "SELECT Id, Name, Type, Indoor, Latitude, Longitude, Capacity, Tag FROM Place WHERE Id = @Id AND Active = 1";

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id, DbType.Int64);

                var place = await _connection.QueryFirstOrDefaultAsync<Place>(sql, parameters);

                return place;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error geting Place : {ex.ToString}");
                return null;
            }

        }

        public Place UpdatePlace(Place place)
        {
            if (place == null || place.Id <= 0)
            {
                throw new ArgumentException("Invalid place to update.", nameof(place));
            }
            try
            {
                string sql = "UPDATE Place SET Name = @Name, Type = @Type, Indoor = IIF(@Indoor = 'true', 1, 0), Latitude = CAST(@Latitude AS DECIMAL(8, 6)), Longitude = CAST(@Longitude AS DECIMAL(9, 6)), Capacity = @Capacity, Tag = @Tag WHERE ID = @Id AND Active = 1";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Id", place.Id, DbType.Int64);
                parameters.Add("@Name", place.Name, DbType.String);
                parameters.Add("@Type", place.Type, DbType.String);
                parameters.Add("@Indoor", place.Indoor, DbType.String);
                parameters.Add("@Latitude", place.Latitude, DbType.String);
                parameters.Add("@Longitude", place.Longitude, DbType.String);
                parameters.Add("@Capacity", place.Capacity, DbType.String);
                parameters.Add("@Tag", place.Tag, DbType.String);

                var affectedRows = _connection.Execute(sql, parameters);
                return affectedRows > 0 ? place : null;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error updating place: {ex.Message}");
                
            }
            return null;
        }
    }
}


































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.