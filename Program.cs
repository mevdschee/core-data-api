using System.Text;
using System.IO;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
            .UseUrls("http://localhost:8000/")
            .UseKestrel()
            .UseStartup<Startup>()
            .Build();
 
            host.Run();
        }
    }

    public class Startup
    {
        public void Configure(IApplicationBuilder app){
            app.Run(this.handler);
        }

        public Task handler(HttpContext context) {

            // get the HTTP method, path and body of the request
            string method = context.Request.Method;
            string[] request = context.Request.Path.ToString().Trim('/').Split('/');
            
            int length = ((int?)context.Request.ContentLength) ?? default(int);
            string data = Encoding.UTF8.GetString ((new BinaryReader(context.Request.Body)).ReadBytes(length));
            Dictionary<string,object> input = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);

            // connect to the sql server database
            MySqlConnection link = new MySqlConnection("addr=localhost;uid=php-crud-api;pwd=php-crud-api;database=php-crud-api;SslMode=None");
            link.Open();

            // retrieve the table and key from the path
            string table = Regex.Replace(request[0], "[^a-z0-9_]+", "");
            int key = request.Length>1 ? int.Parse(request[1]) : 0;

            // escape the columns from the input object
            string[] columns = input!=null ? input.Keys.Select(i => Regex.Replace(i.ToString(), "[^a-z0-9_]+", "")).ToArray() : null;

            // build the SET part of the SQL command
            string set = input != null ? string.Join (", ", columns.Select (i => "`" + i + "`=@_" + i).ToArray ()) : "";

            // create SQL based on HTTP method
            string sql = null;
            switch (method) {
            case "GET":
                sql = string.Format ("select * from `{0}`" + (key > 0 ? " where `id`=@pk" : ""), table); break;
            case "PUT":
                sql = string.Format ("update `{0}` set {1} where `id`=@pk",table,set); break;
            case "POST":
                sql = string.Format ("insert into `{0}` set {1}; select scope_identity()",table,set); break;
            case "DELETE":
                sql = string.Format ("delete `{0}` where `id`=@pk",table); break;
            }

            // add parameters to command
            MySqlCommand command = new MySqlCommand(sql, link);
            if (input!=null) foreach (string c in columns) command.Parameters.AddWithValue ("@_"+c, input[c]);
            if (key>0) command.Parameters.AddWithValue ("@pk", key);

            // print results, insert id or affected row count
            if (method == "GET") {
                MySqlDataReader reader = command.ExecuteReader ();
                var fields = new List<string> ();
                for (int i = 0; i < reader.FieldCount; i++) fields.Add (reader.GetName(i));
                if (key == 0) context.Response.WriteAsync("[");
                bool first = true;
                while (reader.Read ()) {
                    if (first) first = false;
                    else context.Response.WriteAsync(",");
                    Dictionary<string, object> row = new Dictionary<string, object> ();
                    foreach (var field in fields) row.Add (field, reader [field]);
                    context.Response.WriteAsync(JsonConvert.SerializeObject((object)row));
                }
                if (key == 0) context.Response.WriteAsync("]");
            } else if (method == "POST") {
                MySqlDataReader reader = command.ExecuteReader ();
                reader.NextResult ();
                reader.Read ();
                context.Response.WriteAsync(JsonConvert.SerializeObject((object)reader.GetValue (0)));
            } else {
                context.Response.WriteAsync(JsonConvert.SerializeObject((object) command.ExecuteNonQuery ()));
            }

            // close mysql connection
            link.Close ();

            return context.Response.Body.FlushAsync();
        }
    }
}