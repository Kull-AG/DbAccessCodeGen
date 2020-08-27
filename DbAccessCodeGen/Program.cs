using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Kull.DatabaseMetadata;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using Microsoft.Extensions.Hosting;
using System.Linq;
using DbAccessCodeGen.Configuration;
using System.Net.Http.Headers;
using Mono.Options;

namespace DbAccessCodeGen
{
    class Program
    {
        static string? GetSubCommand(string[] args)
        {
            string? firstOption = args.FirstOrDefault(a => a.StartsWith("-"));
            int? firstOptionIndex = firstOption != null ? Array.IndexOf(args, firstOption) : (int?)null;
            if (firstOptionIndex == 0) return null;
            var firstParts = firstOptionIndex == null ? args : args.Take(firstOptionIndex.Value);
            var lastPart = firstParts.Last();
            if (lastPart.Contains("."))
                return null;
            return lastPart;
        }

        static async Task Main(string[] args)
        {

            if (!DbProviderFactories.TryGetFactory("System.Data.SqlClient", out var _))
                DbProviderFactories.RegisterFactory("System.Data.SqlClient", System.Data.SqlClient.SqlClientFactory.Instance);

            var subcommand = GetSubCommand(args);
            if (subcommand == null || subcommand == "generate")
            {
                FileInfo? configInfo = null;
                string? connectionString = null;
                bool show_help;
                var p = new OptionSet() {
                    "Usage: dbcodegen [OPTIONS]+",
                    "Greet a list of individuals with an optional message.",
                    "If no message is specified, a generic greeting is used.",
                    "",
                    "Options:",
                    { "c|config=", "the DbCodeGenConfig.yml location",
                      c => configInfo = new FileInfo(c) },
                    { "cs|connectionstring=",
                        "the connection string." ,
                      (cs) => connectionString=cs },
                    { "h|help",  "show this message and exit",
                      v => show_help = v != null },
                };
                try
                {
                    p.Parse(args);

                }
                catch (OptionException e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Try `--help' for more information.");
                    return;
                }

                await ExecuteCodeGen(configInfo ?? throw new ArgumentNullException(),
                    connectionString);
            }


        }

        public static async Task ExecuteCodeGen(FileInfo config, string? connectionString)
        {
            var content = await System.IO.File.ReadAllTextAsync(config.FullName);
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.NullNamingConvention.Instance)
                .Build();
            var settings = deserializer.Deserialize<Settings>(content);
            settings.ConnectionString = connectionString ?? settings.ConnectionString;
            if (settings.ConnectionString == null)
            {
                Console.Error.WriteLine("Must provide connection string");
                Environment.Exit(-1);
            }
            await ExecuteCodeGen(settings);
        }

        public static ServiceProvider RegisterServices(Configuration.Settings settings)
        {
            //setup our DI
            var services = new ServiceCollection();

            services.AddLogging(l =>
            {
                l.AddConsole();
                l.SetMinimumLevel(LogLevel.Debug);
            });
            services.AddKullDatabaseMetadata();
            services.AddSingleton(settings);
            services.AddSingleton<Configuration.NamingHandler, Configuration.NamingHandlerConfiguratable>();
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
            var sp = RegisterServices(settings);
            var executor = sp.GetRequiredService<CodeGen.Executor>();
            return executor.Execute();
        }
    }
}
