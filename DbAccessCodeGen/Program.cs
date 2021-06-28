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
            Kull.Data.DatabaseUtils.UseNewMSSqlClient = true;
            if (!DbProviderFactories.TryGetFactory("Microsoft.Data.SqlClient", out var _))
                DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance);

            var subcommand = GetSubCommand(args);
            if (subcommand == null || subcommand == "generate")
            {
                FileInfo? configInfo = null;
                string? connectionString = null;
                bool show_help = false;
                var p = new OptionSet() {
                    "Usage: dbcodegen [OPTIONS]+",
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
                if (show_help)
                {

                    p.WriteOptionDescriptions(Console.Out);
                    Console.WriteLine("You can also you the subcommands init and migrateef if you want:");
                    Console.WriteLine("Eg type `dbcodegen init -h` for more info");
                    return;
                }
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
            else if (subcommand == "init")
            {
                var argsReal = args.Skip(Array.IndexOf(args, "init")).ToArray();
                string configfile = "DbCodeGenConfig.yml";
                bool show_help = false;
                var p = new OptionSet() {
                    "Usage: init [OPTIONS]+",
                    "",
                    "Options:",
                    { "c|config=", "the DbCodeGenConfig.yml location",
                      c => configfile = c},
                    { "h|help",  "show this message and exit",
                      v => show_help = v != null },
                };
                if (show_help)
                {
                    p.WriteOptionDescriptions(Console.Out);
                    return;
                }
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
                await ExecuteInit(configfile);
            }
            else if (subcommand == "migrateef")
            {
                var argsReal = args.Skip(Array.IndexOf(args, "migrateef")).ToArray();
                string? edmxFile = null;
                string? outDir = null;
                bool show_help = false;
                var p = new OptionSet() {
                    "Usage: migrateef [OPTIONS]+",
                    "",
                    "Options:",
                    { "e|edmxfile=", "the *.edmx file location",
                      c => edmxFile = c},
                    { "o|outputdirectory=",
                        "the output directory" ,
                      (o) => outDir=o },
                    { "h|help",  "show this message and exit",
                      v => show_help = v != null },
                };
                if (show_help)
                {
                    p.WriteOptionDescriptions(Console.Out);
                    return;
                }
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
                if (edmxFile == null)
                {
                    Console.Error.WriteLine("Must provide edmx File (-e)");
                    p.WriteOptionDescriptions(Console.Out);
                    Environment.Exit(-1);
                }
                if (outDir == null)
                {
                    Console.Error.WriteLine("Must provide out dir");
                    p.WriteOptionDescriptions(Console.Out);
                    Environment.Exit(-1);
                }
                await ExecuteMigrateEF(edmxFile, outDir);
            }
            else
            {
                Console.Error.Write(subcommand + " is not a valid subcommand. Valid are: generate migrateef");
                Environment.Exit(-1);
            }
        }

        public static Task ExecuteMigrateEF(string edmxFile, string outDir)
        {
            var sp = RegisterServices4Migrate();
            var executor = sp.GetRequiredService<EFMigrate.Migrator>();
            return executor.Execute(edmxFile, outDir);
        }

        public static Task ExecuteInit(string configFilePath)
        {
            var sp = RegisterServices4Init();
            Console.WriteLine("Enter namespace:");
            string? @namespace = Console.ReadLine();
            Console.WriteLine("Enter Server (default localhost):");
            string? server = Console.ReadLine();
            if (String.IsNullOrEmpty(server))
                server = "localhost";
            Console.WriteLine("Enter Databasename:");
            string? db = Console.ReadLine();
            string template = Templates.TemplateRetrieval.GetTemplate("DbCodeGenConfig");
            template = template.Replace("{{Namespace}}", @namespace ?? "Enter.your.Namespace");
            template = template.Replace("{{server}}", server);
            template = template.Replace("{{db}}", db ?? "testdb");
            System.IO.File.WriteAllText(configFilePath, template);

            var runTool = @"dotnet tool restore
dotnet tool run dbcodegen -c DbCodeGenConfig.yml";
            var path = System.IO.Path.GetDirectoryName(configFilePath)!;
            System.IO.File.WriteAllText(Path.Combine(path, "rundbcodegen.bat"), runTool, System.Text.Encoding.ASCII);
            System.IO.File.WriteAllText(Path.Combine(path, "rundbcodegen.sh"), runTool, System.Text.Encoding.ASCII);

            // TODO: Testing
            return Task.CompletedTask;
        }

        public static async Task ExecuteCodeGen(FileInfo config, string? connectionString)
        {
            var content = await System.IO.File.ReadAllTextAsync(config.FullName);
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.NullNamingConvention.Instance)
                .Build();
            var obj = deserializer.Deserialize<object>(content);
            var settings = Settings.FromObject(obj);
            settings = settings with { ConnectionString = connectionString ?? settings.ConnectionString };
            if (settings.ConnectionString == null)
            {
                Console.Error.WriteLine("Must provide connection string");
                Environment.Exit(-1);
            }
            await ExecuteCodeGen(settings);
        }

        public static ServiceProvider RegisterServices4Init()
        {
            var services = new ServiceCollection();

            services.AddLogging(l =>
            {
                l.AddConsole();
                l.SetMinimumLevel(LogLevel.Debug);
            });
            return services.BuildServiceProvider();
        }
        public static ServiceProvider RegisterServices4Migrate()
        {
            var services = new ServiceCollection();

            services.AddLogging(l =>
            {
                l.AddConsole();
                l.SetMinimumLevel(LogLevel.Debug);
            });
            services.AddSingleton<EFMigrate.EdmTypes>();
            services.AddTransient<EFMigrate.Migrator>();

            return services.BuildServiceProvider();
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
                return Kull.Data.DatabaseUtils.GetConnectionFromEFString(settings.ConnectionString!, true);
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
