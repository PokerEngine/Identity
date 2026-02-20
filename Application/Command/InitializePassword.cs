using Application.Exception;
using Application.Repository;
using Application.Service.PasswordEncryptor;
using Application.Storage;
using Application.UnitOfWork;
using Domain.Entity;
using Domain.Exception;

namespace Application.Command;

public record InitializePasswordCommand : ICommand
{
    public required Guid AccountUid { get; init; }
    public required string Password { get; init; }
}

public record InitializePasswordResponse : ICommandResponse
{
    public required Guid AccountUid { get; init; }
}

public class InitializePasswordHandler(
    IRepository repository,
    IAccountStorage accountStorage,
    IPasswordEncryptor passwordEncryptor,
    IUnitOfWork unitOfWork
) : ICommandHandler<InitializePasswordCommand, InitializePasswordResponse>
{
    public async Task<InitializePasswordResponse> HandleAsync(InitializePasswordCommand command)
    {
        if (!await accountStorage.AccountExistsAsync(command.AccountUid))
        {
            throw new AccountNotFoundException("The account is not found");
        }

        if (await repository.ExistsAsync(command.AccountUid))
        {
            throw new PasswordInitializedException("The account password is already initialized");
        }

        var encryptedPassword = await passwordEncryptor.EncryptPassword(command.Password);
        var identity = Identity.FromScratch(
            accountUid: command.AccountUid,
            encryptedPassword: encryptedPassword
        );

        unitOfWork.Register(identity);
        await unitOfWork.CommitAsync();

        return new InitializePasswordResponse
        {
            AccountUid = identity.AccountUid
        };
    }
}
