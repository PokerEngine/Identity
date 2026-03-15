using Application.Exception;
using Application.Repository;
using Application.Service.PasswordHasher;
using Application.Storage;
using Application.UnitOfWork;
using Domain.Entity;

namespace Application.Command;

public record ConfirmPasswordResetCommand : ICommand
{
    public required string Token { get; init; }
    public required string Password { get; init; }
}

public record ConfirmPasswordResetResponse : ICommandResponse;

public class ConfirmPasswordResetHandler(
    IRepository repository,
    IPasswordResetTokenStorage passwordResetTokenStorage,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork
) : ICommandHandler<ConfirmPasswordResetCommand, ConfirmPasswordResetResponse>
{
    public async Task<ConfirmPasswordResetResponse> HandleAsync(ConfirmPasswordResetCommand command)
    {
        var accountUid = await passwordResetTokenStorage.VerifyTokenAsync(command.Token);
        var passwordHash = await passwordHasher.HashAsync(command.Password);

        Identity identity;

        try
        {
            identity = Identity.FromEvents(
                accountUid: accountUid,
                events: await repository.GetEventsAsync(accountUid)
            );
            identity.ChangePassword(passwordHash);
        }
        catch (IdentityNotFoundException)
        {
            identity = Identity.FromScratch(
                accountUid: accountUid,
                passwordHash: passwordHash
            );
        }

        unitOfWork.Register(identity);
        await unitOfWork.CommitAsync();

        await passwordResetTokenStorage.DeleteTokensAsync(accountUid);

        return new ConfirmPasswordResetResponse();
    }
}
