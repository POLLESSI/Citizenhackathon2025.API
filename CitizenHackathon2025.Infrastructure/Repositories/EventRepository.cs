using Dapper;
using CitizenHackathon2025.Domain.Entities;
using System.Data;
using Microsoft.Extensions.Logging;
using IDbConnection = System.Data.IDbConnection;
using CitizenHackathon2025.Domain.Interfaces;

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
            INSERT INTO [Event] 
                ([Name], [Latitude], [Longitude], [DateEvent], [ExpectedCrowd], [IsOutdoor], [Active])
            VALUES 
                (@Name, @Latitude, @Longitude, @DateEvent, @ExpectedCrowd, @IsOutdoor, 1);
            SELECT CAST(SCOPE_IDENTITY() as int);";

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Name", newEvent.Name);
                parameters.Add("@Latitude", newEvent.Latitude);
                parameters.Add("@Longitude", newEvent.Longitude);
                parameters.Add("@DateEvent", newEvent.DateEvent);
                parameters.Add("@ExpectedCrowd", newEvent.ExpectedCrowd);
                parameters.Add("@IsOutdoor", newEvent.IsOutdoor);

                var newId = await _connection.ExecuteScalarAsync<int>(sql, parameters);

                newEvent.Id = newId; // Important pour retourner l'objet complété
                newEvent.Active = true; // Puisque créé actif par défaut

                return newEvent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Event: {ex.Message}");
                throw; // Laisse l'exception remonter pour traitement global
            }
        }

        public async Task<Event?> GetByIdAsync(int id)
        {
            try
            {
                const string sql = @"
            SELECT [Id],
                   [Name],
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


        public async Task<IEnumerable<Event>> GetLatestEventAsync()
        {
            try
            {
                string sql = " SELECT TOP 10 * FROM Event WHERE Active = 1 ORDER BY DateEvent DESC";

                var events = await _connection.QueryAsync<Event?>(sql);
                return [.. events];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving certifications: {ex.Message}");
                return [];
            }

        }

        public async Task<IEnumerable<Event>> GetUpcomingOutdoorEventsAsync()
        {
            string sql = @"
            SELECT [Id],
                   [Name],
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
            try
            {
                string sql = "INSERT INTO Event (Id, Name, Latitude, Longitude, DateEvent, ExpectedCrowd, IsOutdoor)" +
                "VALUES (@Id, @Name, @Latitude, @Longitude, @DateEvent, @ExpectedCrow, @IsOutdoor)";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Name", @event.Name);
                parameters.Add("@Latitude", @event.Latitude);
                parameters.Add("@Longitude", @event.Longitude);
                parameters.Add("@DateEvent", @event.DateEvent);
                parameters.Add("@ExpectedCrowd", @event.ExpectedCrowd);

                int rowsAffected = await _connection.ExecuteAsync(sql, parameters);
                return @event;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error adding certification: {ex.ToString()}");
                return null;
            }
        }

        public Event UpdateEvent(Event @event)
        {
            if (@event == null || @event.Id <= 0)
            {
                _logger.LogWarning("Invalid event data provided for update.");
                throw new ArgumentException("Invalid event object", nameof(@event));
            }
            try
            {
                string sql = "UPDATE Event SET Name = @Name, Latitude = CAST(@Latitude AS DECIMAL(8, 2)), Longitude = CAST(@Longitude AS DECIMAL(9, 3)), DateEvent = @DateEvent, ExpectecCrowd = @ExpectedCrowd, IsOutdoor = @IsOutdoor WHERE Id = @Id AND Active = 1";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Id", @event.Id, DbType.Int32);
                parameters.Add("@Name", @event.Name, DbType.String);
                parameters.Add("@Latitude", @event.Latitude, DbType.String);
                parameters.Add("@Longitude", @event.Longitude, DbType.String);
                parameters.Add("@DateEvent", @event.DateEvent, DbType.DateTime);
                parameters.Add("@ExpectedCrowd", @event.ExpectedCrowd, DbType.String);
                parameters.Add("@IsOutdoor", @event.IsOutdoor, DbType.String);

                var affectedRows = _connection.Execute(sql, parameters);

                if (affectedRows == 0)
                {
                    _logger.LogWarning($"No event found to update with Id {@event.Id}");
                    return null;
                }

                _logger.LogInformation($"Event with Id {@event.Id} successfully updated.");
                return @event;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, $"An error occurred while updating the event with Id {@event.Id}");
                throw;
            }
        }
        public async Task<int> ArchivePastEventsAsync() 
        {
            string sql = "UPDATE Event SET Active = 0 WHERE Active = 1 AND DateEvent < DATEADD(DAY, -2, CAST(GETDATE() AS DATE))";

            try
            {
                var affectedRows = await _connection.ExecuteAsync(sql);
                _logger.LogInformation($"{affectedRows} event(s) archived.");
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