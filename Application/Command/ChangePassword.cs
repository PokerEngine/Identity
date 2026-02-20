using Application.Exception;
using Application.Repository;
using Application.Service.PasswordEncryptor;
using Application.Storage;
using Application.UnitOfWork;
using Domain.Entity;
using Domain.Exception;

namespace Application.Command;

public record ChangePasswordCommand : ICommand
{
    public required Guid AccountUid { get; init; }
    public required string OldPassword { get; init; }
    public required string NewPassword { get; init; }
}

public record ChangePasswordResponse : ICommandResponse
{
    public required Guid AccountUid { get; init; }
}

public class ChangePasswordHandler(
    IRepository repository,
    IAccountStorage accountStorage,
    IPasswordEncryptor passwordEncryptor,
    IUnitOfWork unitOfWork
) : ICommandHandler<ChangePasswordCommand, ChangePasswordResponse>
{
    public async Task<ChangePasswordResponse> HandleAsync(ChangePasswordCommand command)
    {
        if (!await accountStorage.AccountExistsAsync(command.AccountUid))
        {
            throw new AccountNotFoundException("The account is not found");
        }

        if (!await repository.ExistsAsync(command.AccountUid))
        {
            throw new PasswordNotInitializedException("The account password is not initialized");
        }

        var identity = Identity.FromEvents(
            accountUid: command.AccountUid,
            events: await repository.GetEventsAsync(command.AccountUid)
        );

        await passwordEncryptor.ValidatePassword(command.OldPassword, identity.EncryptedPassword);

        var encryptedPassword = await passwordEncryptor.EncryptPassword(command.NewPassword);
        identity.ChangePassword(encryptedPassword);

        unitOfWork.Register(identity);
        await unitOfWork.CommitAsync();

        return new ChangePasswordResponse
        {
            AccountUid = identity.AccountUid
        };
    }
}
