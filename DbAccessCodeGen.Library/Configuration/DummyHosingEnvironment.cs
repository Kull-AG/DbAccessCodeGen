using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace DbAccessCodeGen.Configuration
{
    public class DummyHosingEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environment.GetEnvironmentVariable("Environment")??"";
        public string ApplicationName { get; set; } = "DbAccessCodeGen";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
