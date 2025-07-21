using System.Text;
using System.Text.Json;
using Babrat.Domain.Models;
using Babrat.Server.Core;
using Babrat.Server.Core.Exceptions;
using com.sun.tools.@internal.ws.processor.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using org.apache.calcite.sql.parser;
using Exception = System.Exception;

namespace Babrat.Server.REST.API.Controller;

/// <summary>
/// 
/// </summary>
[Authorize] 
[ApiController]
[Route("/api/babrat-db")]
[Produces("application/json")]

public sealed partial class FileDbController :
    ControllerBase
{

    #region Fields

    private readonly IQuerySqlParser _querySqlParser;

    private readonly ILogger<FileDbController> _logger;

    private readonly IQueryProcessor _queryProcessor;

    #endregion

    #region Constructors

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="queryProcessor"></param>
    /// <param name="querySqlParser"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public FileDbController(
        IQueryProcessor queryProcessor,
        IQuerySqlParser querySqlParser,
        ILogger<FileDbController> logger)
    {
        _queryProcessor = queryProcessor ?? throw new ArgumentNullException(nameof(queryProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _querySqlParser = querySqlParser ?? throw new ArgumentNullException(nameof(querySqlParser));
    }

    #endregion

    #region API methods

    [HttpPost("send")]
    public async Task<IActionResult> SendQueryAsync(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (file == null)
        {
            return BadRequest("No file provided");
        }

        try
        {
            var results = await ProcessSqlFileAsync(file, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                $"Data with {string.Join(',', results)} queries processed successfully.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("SQL query processing was canceled");
            return StatusCode(StatusCodes.Status499ClientClosedRequest, "Request was canceled");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to process SQL queries");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ApiError
                {
                    ExceptionType = e.GetType().Name,
                    Message = "Failed to process SQL queries. Please try again later.",
                    Details = new Details
                    {
                        MessageException = e.Message,
                        InnerException = e.InnerException?.Message,
                        ErrorContextType = nameof(SendQueryAsync)
                    }
                });
        }
    }


    #endregion

    private async Task<List<string>> ProcessSqlFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var results = new List<string>();
        
        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: false);

        var currentQuery = new StringBuilder(1024);
        var parseState = new ParseState();

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            ProcessLine(line, currentQuery, parseState, results, cancellationToken);
        }
        
        ProcessRemainingQuery(currentQuery, results, cancellationToken);

        return results;
    }

    private readonly struct ParseState
    {
        public bool InString { get; init; }
        public bool InBlockComment { get; init; }
        public bool InLineComment { get; init; }
        public bool Escape { get; init; }
        public char StringDelimiter { get; init; }
        public char PrevChar { get; init; }
    }

    private void ProcessLine(string line, StringBuilder currentQuery, ParseState state, List<string> results, CancellationToken cancellationToken)
    {
        var span = line.AsSpan();
        foreach (var c in span)
        {
            var newState = ProcessChar(c, ref state, currentQuery, results, cancellationToken);
            state = newState;
        }
        
    }

    private ParseState ProcessChar(char c, ref ParseState state, StringBuilder currentQuery, List<string> results, CancellationToken cancellationToken)
    {
     
        if (state.InLineComment)
        {
            return state;
        }

        if (state.InBlockComment)
        {
            if (state.PrevChar == '*' && c == '/')
            {
                return state with { InBlockComment = false, PrevChar = '\0' };
            }
            return state with { PrevChar = c };
        }
        
        if (state.InString)
        {
            currentQuery.Append(c);
            
            if (c == state.StringDelimiter && !state.Escape)
            {
                return state with { InString = false, StringDelimiter = '\0', PrevChar = c };
            }
        
            return state with { Escape = c == '\\' && !state.Escape, PrevChar = c };
        }
        
        if (!state.InString && (c == '\'' || c == '"'))
        {
            currentQuery.Append(c);
            return state with { InString = true, StringDelimiter = c, PrevChar = c };
        }
        
        if (state.PrevChar == '/' && c == '*')
        {
            currentQuery.Length--;
            return state with { InBlockComment = true, PrevChar = '\0' };
        }

        if (state.PrevChar == '-' && c == '-')
        {
            currentQuery.Length--; 
            return state with { InLineComment = true, PrevChar = '\0' };
        }
        
        if (c == ';')
        {
            ProcessCompleteQuery(currentQuery, results, cancellationToken);
            return state with { PrevChar = '\0' };
        }
        
        currentQuery.Append(c);
        return state with { PrevChar = c, Escape = false };
    }

    private void ProcessCompleteQuery(StringBuilder currentQuery, List<string> results, CancellationToken cancellationToken)
    {
        var query = currentQuery.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var content = ExecuteAndProcessQueryAsync(query, cancellationToken).GetAwaiter().GetResult();
            results.Add(content);
        }
        currentQuery.Clear();
    }

    private void ProcessRemainingQuery(StringBuilder currentQuery, List<string> results, CancellationToken cancellationToken)
    {
        var lastQuery = currentQuery.ToString().Trim();
        if (string.IsNullOrWhiteSpace(lastQuery)) return;
        var content = ExecuteAndProcessQueryAsync(lastQuery, cancellationToken).GetAwaiter().GetResult();
        results.Add(content);
    }

   
    private async Task<string> ExecuteAndProcessQueryAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            var response = await ProcessSqlQuery(query, cancellationToken);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (SqlParseException ex)
        {
            _logger.LogError(ex, "Failed to parse SQL query: {Query}", query);
            return $"Error: {ex.Message}";
        }
    }

    private async Task<HttpResponseMessage> ProcessSqlQuery(string sql, CancellationToken cancellationToken)
    {
        var model = await _querySqlParser.ParseAsync(sql, cancellationToken);
        
        if (model is InsertModel insertModel)
        {
            Console.WriteLine(insertModel.TableName);
            Console.Write("[ ");
            insertModel.ColumnNames.ForEach(c => Console.Write(c + ", "));
            Console.WriteLine(" ]");
        
            foreach (var column in insertModel.Rows)
            { 
               Console.Write("[ ");
               column.Values.ForEach(c => Console.Write(c + ", "));
               Console.WriteLine(" ]");
            }
            
            
        }

        if (model is CreateTableModel tableModel)
        {
            Console.WriteLine(tableModel.TableName);
            foreach (var column in tableModel.Columns)
            {
                Console.WriteLine(column.ColumnName);
                Console.WriteLine(column.Default);
                Console.WriteLine(column.DataType);
                Console.WriteLine(column.IsNullable);
            }
            Console.WriteLine(tableModel.QueryResultType);
       
        }
        
        return await _queryProcessor.ProcessAsync(model, cancellationToken);
    }
}