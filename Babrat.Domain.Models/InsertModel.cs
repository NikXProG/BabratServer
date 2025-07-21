namespace Babrat.Domain.Models;

public class InsertModel : SqlQueryResult
{
    
    public string TableName
    {
        get; 
        set;
    }
    
    public List<string> ColumnNames
    {
        get; 
        set;
    } = new List<string>();
    
    public List<InsertRow> Rows
    {
        get; 
        set;
    } = new List<InsertRow>();
    
}

public class InsertRow
{
    public List<string> Values
    {
        get; 
        set;
    } = new List<string>();
    
}
