
namespace Babrat.Domain.Models;


public class CreateTableModel : SqlQueryResult
{
    public string TableName
    {
        get;
        set;
    }

    public List<ColumnModel> Columns
    {
        get; 
        set;
    }
    
}

public class ColumnModel
{
    public string ColumnName { get; set; }
    public string DataType { get; set; }
    public bool IsNullable { get; set; }
    public string? Default { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsUnique { get; set; }
    
}