using System;
using System.Collections.Generic;
using System.Text;

namespace DbAccessCodeGen.Templates
{
    internal static class DefaultTemplates
    {
        // Sync this with the template files. 
        // EmbeddedResource does not seem to work, so this is required

        internal const string ModelFileTemplate =
@"
namespace {{Name.Namespace}}
{
    public partial class {{Name.Name}} 
    {
        {{ for prop in Properties }}
        public {{prop.CompleteNetType}} {{prop.CSPropertyName}} { get; }{{ end }}

        public {{Name.Name}}({{ for prop in Properties }}{{prop.CompleteNetType}} {{prop.ParameterName}}{{if !for.last}}, {{ end }}{{ end }})
        {
        {{ for prop in Properties }}
            this.{{prop.CSPropertyName}} = {{prop.ParameterName}};{{ end }}
        }
    }

}";

        internal const string ServiceClassTemplate =
@"using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace {{Name.Namespace}} 
{
	public partial class {{Name.Name}}
	{
		protected readonly DbConnection connection;
        protected readonly bool isMSSqlServer;

		public {{Name.Name}}(DbConnection connection)
		{
			this.connection = connection;
            this.isMSSqlServer = connection.GetType().FullName.StartsWith(""System.Data.SqlClient"")
                || connection.GetType().FullName.StartsWith(""Microsoft.Data.SqlClient"");
		}

		partial void OnCommandStart(DbCommand cmd, DateTime startedAt);
		partial void OnCommandEnd(DbCommand cmd, DateTime startedAt);

        private void AddCommandParameter<T>(DbCommand cmd, string name, T value, ParameterDirection direction) 
        {
		    var prm = cmd.CreateParameter();
            prm.Direction = direction;
            prm.ParameterName = (isMSSqlServer && !name.StartsWith(""@"") ? ""@"" : """") +  name;
            prm.Value = (value as object) ?? DBNull.Value;
            
            if (typeof(T) == typeof(byte[]))
            {
                prm.DbType = DbType.Binary;
            }
            cmd.Parameters.Add(prm);
        }

        private Dictionary<string, object?> ReadDictionary(IDataRecord record, int fieldCount) 
        {
            Dictionary<string, object?> rowData = new Dictionary<string, object?>(fieldCount);
            for(int i = 0; i<fieldCount; i++) 
            {
                rowData[record.GetName(i)] = record.IsDBNull(i) ? (object?)null : record.GetValue(i);
            }
            return rowData;
        }
{{Methods}}
	}

}";

        internal const string ServiceMethodTemplate =
@"{{if ResultFields}}
protected readonly struct {{MethodName}}Ordinals
{
	{{ for res in ResultFields }}public readonly int {{ res.CSPropertyName }};
	{{ end }}	

	public {{MethodName}}Ordinals({{ for prop in ResultFields }}int {{prop.ParameterName}}{{if !for.last}}, {{ end }}{{ end }})
	{
		{{ for prop in ResultFields }}{{prop.CSPropertyName}} = {{prop.ParameterName}};
		{{ end }}		
	}
}

protected {{ResultType}} {{MethodName}}_FromRecord(IDataRecord row, in {{MethodName}}Ordinals ordinals) 
{
	return new {{ResultType}}(
		{{ for prop in ResultFields }}{{prop.ParameterName}}: {{prop.GetCode}}{{if !for.last}}, {{ end }}{{ end }}
		);
}
{{end}}

{{if GenerateAsyncCode}}
{{if ParameterTypeName}}
public IAsyncEnumerable<{{ResultType}}> {{MethodName}}Async ({{ for prop in Parameters }}{{prop.CompleteNetType}} {{prop.CSPropertyName}}{{if !for.last}}, {{ end }}{{end}})
{
	return {{MethodName}}Async(new {{ParameterTypeName}}({{ for prop in Parameters }}{{prop.ParameterName}}: {{prop.CSPropertyName}}{{if !for.last}}, {{ end }}
		{{end}}));
}
{{end}}

public async IAsyncEnumerable<{{ResultType}}> {{MethodName}}Async ({{if ParameterTypeName}} {{ParameterTypeName}} parameters{{end}})
{
    var cmd = connection.CreateCommand();
	if(connection.State != ConnectionState.Open) 
	{
		await connection.OpenAsync();
	}
	cmd.CommandType = CommandType.StoredProcedure;
	cmd.CommandText = ""{{SqlName}}"";
	{{ for prop in Parameters }}this.AddCommandParameter(cmd, ""{{prop.SqlName}}"", parameters.{{ prop.CSPropertyName }}, ParameterDirection.{{ prop.ParameterDirection }});
	{{ end }}
	DateTime start = DateTime.Now;
	OnCommandStart(cmd, start);
	using var rdr = await cmd.ExecuteReaderAsync();
	{{if ResultFields}}
	var ordinals = new {{MethodName}}Ordinals(
		{{ for prop in ResultFields }}{{prop.ParameterName}}: rdr.GetOrdinal(""{{prop.SqlName}}"") {{if !for.last}}, {{ end }}{{ end }}
	);
	{{else}}
	int fieldCount = rdr.FieldCount;
	{{end}}
	while(await rdr.ReadAsync())
	{
		{{if ResultFields}}yield return {{MethodName}}_FromRecord(rdr, ordinals);
		{{else}}yield return ReadDictionary(rdr, fieldCount);
		{{end}}
	}
	OnCommandEnd(cmd, start);
}
{{end}}
{{if GenerateSyncCode}}
{{if ParameterTypeName}}
public IEnumerable<{{ResultType}}> {{MethodName}} ({{ for prop in Parameters }}{{prop.CompleteNetType}} {{prop.CSPropertyName}}{{if !for.last}}, {{ end }}{{end}})
{
	return {{MethodName}}(new {{ParameterTypeName}}({{ for prop in Parameters }}{{prop.ParameterName}}: {{prop.CSPropertyName}}{{if !for.last}}, {{ end }}
	{{end}}));
}
{{end}}

public IEnumerable<{{ResultType}}> {{MethodName}} ({{if ParameterTypeName}}{{ParameterTypeName}} parameters{{end}})
{
    var cmd = connection.CreateCommand();
	if(connection.State != ConnectionState.Open) 
	{
		connection.Open();
	}
	cmd.CommandType = CommandType.StoredProcedure;
	cmd.CommandText = ""{{SqlName}}"";
	{{ for prop in Parameters }}this.AddCommandParameter(cmd, ""{{prop.SqlName}}"", parameters.{{ prop.CSPropertyName }}, ParameterDirection.{{ prop.ParameterDirection }});
	{{ end }}
	DateTime start = DateTime.Now;
	OnCommandStart(cmd, start);
	using var rdr = cmd.ExecuteReader();
	{{if ResultFields}}
	var ordinals = new {{MethodName}}Ordinals(
		{{ for prop in ResultFields }}{{prop.ParameterName}}: rdr.GetOrdinal(""{{prop.SqlName}}"") {{if !for.last}}, {{ end }}{{ end }}
	);
	{{else}}
	int fieldCount = rdr.FieldCount;
	{{end}}
	while(rdr.Read())
	{
		{{if ResultFields}}yield return {{MethodName}}_FromRecord(rdr, in ordinals);
		{{else}}yield return ReadDictionary(rdr, fieldCount);
		{{end}}
	}
	OnCommandEnd(cmd, start);
}
{{end}}";

    }
}
