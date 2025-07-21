using System.Globalization;
using Babrat.Domain.Models;
using Babrat.Server.Core;
using DryIoc.ImTools;
using DynamicExpresso;
using Example;
using ikvm.extensions;
using Microsoft.Extensions.Logging.Console;
using org.apache.calcite.linq4j.function;
using org.apache.calcite.rel.core;
using org.apache.calcite.sql;
using org.apache.calcite.sql.ddl;
using org.apache.calcite.sql.util;
using sun.security.x509;

namespace Babrat.Server.CalciteParser;
using System;
using System.Collections.Generic;
public interface ISqlModelBuilder
{
    SqlQueryResult BuildModel(SqlCall sqlCall);
}

public abstract class SqlModelBuilderBase : SqlBasicVisitor, ISqlModelBuilder
{
    protected SqlQueryResult _currentModel;
    
    public abstract SqlQueryResult BuildModel(SqlCall sqlCall);
    
    protected string TryGetDefaultValue(SqlNode sqlNode)
    {
   
        var defaultValue = sqlNode.toString().Trim('\'');
        
        if (TryEvaluateNumericExpression(defaultValue, out double numericValue))
        {
            defaultValue = numericValue.ToString(CultureInfo.InvariantCulture);
        }
        
        return defaultValue;
        
        bool TryEvaluateNumericExpression(string expr, out double result)
        {
            result = 0;
    
            try
            {
                var interpreter = new Interpreter();
                result = interpreter.Eval<double>(expr);
                return true;
            }
            catch (DynamicExpresso.Exceptions.ParseException)
            {
                return false;
            }
        }
        
        
    }

   
}

public class CreateTableModelBuilder : SqlModelBuilderBase
{
    private CreateTableModel CurrentModel => (CreateTableModel)_currentModel;
    
    public override SqlQueryResult BuildModel(SqlCall sqlCall)
    {
        var createTableQuery = (SqlCreateTable)sqlCall;
        
        InitializeModel(createTableQuery);
        ProcessTableComponents(createTableQuery.columnList);
        
        CurrentModel.QueryResultType = QueryResultType.Create;
        
        return _currentModel;
    }

    private void InitializeModel(SqlCreateTable createTableQuery)
    {
        _currentModel = new CreateTableModel
        {
            TableName = createTableQuery.name.ToString(),
            Columns = []
        };
    }

    private void ProcessTableComponents(SqlNodeList elements)
    {
        foreach (var node in elements)
        {
            switch (node)
            {
                case SqlColumnDeclaration columnDecl:
                    AddColumn(columnDecl);
                    break;
                case SqlKeyConstraint keyConstraint:
                    ProcessKeyConstraint(keyConstraint);
                    break;
                case SqlCheckConstraint checkConstraint:
                    // ProcessCheckConstraint(checkConstraint);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported node type: {node.GetType().Name}");
            }
        }
    }
    
    

    private void AddColumn(SqlColumnDeclaration columnDecl)
    {
      
        CurrentModel.Columns.Add(new ColumnModel
        {
            ColumnName = columnDecl.name.ToString(),
            DataType = columnDecl.dataType.ToString(),
            IsNullable = columnDecl.dataType.getNullable().booleanValue(),
            Default = GetDefaultValue(columnDecl.expression)
        });
        
       
    }

    private string GetDefaultValue(SqlNode? expression)
    {
        return expression == null ? string.Empty : TryGetDefaultValue(expression);
    }

    private void ProcessKeyConstraint(SqlKeyConstraint constraint)
    {
        var kind = constraint.getKind();

        SetConstraint(constraint,
                kind == SqlKind.PRIMARY_KEY
                    ? col => col.IsPrimaryKey = true
                    : col => col.IsUnique = false);
        
    }

    private void SetConstraint(SqlKeyConstraint constraint, Action<ColumnModel> setter)
    {
        var node = (SqlNodeList)constraint.getOperandList().get(1);
        
        foreach (SqlNode column in  node)
        {
             var col = CurrentModel.Columns.FirstOrDefault(c => c.ColumnName == column.ToString());
             if (col != null) setter(col);
        }
        
    }
   
    // private void ProcessCheckConstraint(SqlCheckConstraint constraint)
    // {
    //     var node = (SqlNode) constraint.getOperandList().get(1);
    //
    //  
    //     Console.WriteLine(node);
    //     
    //     // var condition = (SqlNode) constraint.getOperandList().get(0);
    //     // Console.WriteLine(condition.getMonotonicity());
    //     // Console.WriteLine($"Check Constraint: {condition}");
    // }
    
    
}

public class InsertModelBuilder : SqlModelBuilderBase
{
    private InsertModel CurrentModel => (InsertModel)_currentModel;
    
    public override SqlQueryResult BuildModel(SqlCall sqlCall)
    {
        var insertQuery = (SqlInsert)sqlCall;
        
        InitializeModel(insertQuery);
        ProcessColumns(insertQuery.getTargetColumnList());
        insertQuery.getSource().accept(this);
        
        return CurrentModel;
    }
    
    private void InitializeModel(SqlInsert insertQuery)
    {
        _currentModel = new InsertModel
        {
            TableName = insertQuery.getTargetTable().ToString(),
            ColumnNames = new List<string>(),
            Rows = new List<InsertRow> { new InsertRow() }
        };
    }
    
    private void ProcessColumns(SqlNodeList columns)
    {
        foreach (var column in columns)
        {
            CurrentModel.ColumnNames.Add(column.toString());
        }
    }
    
    public override object visit(SqlIdentifier identifier)
    {
        AddValueToCurrentRow(identifier.toString());
        return base.visit(identifier);
    }

    public override object visit(SqlLiteral literal)
    {
      
        AddValueToCurrentRow(
            literal.getTypeName().equals("NULL") ? null : literal.toString());
        
        return base.visit(literal);
    }
    
    
    private void AddValueToCurrentRow(string? value)
    {
        var currentRow = CurrentModel.Rows.Last();
        
        if (currentRow.Values.Count >= CurrentModel.ColumnNames.Count)
        {
            currentRow = new InsertRow();
            CurrentModel.Rows.Add(currentRow);
        }
        
        currentRow.Values.Add(value);
    }
}
    
public class SelectModelBuilder : SqlModelBuilderBase
{
    private InsertModel CurrentModel => (InsertModel)_currentModel;
    
    public override SqlQueryResult BuildModel(SqlCall sqlCall)
    {
        var insertQuery = (SqlInsert)sqlCall;
        
        InitializeModel(insertQuery);
        ProcessColumns(insertQuery.getTargetColumnList());
        insertQuery.getSource().accept(this);
        
        return CurrentModel;
    }
    
    private void InitializeModel(SqlInsert insertQuery)
    {
        _currentModel = new InsertModel
        {
            TableName = insertQuery.getTargetTable().ToString(),
            ColumnNames = new List<string>(),
            Rows = new List<InsertRow> { new InsertRow() }
        };
    }
    
    private void ProcessColumns(SqlNodeList columns)
    {
        foreach (var column in columns)
        {
            CurrentModel.ColumnNames.Add(column.toString());
        }
    }
    
    public override object visit(SqlIdentifier identifier)
    {
        AddValueToCurrentRow(identifier.toString());
        return base.visit(identifier);
    }

    public override object visit(SqlLiteral literal)
    {
        AddValueToCurrentRow(literal.toString());
        return base.visit(literal);
    }
    
    
    private void AddValueToCurrentRow(string value)
    {
        var currentRow = CurrentModel.Rows.Last();
        
        if (currentRow.Values.Count >= CurrentModel.ColumnNames.Count)
        {
            currentRow = new InsertRow();
            CurrentModel.Rows.Add(currentRow);
        }
        
        currentRow.Values.Add(value);
    }
    
}

public class SqlToModelVisitor : SqlBasicVisitor
{
    
    private readonly Dictionary<SqlKind, ISqlModelBuilder> _builders;

    public SqlToModelVisitor()
    {
        _builders = new Dictionary<SqlKind, ISqlModelBuilder>
        {
            { SqlKind.CREATE_TABLE, new CreateTableModelBuilder() },
            { SqlKind.INSERT, new InsertModelBuilder() },
            { SqlKind.SELECT, new SelectModelBuilder() }
        };
        
    }
    
   public override object visit(SqlCall sqlCall)
    {
        var nodeKind = sqlCall.getKind();
        
        if (_builders.TryGetValue(nodeKind, out var builder))
        {
            return builder.BuildModel(sqlCall);
        }
        
        return base.visit(sqlCall);
    }
   

    
}

