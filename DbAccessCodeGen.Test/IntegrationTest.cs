using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using CliWrap;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace DbAccessCodeGen.Test
{
    [TestClass]
    public class IntegrationTest
    {
        // Before running, increase version nr

        private TestContext testContextInstance;

        /// <summary>
        ///  Gets or sets the test context which provides
        ///  information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        [TestMethod]
        public void A_Delete()
        {
            //Returns the bin/Debug/ folder
            string solutionDir = GetSolutionDir();
            var pkgFolder = Path.Combine(solutionDir, "DbAccessCodeGen/nupkg");
            if (System.IO.Directory.Exists(pkgFolder))
                System.IO.Directory.Delete(pkgFolder, true);

        }

        [TestMethod]
        public async Task B_Pack()
        {
            await ExecuteAndAssertSuccess(Path.Combine(GetSolutionDir(), "DbAccessCodeGen"), 60, "dotnet", "pack");
        }

        [TestMethod]
        public async Task C_DbSetup()
        {
            var solutionDir = GetSolutionDir();
            // Restore Tools Reference
            await ExecuteAndAssertSuccess(Path.Combine(solutionDir, "DbCode.Test"), 30,
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "powershell" : "pwsh", "-File", "SetupDB.ps1", "-ExecutionPolicy",
                    "Unrestricted");
        }


        [TestMethod]
        public async Task D_Build_Tool()
        {
            var solutionDir = GetSolutionDir();

            await ExecuteAndAssertSuccess(Path.Combine(solutionDir, "DbAccessCodeGen"), 30, "dotnet", "build","-c","Debug");
        }

        [TestMethod]
        public async Task E_CodeGen()
        {
            var solutionDir = GetSolutionDir();

            await ExecuteAndAssertSuccess(Path.Combine(solutionDir, "DbCode.Test"), 60, Path.Combine(solutionDir, "DbAccessCodeGen/bin/Debug/net6/DbAccessCodeGen.exe"),
                "-c", "DbCodeGenConfig.yml");
        }

        [TestMethod]
        public async Task F_Build()
        {
            var solutionDir = GetSolutionDir();

            await ExecuteAndAssertSuccess(Path.Combine(solutionDir, "DbCode.Test"), 30, "dotnet", "build");
        }

        [TestMethod]
        public async Task G_Run()
        {
            var solutionDir = GetSolutionDir();

            await ExecuteAndAssertSuccess(Path.Combine(solutionDir, "DbCode.Test"), 60, "dotnet", "run", "--framework", "netcoreapp3.1");
        }



        private static string GetSolutionDir()
        {
            var currentDir = System.IO.Directory.GetCurrentDirectory().Replace("\\", "/");
            var projectDir = currentDir.Substring(0, currentDir.IndexOf("/bin/Debug"));

            var solutionDir = System.IO.Path.GetDirectoryName(projectDir);
            return solutionDir;
        }

        private async Task ExecuteAndAssertSuccess(string workingDir, int maxRunTimeInS, string exe, params string[] args)
        {
            List<string> lines = new List<string>();
            var packExe = Cli.Wrap(exe).WithArguments(args)
                .WithWorkingDirectory(workingDir)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(h =>
                {
                    TestContext.WriteLine(h);
                }))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(h =>
                {
                    TestContext.WriteLine("ERROR: " + h);
                    lines.Add(h);
                }))
                .WithValidation(CommandResultValidation.None); // We validate our-selves
            var cts = new CancellationTokenSource();
            cts.CancelAfter(maxRunTimeInS * 1000);
            var generateresult = await packExe
                .ExecuteAsync(cts.Token);
            Assert.AreEqual(string.Join(Environment.NewLine, lines), "");
            Assert.AreEqual(generateresult.ExitCode, 0);
        }

    }
}
