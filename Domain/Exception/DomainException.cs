namespace Domain.Exception;

public abstract class DomainException(string message) : System.Exception(message);

public class InvalidPasswordException(string message) : DomainException(message);
