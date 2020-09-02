using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace DbCode.Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
#if !NET48
            if (!DbProviderFactories.TryGetFactory("Microsoft.Data.SqlClient", out var _))
                DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance);
#endif
            string conStr = "Server=(LocalDB)\\MSSQLLocalDB; Integrated Security=true;Initial Catalog=CodeGenTestDb;MultipleActiveResultSets=true";
            

            DataAccessor dba = new DataAccessor(new Microsoft.Data.SqlClient.SqlConnection(conStr));
            var res = await dba.spGetPetsAsync(false, "", "::1").ToListAsync();
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(res);
            Console.WriteLine(json);
            Console.WriteLine("----------------------");
            var res2 = await dba.spTestBackendAsync(1, new UDT.IdNameType[] { new UDT.IdNameType(23, "tester"), new UDT.IdNameType(12, "3425") }).ToListAsync();
            var json2 = Newtonsoft.Json.JsonConvert.SerializeObject(res2);
            Console.WriteLine(json2);

        }
    }
}
