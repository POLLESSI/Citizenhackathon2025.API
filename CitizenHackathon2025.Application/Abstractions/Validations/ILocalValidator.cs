namespace CitizenHackathon2025.Application.Abstractions.Validations
{
    public interface ILocalValidator<in TRequest>
    {
        Task ValidateAsync(TRequest request, CancellationToken cancellationToken);
    }

}
