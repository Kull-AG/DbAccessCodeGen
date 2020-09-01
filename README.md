# Database Code Generator for C#

This project aims to generate low-level .Net Code to access a database. 

- Does not use Reflection at Runtime. Is therefore very fast
- The generated code relies on IAsyncEnumerable by default for async code, therefore you will need Microsot.Bcl.AsyncInterfaces for targets below .Net Core 3.0.
For all platforms, System.Linq.Async might be helpful
- It uses Scriban as a template language which is very powerful, making it simple to Customize
- Currently only Stored Procedures are supported and only MS SQL Server works realiably.
- Generated Code requires C# 8.0. This can easily be set in the project settings and allows for certain great features like Nullable Context

## Requirements / configuration

Create a file called DbCodeGenConfig.yml and configure your must important settings:

```yaml
---
GenerateAsyncCode: true
GenerateSyncCode: false
TemplateDir: "./Templates"
OutputDir: "../DbCode.Test/DbAccess"
ServiceClassName: "DataEntities"
Namespace: DbCode.Test
Procedures:
- bfm.spGetFinancialPeriods
- bfm.spGetSaldo
- bfm.spGetForeCastData
- bfm.spGetPlanningSubTypes
- bfm.spGetBudgetData
- bfm.spGetBudgetDataV2
- bfm.spGetForeCastDataV2

```

The template dir allows you to overwrite the templates used, the default ones are in the [DbAccessCodeGen/Templates](DbAccessCodeGen/Templates) folder.

You must provide the config location via --config parameter

In Addition you have to provide a connection string somehow. One way is to just add "ConnectionString" to the Settings file above, the other way is to 
use the --connectionString Parameter . Always be sure not to include credentials in a config if you should not

## What the code looks like 

There are two files per Procedure (one for the parameters, one for the result) and a Service Class for all procedures. 

The parameters and result files are POCO's, however they are immutable:

```C#
public partial class DogGetParameters 
    {
        
        public System.DateTime? DateFrom { get; }
        public System.DateTime? DateTo { get; }
        public string? Identity { get; }
        public bool? OnlyWithData { get; }

        public DogGetParameters(System.DateTime? dateFrom, System.DateTime? dateTo, string? identity, bool? onlyWithData)
        {
        
            this.DateFrom = dateFrom;
            this.DateTo = dateTo;
            this.Identity = identity;
            this.OnlyWithData = onlyWithData;
        }
    }
```

They parameters are always assumed to be nullable, the results are nullable only if indicated by the database.

The generated service method look something like this :

```C#

    protected DbCode.Test.SpGetFinancialPeriodsResult spGetFinancialPeriods_FromRecord(IDataRecord row, in spGetFinancialPeriodsOrdinals ordinals) 
    {
        return new DbCode.Test.SpGetFinancialPeriodsResult(
            finPeriod: row.GetString(ordinals.FinPeriod), isDefault: row.IsDBNull(ordinals.IsDefault) ? (bool?)null : row.GetBoolean(ordinals.IsDefault), firstInPeriod: (System.DateTime)row.GetValue(ordinals.FirstInPeriod), lastInPeriod: (System.DateTime)row.GetValue(ordinals.LastInPeriod)
            );
    }
    
    
    
    
    public IAsyncEnumerable<DbCode.Test.SpGetFinancialPeriodsResult> spGetFinancialPeriodsAsync (string? Identity, string? TenantId)
    {
        return spGetFinancialPeriodsAsync(new DbCode.Test.SpGetFinancialPeriodsParameters(identity: Identity, 
            tenantId: TenantId
            ));
    }
    
    
    public async IAsyncEnumerable<DbCode.Test.SpGetFinancialPeriodsResult> spGetFinancialPeriodsAsync ( DbCode.Test.SpGetFinancialPeriodsParameters parameters)
    {
        var cmd = connection.CreateCommand();
        if(connection.State != ConnectionState.Open) 
        {
            await connection.OpenAsync();
        }
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "bfm.spGetFinancialPeriods";
        this.AddCommandParameter(cmd, "Identity", parameters.Identity, ParameterDirection.Input);
        this.AddCommandParameter(cmd, "TenantId", parameters.TenantId, ParameterDirection.Input);
        
        DateTime start = DateTime.Now;
        OnCommandStart(cmd, start);
        using var rdr = await cmd.ExecuteReaderAsync();
        
        var ordinals = new spGetFinancialPeriodsOrdinals(
            finPeriod: rdr.GetOrdinal("FinPeriod") , isDefault: rdr.GetOrdinal("IsDefault") , firstInPeriod: rdr.GetOrdinal("FirstInPeriod") , lastInPeriod: rdr.GetOrdinal("LastInPeriod") 
        );
        
        while(await rdr.ReadAsync()),
        {
            yield return spGetFinancialPeriods_FromRecord(rdr, ordinals);
            
        }
        OnCommandEnd(cmd, start);
        return results;
    }
    
```

You can configure to generate non-async code. As you can see, you get two overloads for the function, one with an Object as Parameter and the otherone with just plain parameters.
It depends a lot on your use case which one is better suited.

## Customization

You can fully customize the Templates and the naming Convention. 
To customize the templates, provide a TemplateDir in the Settings yaml and download [the Templates in the project](DbAccessCodeGen/Templates). There are three templates currently:

- ModelFile: Code for a parameters/result class
- ServiceMethod: Code for a single method in the Service class
- ServiceClass: The service class itself.

The generated code does by default alread expose two partial Methods you can use to customize things like Logging / Command Timeout etc:

```C#
        partial void OnCommandStart(DbCommand cmd, DateTime startedAt)
        {
            cmd.CommandTimeout = this.CommandTimeout;
        }

        partial void OnCommandEnd(DbCommand cmd, DateTime startedAt)
        {
            
        }
```    

The most advanced use case currently is customizing naming convetion. You can set the NamingJS Value in the Yaml file that points to a JavaScript file which allows overwriting all methods of the [NamingHandler](DbAccessCodeGen/Configuration/NamingHandler.cs). An example can be found in the [Test project](DbCode.Test/naming.js)

This is yet to be documented, however the Tests/project provides examples.
