namespace OpenNEL.MPay.Exceptions;

public class VerifyException : Exception
{
    public string ResponseBody { get; }

    public VerifyException(string responseBody) : base("Verification required")
    {
        ResponseBody = responseBody;
    }
}
