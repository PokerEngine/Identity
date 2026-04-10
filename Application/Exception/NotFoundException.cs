namespace Application.Exception;

public abstract class NotFoundException : System.Exception
{
    protected NotFoundException(string message) : base(message) { }

    protected NotFoundException(string message, System.Exception innerException) : base(message, innerException) { }
}

public class IdentityNotFoundException : NotFoundException
{
    public IdentityNotFoundException(string message) : base(message) { }

    public IdentityNotFoundException(string message, System.Exception innerException) : base(message, innerException) { }
}

public class AccountNotFoundException : NotFoundException
{
    public AccountNotFoundException(string message) : base(message) { }

    public AccountNotFoundException(string message, System.Exception innerException) : base(message, innerException) { }
}

public class SessionNotFoundException : NotFoundException
{
    public SessionNotFoundException(string message) : base(message) { }

    public SessionNotFoundException(string message, System.Exception innerException) : base(message, innerException) { }
}
