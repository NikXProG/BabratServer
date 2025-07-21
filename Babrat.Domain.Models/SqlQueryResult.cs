namespace Babrat.Domain.Models;

public enum QueryResultType
{
    Create = 0,
    Drop = 1
}

public abstract class SqlQueryResult
{
    public QueryResultType QueryResultType
    {
        get;
        set;
    }
    
}