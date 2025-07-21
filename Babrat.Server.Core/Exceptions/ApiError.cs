namespace Babrat.Server.Core.Exceptions;

public class ApiError
{

    public required string ExceptionType
    {
        get; 
        set;
    }
    
    public required string Message
    {
        get;
        
        set;
    }
    
    
    public Details? Details
    { 
        get; 
        set;
        
    }
    
}

public class Details
{
    
    /// <summary>
    /// stores information about the call context
    /// </summary>
    public string ErrorContextType {
        
        get; 
        set;
        
    }
    
    public string? InnerException
    {
        get;
        
        set;
        
    }
    
    public string? MessageException
    {
        get;
        set;
    }

    public string? ValidationExample
    {
        
        get;

        set;
        
    }
    
    
}

