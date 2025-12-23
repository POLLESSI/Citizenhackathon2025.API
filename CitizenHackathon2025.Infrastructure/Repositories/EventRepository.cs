using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using IDbConnection = System.Data.IDbConnection;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class EventRepository : IEventRepository
    {
#nullable disable
        private readonly IDbConnection _connection;
        private readonly ILogger<EventRepository> _logger;

        public EventRepository(IDbConnection connection, ILogger<EventRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }
        public async Task<Event> CreateEventAsync(Event newEvent)
        {
            const string sql = @"
                INSERT INTO [Event] ([Name], [PlaceId] [Latitude], [Longitude], [DateEvent], [ExpectedCrowd], [IsOutdoor], [Active])
                VALUES (@Name, @PlaceId, @Latitude, @Longitude, @DateEvent, @ExpectedCrowd, @IsOutdoor, 1);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Name", newEvent.Name);
                parameters.Add("@PlaceId", newEvent.PlaceId);
                parameters.Add("@Latitude", newEvent.Latitude);
                parameters.Add("@Longitude", newEvent.Longitude);
                parameters.Add("@DateEvent", newEvent.DateEvent);
                parameters.Add("@ExpectedCrowd", newEvent.ExpectedCrowd);
                parameters.Add("@IsOutdoor", newEvent.IsOutdoor);

                var newId = await _connection.ExecuteScalarAsync<int>(sql, parameters);

                newEvent.Id = newId; 
                newEvent.Active = true; 

                return newEvent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Event: {ex.Message}");
                throw; 
            }
        }

        public async Task<Event?> GetByIdAsync(int id)
        {
            try
            {
                const string sql = @"
            SELECT [Id],
                   [Name],
                   [PlaceId],
                   [Latitude],
                   [Longitude],
                   [DateEvent],
                   [ExpectedCrowd],
                   [IsOutdoor]
            FROM [Event]
            WHERE [Id] = @Id
              AND [Active] = 1";

                var parameters = new DynamicParameters();
                parameters.Add("@Id", id, DbType.Int32);

                var @event = await _connection.QueryFirstOrDefaultAsync<Event>(sql, parameters);

                return @event;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Event : {ex.Message}");
                return null;
            }
        }


        public Task<IEnumerable<Event>> GetLatestEventAsync(int limit = 10, CancellationToken ct = default)
        {
            const string sql = @"
                    SELECT TOP(@Limit)
                        [Id], [Name], [PlaceId], [Latitude], [Longitude], [DateEvent], [ExpectedCrowd], [IsOutdoor], [Active]
                    FROM [Event]
                    WHERE [Active] = 1
                    ORDER BY [DateEvent] DESC;";

            return _connection.QueryAsync<Event>(new CommandDefinition(sql, new { Limit = limit }, cancellationToken: ct));
        }

        public async Task<IEnumerable<Event>> GetUpcomingOutdoorEventsAsync()
        {
            string sql = @"
            SELECT [Id],
                   [Name],
                   [PlaceId],
                   [Latitude],
                   [Longitude],
                   [DateEvent],
                   [ExpectedCrowd],
                   [IsOutdoor],
                   [Active]
            FROM [Event]
            WHERE [IsOutdoor] = 1
              AND [Active] = 1
              AND [DateEvent] >= CAST(GETDATE() AS DATE)
            ORDER BY [DateEvent] ASC;";

            try
            {
                var events = await _connection.QueryAsync<Event>(sql);
                return events;

            }
            catch (Exception ex)
            {
                // Gestion d'erreur : log ou remonter une exception customisée si besoin
                throw new Exception("Error retrieving upcoming external events.", ex);
            }
        }

        public async Task<Event> SaveEventAsync(Event @event)
        {
            const string sql = @"
                    INSERT INTO [Event] ([Name], [PlaceId], [Latitude], [Longitude], [DateEvent], [ExpectedCrowd], [IsOutdoor], [Active])
                    VALUES (@Name, @PlaceId, @Latitude, @Longitude, @DateEvent, @ExpectedCrowd, @IsOutdoor, 1);
                    SELECT CAST(SCOPE_IDENTITY() as int);";
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Name", @event.Name, DbType.String);
                parameters.Add("@PlaceId", @event.PlaceId, DbType.Int32);
                parameters.Add("@Latitude", @event.Latitude, DbType.Decimal);
                parameters.Add("@Longitude", @event.Longitude, DbType.Decimal);
                parameters.Add("@DateEvent", @event.DateEvent, DbType.DateTime);
                parameters.Add("@ExpectedCrowd", @event.ExpectedCrowd, DbType.Int32);
                parameters.Add("@IsOutdoor", @event.IsOutdoor, DbType.Boolean);

                var newId = await _connection.ExecuteScalarAsync<int>(sql, parameters);
                @event.Id = newId;
                @event.Active = true; 
                return @event;
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 2627 || sqlEx.Number == 2601) // UNIQUE constraint
            {
                _logger.LogWarning(sqlEx, "Duplicate (Name, DateEvent) for event {Name} @ {DateEvent}", @event.Name, @event.DateEvent);
                return null; 
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error saving Event");
                return null;
            }
        }

        public Event UpdateEvent(Event @event)
        {
            if (@event == null || @event.Id <= 0)
            {
                throw new ArgumentException("Invalid event object", nameof(@event));
            }

            const string sql = @"
                    UPDATE [Event]
                    SET Name = @Name,
                        PlaceId = @PlaceId,
                        Latitude = @Latitude,     
                        Longitude = @Longitude,    
                        DateEvent = @DateEvent,
                        ExpectedCrowd = @ExpectedCrowd,
                        IsOutdoor = @IsOutdoor
                    WHERE Id = @Id AND Active = 1;";

            try
            {
                
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Id", @event.Id, DbType.Int32);
                parameters.Add("@Name", @event.Name, DbType.String);
                parameters.Add("@PlaceId", @event.PlaceId, DbType.Int32);
                parameters.Add("@Latitude", @event.Latitude, DbType.String);
                parameters.Add("@Longitude", @event.Longitude, DbType.String);
                parameters.Add("@DateEvent", @event.DateEvent, DbType.DateTime);
                parameters.Add("@ExpectedCrowd", @event.ExpectedCrowd, DbType.String);
                parameters.Add("@IsOutdoor", @event.IsOutdoor, DbType.String);

                var affectedRows = _connection.Execute(sql, parameters);

                if (affectedRows == 0) return null;
                
                return @event;
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 2627 || sqlEx.Number == 2601)
            {
                _logger.LogWarning(sqlEx, "Unique constraint violation on update for (Name, DateEvent)");
                return null;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, $"Error updating event with Id {@event.Id}");
                throw;
            }
        }
        public async Task<int> ArchivePastEventsAsync() 
        {
            const string sql = @"
                        UPDATE [Event]
                        SET [Active] = 0
                        WHERE [Active] = 1
                          AND [DateEvent] < DATEADD(DAY, -1, CAST(GETDATE() AS DATETIME2(0)));";

            try
            {
                var affectedRows = await _connection.ExecuteAsync(sql);
                _logger.LogInformation("{Count} event(s) archived.", affectedRows);
                return affectedRows;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error archiving past events.");
                return 0;
            }
        }
    }
}









































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.