using System.Reflection;
using System.Text.Json;
using Babrat.Domain.Models;
using Babrat.Server.CalciteParser.Settings;
using Babrat.Server.Core;
using com.sun.tools.@internal.ws.processor.model;
using Microsoft.AspNetCore.Http;
using org.slf4j;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using org.apache.calcite.config;
using org.apache.calcite.sql;
using org.apache.calcite.sql.ddl;
using org.apache.calcite.sql.parser;
using org.apache.calcite.sql.parser.ddl;
using org.apache.calcite.sql.parser.impl;
using org.apache.calcite.sql.validate;
using sun.security.krb5;

namespace Babrat.Server.CalciteParser;

public class CalciteQueryParser : IQuerySqlParser
{
    
    #region Fields

    private readonly ILogger<CalciteQueryParser> _logger;
    
    private readonly SqlParser.Config _configuration;
    
    #endregion
    
    #region Constructors

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="logger"></param>
    public CalciteQueryParser(
        SqlParser.Config configuration,
        ILogger<CalciteQueryParser> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    #endregion
    
    #region Methods

    
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    public Task<SqlQueryResult> ParseAsync(string query,
        CancellationToken cancellationToken = default)
    {
        
        _logger.LogInformation("Parsing query...");
        
        try
        {
            var sqlNode =  SqlParser.create(
                query, 
                _configuration)
                .parseStmt();
            
            _logger.LogInformation("Parsing statements...");
            
            // if (sqlNode is SqlSelect select)
            // {
            //     Console.WriteLine("Select list: " +  select.getSelectList());
            //     Console.WriteLine("Table name: " +  select.getFrom().toString());
            //     
            // }
            //
            // if (sqlNode is SqlCreateTable table) {
            //     String tableName = table.name.getSimple();
            //     Console.WriteLine($"Table name: {tableName}");
            //     foreach (SqlNode columnNode in  table.columnList) {
            //
            //     if (columnNode is SqlColumnDeclaration column) {
            //         String columnName = column.name.getSimple();
            //         SqlDataTypeSpec dataType = column.dataType;
            //
            //         String typeString = dataType.toString();
            //         bool notNull = false;
            //
            //
            //         Console.WriteLine("  Column: " + columnName);
            //         Console.WriteLine("    Type: " + typeString);
            //        
            //         Console.WriteLine(" Not NUll: " + column.strategy);
            //     }
            //
            //     else if (columnNode is SqlKeyConstraint keyConstraint) {
            //         
            //         Console.WriteLine("KeyConstraint: " + keyConstraint.getOperator().getName());
            //         Console.WriteLine("Columns: " + keyConstraint);
            //     }
            //     else if (columnNode is SqlCheckConstraint checkConstraint) {
            //         
            //         Console.WriteLine("KeyConstraint: " + checkConstraint.getOperator().getName());
            //         Console.WriteLine("Columns: " + checkConstraint.getKind().sql);
            //     }
            //     else {
            //         Console.WriteLine("Other node: " + columnNode.getClass().getSimpleName());
            //         Console.WriteLine("  Value: " + columnNode.toString());
            //     }
            //
            // }
            //
            //  
            //
            // }
            
            return Task.FromResult((SqlQueryResult)sqlNode.accept(new SqlToModelVisitor()));
            
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to parse query.");
            return Task.FromResult<SqlQueryResult>(null);
        }
       

        // foreach (SqlNode stat in statements)
        // {
        //     if (stat is not SqlCreateTable table) continue;
        //     String tableName = table.name.getSimple();
        //     _logger.LogInformation("Создать таблицу: " + tableName);
        // }
        
        
    }
    
    #endregion
    
    
    
}