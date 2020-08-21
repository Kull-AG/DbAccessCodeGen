using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;
using Kull.DatabaseMetadata;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using Microsoft.Extensions.Hosting;

namespace DbAccessCodeGen
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!DbProviderFactories.TryGetFactory("System.Data.SqlClient", out var _))
                DbProviderFactories.RegisterFactory("System.Data.SqlClient", System.Data.SqlClient.SqlClientFactory.Instance);

            var rootCommand = new RootCommand
    {
        new Option<FileInfo>(
            new string[]{"--config", "-c" },
            description: "The config file")
        {
            Argument = new Argument<FileInfo>().ExistingOnly()
        },
        new Option<string> (
            new string[] {"--connectionString", "cs"},
            description: "The connection string"
            )
        }
            ;
            rootCommand.Handler = CommandHandler.Create<FileInfo, string>(async (config, connectionString) =>
            {
                var content = await System.IO.File.ReadAllTextAsync(config.FullName);
                var settings = JsonSerializer.Deserialize<Configuration.Settings>(content);
                settings.ConnectionString = connectionString ?? settings.ConnectionString;
                await ExecuteCodeGen(settings);
            });

            var r = rootCommand.InvokeAsync(args).Result;
            Environment.ExitCode = r;

        }

        public static ServiceProvider RegisterServices(Configuration.Settings settings)
        {
            //setup our DI
            var services = new ServiceCollection();

            services.AddLogging(l=>
            {
                l.AddConsole();
                l.SetMinimumLevel(LogLevel.Debug);
            });
            services.AddKullDatabaseMetadata();
            services.AddSingleton(settings);
            services.AddSingleton<Configuration.NamingHandler>();
            services.AddTransient<CodeGen.Executor>();
            services.AddTransient<CodeGen.CodeGenHandler>();
            services.AddTransient<CodeGen.SqlTypeMapper>();
            services.AddSingleton<IHostingEnvironment, Configuration.DummyHosingEnvironment>();
            services.AddScoped<DbConnection>(_ =>
            {
                return Kull.Data.DatabaseUtils.GetConnectionFromEFString(settings.ConnectionString, true);
            });

            return services.BuildServiceProvider();

        }

        public static Task ExecuteCodeGen(Configuration.Settings settings)
        {
            var sp =RegisterServices(settings);
            var executor = sp.GetRequiredService<CodeGen.Executor>();
            return executor.Execute();
        }
    }
}
