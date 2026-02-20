namespace Domain.Exception;

public abstract class DomainException(string message) : System.Exception(message);

public class PasswordInitializedException(string message) : DomainException(message);

public class PasswordNotInitializedException(string message) : DomainException(message);

public class InvalidPasswordException(string message) : DomainException(message);
