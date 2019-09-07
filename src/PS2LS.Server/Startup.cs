using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Websocket.Client;

namespace PS2LS.Server
{
    public class Startup
    {
        private const string ps2ApiEndpoint = "wss://push.planetside2.com/streaming?environment=ps2&service-id=s:3216732167";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();

            app.UseWebSockets();

            var exitEvent = new ManualResetEvent(false);
            var url = new Uri(ps2ApiEndpoint);

            using (var ws = new WebsocketClient(url))
            {
                ws.ReconnectTimeoutMs = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;
                ws.ReconnectionHappened.Subscribe(type =>
                    Debug.WriteLine($"Reconnection happened, type: {type}"));

                ws.MessageReceived.Subscribe(msg => Debug.WriteLine($"Message received: {msg}"));
                ws.Start();

                var item = "1125904204410686";
                string sendString =
            "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[" + item + "],\r\n\t\"eventNames\":[\"Deaths\", \"PlayerLogin\", \"PlayerLogout\"]\r\n}";
                Task.Run(() => ws.Send(sendString));

                exitEvent.WaitOne();
            }

        }
    }
}
