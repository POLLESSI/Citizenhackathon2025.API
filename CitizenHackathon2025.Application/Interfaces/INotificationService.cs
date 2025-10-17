namespace CitizenHackathon2025.Application.Interfaces
{
    public interface INotificationService
    {
        Task NotifyAsync(string message);
        Task NotifyEventAsync(string type, string message);
    }
}

































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.