namespace Domain.Exception;

public abstract class DomainException(string message) : System.Exception(message);

public class InvalidPasswordException(string message) : DomainException(message);

public class SessionExpiredException(string message) : DomainException(message);

public class SessionRevokedException(string message) : DomainException(message);
