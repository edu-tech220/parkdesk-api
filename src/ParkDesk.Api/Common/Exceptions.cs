namespace ParkDesk.Api.Common;

/// <summary>Base for exceptions that the global handler maps to a ProblemDetails response.</summary>
public abstract class ApiException : Exception
{
    protected ApiException(string message) : base(message)
    {
    }

    public abstract int StatusCode { get; }
}

public class NotFoundException : ApiException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status404NotFound;
}

public class ConflictException : ApiException
{
    public ConflictException(string message) : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status409Conflict;
}

public class BusinessRuleException : ApiException
{
    public BusinessRuleException(string message) : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status422UnprocessableEntity;
}

public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message) : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status401Unauthorized;
}
