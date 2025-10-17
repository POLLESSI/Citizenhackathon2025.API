namespace CitizenHackathon2025.Application.Abstractions.Validations
{
    public interface ILocalValidator<in TRequest>
    {
        Task ValidateAsync(TRequest request, CancellationToken cancellationToken);
    }

}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.