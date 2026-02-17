namespace Domain.Exception;

public abstract class InvariantViolatedException(string message) : System.Exception(message);

public class InvalidIdentityStateException(string message) : InvariantViolatedException(message);
