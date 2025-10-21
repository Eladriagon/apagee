namespace Apagee.Models;

public class ApageeException : Exception
{
    public ApageeException(string message) : base(message) { }
    public ApageeException(string message, Exception innerException) : base(message, innerException) { }
}