# Database Code Generator for C#

This project aims to generate low-level .Net Code to access a database. 

- Does not use Reflection at Runtime. Is therefore very fast
- The generated code relies on IAsyncEnumerable by default for async code, therefore you will need Microsot.Bcl.AsyncInterfaces for targets below .Net Core 3.0.
For all platforms, System.Linq.Async might be helpful
- It uses Scriban as a template language which is very powerful, making it simple to Customize
- Currently only Stored Procedures are supported and only MS SQL Server works realiably.
- Generated Code requires C# 8.0. This can easily be set in the project settings and allows for certain great features like Nullable Context

## Why you should use it

- Great for AOT (like [Xamarin](https://www.xamarinhelp.com/xamarin-android-aot-works/)) and [App Trimming](https://devblogs.microsoft.com/dotnet/app-trimming-in-net-5/). Generated Code does not need Reflection
- No need for Entity Framework
- It's really fast in generating code
- The generated code is very fast (Tests pending) as it uses Low-Level .Net Code

## Requirements / configuration

Install it via as a [tool](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install) via nuget:

[![NuGet Badge](https://buildstats.info/nuget/DbAccessCodeGen)](https://www.nuget.org/packages/DbAccessCodeGen/)

Create a file called DbCodeGenConfig.yml and configure your must important settings:

```yaml
---
GenerateAsyncCode: true # Generate Code that returns Task<IEnumerable<T>> 
GenerateStreamAsyncCode: false # Generate Code that returns IAsyncEnumerable<T>
GenerateSyncCode: false # Generate Code that returns IEnumerable<T>
TemplateDir: "./Templates"
OutputDir: "../DbCode.Test/DbAccess"
ServiceClassName: "DataEntities"
Namespace: DbCode.Test
Procedures:
- bfm.spGetFinancialPeriods
- bfm.spGetSaldo
- bfm.spGetForeCastData
- bfm.spGetPlanningSubTypes
- SP: bfm.spGetBudgetData
  GenerateStreamAsyncCode: true # Overwrites default of false
- bfm.spGetBudgetDataV2
- SP: spTestExecuteParams  
  ExecuteParameters: # Execute  if not otherwise possible to get metadata
    testId: 1
  IgnoreParametes: # Do not generate code for the following parameters. Those MUST have Default values in the database
    - ObsoleteParameterName
    - VeryOldParameter
- bfm.spGetForeCastDataV2

```

The template dir allows you to overwrite the templates used, the default ones are in the [DbAccessCodeGen/Templates](DbAccessCodeGen/Templates) folder.

You must provide the config location via -c / --config parameter, therefore a possible command looks like this:
```dotnet tool run dbcodegen -c DbConfig.yml```

In Addition you have to provide a connection string somehow. One way is to just add "ConnectionString" to the Settings file above, the other way is to 
use the --connectionString Parameter . Always be sure not to include credentials in a config if you should not

## What the (default) generated code looks like 

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

    public Task<IEnumerable<DbCode.Test.spAddPetResult>> spAddPetAsync (string? petName, bool? isNice)
    {
        return spAddPetAsync(new DbCode.Test.spAddPetParameters(petName: petName, 
        isNice: isNice
        ));
    }

    public async Task<IEnumerable<DbCode.Test.spAddPetResult>> spAddPetAsync (DbCode.Test.spAddPetParameters parameters)
    {
        using var cmd = connection.CreateCommand();
        if(connection.State != ConnectionState.Open) 
        {
            await connection.OpenAsync();
        }
        spAddPet_PrepareCommand(cmd, parameters);
        DateTime start = DateTime.Now;
        OnCommandStart(cmd, start);
        var rdr = await cmd.ExecuteReaderAsync();
        var dt = spAddPet_FromReader(rdr);
        return AfterEnumerable(dt, () => OnCommandEnd(cmd, start), cmd, rdr);
    }

    private void spAddPet_PrepareCommand(DbCommand cmd, DbCode.Test.spAddPetParameters parameters)
    {
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.spAddPet";
            
        this.AddCommandParameter(cmd, "PetName", parameters.PetName, ParameterDirection.Input);
        this.AddCommandParameter(cmd, "IsNice", parameters.IsNice, ParameterDirection.Input);
    }
    
    private IEnumerable<DbCode.Test.spAddPetResult> spAddPet_FromReader(DbDataReader rdr)
    {
        if (!rdr.HasRows) 
        {
            yield break;
        }
            
        var ordinals = new spAddPetOrdinals(
            success: rdr.GetOrdinal("Success") , newPetId: rdr.GetOrdinal("NewPetId") 
        );
            
        while(rdr.Read())
        {
            yield return spAddPet_FromRecord(rdr, in ordinals);                
        }
    } 

    protected DbCode.Test.spAddPetResult spAddPet_FromRecord(IDataRecord row, in spAddPetOrdinals ordinals) 
    {
        return new DbCode.Test.spAddPetResult(
            success: row.IsDBNull(ordinals.Success) ? (bool?)null : row.GetBoolean(ordinals.Success), newPetId: row.IsDBNull(ordinals.NewPetId) ? throw new NullReferenceException("NewPetId") : row.GetInt32(ordinals.NewPetId)
            );
    }
    
```

You can configure to generate async, sync or stream-async code. All versions use IEnumerable or IAsyncEnumerable and assume that you fully enumerate the list (once and only once).

As you can see, you get two overloads for the function, one with an Object as Parameter and the otherone with just plain parameters.
It depends a lot on your use case which one is better suited.

## Customization

You can fully customize the Templates and the naming Convention. 
To customize the templates, provide a TemplateDir in the Settings yaml and download [the Templates in the project](DbAccessCodeGen/Templates). There are three templates currently:

- ModelFile: Code for a parameters/result class
- ServiceMethod: Code for a single method in the Service class
- ServiceClass: The service class itself.

The generated code does by default already expose two partial Methods you can use to customize things like Logging / Command Timeout etc:

```C#
        partial void OnCommandStart(DbCommand cmd, DateTime startedAt)
        {
            cmd.CommandTimeout = this.CommandTimeout;
        }

        partial void OnCommandEnd(DbCommand cmd, DateTime startedAt)
        {
            
        }
```    

The most advanced use case currently is customizing naming convention. You can set the NamingJS Value in the Yaml file that points to a JavaScript file which allows overwriting all methods of the [NamingHandler](DbAccessCodeGen/Configuration/NamingHandler.cs). An example can be found in the [Test project](DbCode.Test/naming.js)
