using System.Reflection;
using Babrat.Server.CalciteParser.Settings;
using Babrat.Server.Core;
using DryIoc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using org.apache.calcite.config;
using org.apache.calcite.sql.parser;
using org.apache.calcite.sql.parser.ddl;
using org.apache.calcite.sql.parser.impl;
using org.apache.calcite.sql.validate;

namespace Babrat.Server.CalciteParser;

public sealed class ServiceRegistrator:
    IServiceRegistrator
{
    
    #region RGU.WebProgramming.Server.Core.IServiceRegistrator implementation
    
    /// <inheritdoc cref="IServiceRegistrator.Register" />
    public void Register(
        IRegistrator registrator,
        IConfiguration configuration)
    {
        
        var calciteSettings = new CalciteSettings();
        configuration.Bind(nameof(CalciteSettings), calciteSettings);

        registrator.RegisterInstance(BuildSqlParserConfig(calciteSettings));
        
        registrator.Register<IQuerySqlParser, CalciteQueryParser>(Reuse.Singleton);

       
    }
    
    #endregion
    
    #region Private Methods

    private SqlParser.Config BuildSqlParserConfig(CalciteSettings settings)
    {
        
        // preload logging provider for clear calcite warning
        Assembly.Load("slf4j.simple");
        
        var parserFactory = settings.UseDdl
            ? SqlDdlParserImpl.FACTORY
            : SqlParserImpl.FACTORY;
      
        
        var lex = settings.Lex?.ToUpperInvariant() switch
        {
            "JAVA" => Lex.JAVA,
            "MYSQL" => Lex.MYSQL,
            "ORACLE" => Lex.ORACLE,
            "SQL_SERVER" => Lex.SQL_SERVER,
            _ => Lex.MYSQL
        };

        var conformance = settings.Conformance?.ToUpperInvariant() switch
        {
            "MYSQL_5" => SqlConformanceEnum.MYSQL_5,
            "DEFAULT" => SqlConformanceEnum.DEFAULT,
            "STRICT_92" => SqlConformanceEnum.STRICT_92,
            "LENIENT" => SqlConformanceEnum.LENIENT,
            _ => SqlConformanceEnum.MYSQL_5
        };

        return SqlParser.config()
            .withParserFactory(parserFactory)
            .withConformance(conformance)
            .withLex(lex);
    }
    #endregion
    
}