namespace PoeShared.Scaffolding;

public class InvalidStateException : Exception
{
    public InvalidStateException()
    {
    }

    public InvalidStateException(string message) : base(message)
    {
    }

    public InvalidStateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}