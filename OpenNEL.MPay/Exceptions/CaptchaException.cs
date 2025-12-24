namespace OpenNEL.MPay.Exceptions;

public class CaptchaException : Exception
{
    public CaptchaException(string message) : base(message) { }
    public CaptchaException(string message, Exception inner) : base(message, inner) { }
}
