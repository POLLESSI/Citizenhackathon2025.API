using Dapper;
using CitizenHackathon2025.Domain.Interfaces;
using System.Data;
using CitizenHackathon2025.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using IDbConnection = System.Data.IDbConnection;


namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class PlaceRepository : IPlaceRepository
    {
    #nullable disable
        private readonly System.Data.IDbConnection _connection;
        private readonly ILogger<PlaceRepository> _logger;

        public PlaceRepository(IDbConnection connection, ILogger<PlaceRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public Task<IEnumerable<Place>> GetLatestPlaceAsync(int limit = 200, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT TOP(@Limit)
                    [Id], [Name], [Type], [Indoor], [Latitude], [Longitude], [Capacity], [Tag], [Active]
                FROM [Place]
                WHERE [Active] = 1
                ORDER BY Id DESC;";
            return _connection.QueryAsync<Place>(new CommandDefinition(sql, new { Limit = limit }, cancellationToken: ct));
        }

        public async Task<Place> SavePlaceAsync(Place place)
        {
            try
            {
                const string sql = "INSERT INTO [Place] ([Name], [Type], [Indoor], [Latitude], [Longitude], [Capacity], [Tag])" +
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
                const string sql = "SELECT [Id], [Name], [Type], [Indoor], [Latitude], [Longitude], [Capacity], [Tag] FROM [Place] WHERE [Id] = @Id AND Active = 1";

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
                const string sql = @"
                            IF EXISTS (SELECT 1 FROM Place WHERE Name=@Name)
                              UPDATE [Place] SET Type=@Type, Indoor=@Indoor, Latitude=@Latitude, Longitude=@Longitude, Capacity=@Capacity, Tag=@Tag
                              WHERE Name=@Name;
                            ELSE
                              INSERT INTO [Place]([Name], [Type], [Indoor], [Latitude], [Longitude], [Capacity], [Tag], [Active])
                              VALUES (@Name, @Type, @Indoor, @Latitude, @Longitude, @Capacity, @Tag, 1);";
                DynamicParameters parameters = new DynamicParameters();
                //parameters.Add("@Id", place.Id, DbType.Int64);
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