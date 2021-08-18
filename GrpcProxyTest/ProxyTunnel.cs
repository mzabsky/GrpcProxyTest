using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace GrpcProxyTest
{
    public class ProxyTunnel
    {
        private IHost host;
        public short ProxyPort { get; }
        public string ProxyAddress { get; }
        public string DestinationAddress { get; }

        public bool IsProxy => this.ProxyAddress != this.DestinationAddress;

        public ProxyTunnel(short proxyPort, short destinationPort)
        {
            this.ProxyPort = proxyPort;
            this.ProxyAddress = $"http://{Environment.MachineName}:{proxyPort}/";
            this.DestinationAddress = $"http://{Environment.MachineName}:{destinationPort}/";
        }

        public void Start()
        {
            if (!this.IsProxy)
            {
                return;
            }

            var configurationContent = @"{
              ""ReverseProxy"": {
                ""Routes"": {
                  ""route1"" : {
                    ""ClusterId"": ""Client"",
                    ""Match"": {
                      ""Path"": ""{**catch-all}""
                    },
                  }
                },
                ""Clusters"": {
                  ""Client"": {
                    ""HttpRequest"": {
                      ""Version"": ""2"",
                      ""VersionPolicy"": ""RequestVersionExact""
                    },
                    ""Destinations"": {
                      ""Client1"": {
                        ""Address"": """ + this.DestinationAddress + @"""
                      }
                    }
                  }
                }
              }
            }";

            var configStream = new MemoryStream(Encoding.UTF8.GetBytes(configurationContent));

            var hostBuilder = Host.CreateDefaultBuilder();
            hostBuilder.ConfigureAppConfiguration(configure => configure.AddJsonStream(configStream));
            hostBuilder.ConfigureWebHostDefaults(webHostBuilder =>
            {
                //webHostBuilder.UseUrls($"http://{Environment.MachineName}:{ProxyPort}");
                webHostBuilder.UseStartup<ProxyStartup>();
                webHostBuilder.ConfigureKestrel(options =>
                {
                    options.ListenAnyIP(this.ProxyPort, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });
                });
            });
            this.host = hostBuilder.Build();
            this.host.Start();
        }

        public void Stop()
        {
            this.host.StopAsync().GetAwaiter().GetResult();
        }
    }
}