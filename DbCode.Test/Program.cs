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

            var props = typeof(spGetPetsResult).GetProperties(System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Select(s => s.Name).ToArray();
            if (props.Length == 0)
                throw new Exception("Expected to have properties");
            if (props.Contains("IsNice", StringComparer.InvariantCultureIgnoreCase))
                throw new Exception("Expected not to have IsNice property, as it's ignored");
            DataAccessor dba = new DataAccessor(new Microsoft.Data.SqlClient.SqlConnection(conStr));
            var res = (await dba.spGetPetsAsync(false, "", "::1")).ToList();
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(res);
            Console.WriteLine(json);
            Console.WriteLine("----------------------");
            var res2 = await dba.spTestBackendStreamAsync(1, new UDT.IdNameType[] { new UDT.IdNameType(23, "tester"), new UDT.IdNameType(12, "3425") }).ToListAsync();
            var json2 = Newtonsoft.Json.JsonConvert.SerializeObject(res2);
            Console.WriteLine(json2);

            var res3 = (await dba.spTestExecuteParamsAsync(3)).ToArray();
            if (res3.Length != 1 || res3[0].TestId != 3)
            {
                Console.Error.WriteLine("FAILED TEST for spTestExecuteParamsAsync");
                Environment.ExitCode = -1;
            }
            else
            {

            }
            int a = dba.spReturnsNothing(1).ReturnValue;
            if (a != 1)
            {
                Console.Error.WriteLine("FAILED TEST for spReturnsNothing");
                Environment.ExitCode = -2;
            }
            var pets = dba.GetPets().ToArray();
            if (pets.Length != 2)
            {
                Console.Error.WriteLine("FAILED TEST for GetPets");
                Environment.ExitCode = -2;
            }
        }
    }
}
