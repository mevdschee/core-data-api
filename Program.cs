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
           //Console.WriteLine("Received request");
           return context.Response.WriteAsync("Hello world");
        }
    }
}