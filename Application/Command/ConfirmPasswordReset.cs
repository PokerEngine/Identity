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
    IIdentityRepository identityRepository,
    IPasswordResetTokenStorage passwordResetTokenStorage,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork
) : ICommandHandler<ConfirmPasswordResetCommand, ConfirmPasswordResetResponse>
{
    public async Task<ConfirmPasswordResetResponse> HandleAsync(ConfirmPasswordResetCommand command)
    {
        var accountUid = await passwordResetTokenStorage.VerifyTokenAsync(command.Token);
        var passwordHash = await passwordHasher.HashAsync(command.Password);
        var now = DateTime.UtcNow;

        Identity identity;

        try
        {
            identity = Identity.FromEvents(
                accountUid: accountUid,
                events: await identityRepository.GetEventsAsync(accountUid)
            );
            identity.ChangePassword(passwordHash, now);
        }
        catch (IdentityNotFoundException)
        {
            identity = Identity.FromScratch(
                accountUid: accountUid,
                passwordHash: passwordHash,
                now: now
            );
        }

        unitOfWork.Register(identity);
        await unitOfWork.CommitAsync();

        await passwordResetTokenStorage.DeleteTokensAsync(accountUid);

        return new ConfirmPasswordResetResponse();
    }
}
