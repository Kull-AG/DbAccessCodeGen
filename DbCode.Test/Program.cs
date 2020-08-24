using System;
using System.Data.Common;
using System.Linq;

namespace DbCode.Test
{
    class Program
    {
        static void Main(string[] args)
        {
#if !NET48
            if (!DbProviderFactories.TryGetFactory("Microsoft.Data.SqlClient", out var _))
                DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance);
#endif
            string conStr = "Server=(LocalDB)\\MSSQLLocalDB; Integrated Security=true;Initial Catalog=CodeGenTestDb;MultipleActiveResultSets=true";
            var rootPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            Utils.DatabaseUtils.SetupDb(rootPath, conStr);

            DataAccessor dba = new DataAccessor(new Microsoft.Data.SqlClient.SqlConnection(conStr));
            var res = dba.spGetPetsAsync(false, "", "::1").ToListAsync().Result;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(res);
            Console.WriteLine(json);
        }
    }
}
