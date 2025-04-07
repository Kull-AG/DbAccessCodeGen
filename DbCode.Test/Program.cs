using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

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
            string conStr = "Server=127.0.0.1;user id=sa;password=abcDEF123#;Initial Catalog=CodeGenTestDb;MultipleActiveResultSets=true;TrustServerCertificate=True;";

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
            var es = dba.spSearchPets2("b");
            var pn = es.FirstOrDefault()?.PetName;// If it compiles it' ok
            var res3 = (await dba.spTestExecuteParamsAsync(3)).ToArray();
            dba.UpdateThatPet(1, new byte[] {});
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
            string test = (string)dba.spReturnAsDict().ToArray().Single()["Test"];
            if(test != "hallo")
            {
                Console.Error.WriteLine("FAILED TEST for spReturnAsDict");
                Environment.ExitCode = -2;
            }
            using var rdr = dba.spReturnAsReader();
            rdr.Read();
            string test2 = rdr.GetString(rdr.GetOrdinal("Test"));
            if (test2 != "hallo")
            {
                Console.Error.WriteLine("FAILED TEST for spReturnAsReader");
                Environment.ExitCode = -2;
            }

            //Testing with specific naming of sp
            var rdr2 = dba.spReturnDataDelivery().ToArray();
            if (rdr2.Length != 1)
            {
                Console.Error.WriteLine("FAILED TEST for Sales.DataDelivery.spReturnDataDelivery");
                Console.Error.WriteLine($"Count of entries {rdr2.Length}");
                Environment.ExitCode = -2;
            }
        }
    }
}
