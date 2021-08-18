using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Logging;
using GrpcProxyTest.Protobuf;

namespace GrpcProxyTest
{
    class Program
    {
        private class Impl : TestInterface.TestInterfaceBase
        {
            public override Task<TestMethodReply> TestMethod(TestMethodRequest request, ServerCallContext context)
            {
                Console.WriteLine($"Server method invoked: {request.Input}");

                return Task.FromResult(new TestMethodReply
                {
                    Output = request.Input + "!"
                });
            }
        }

        static void Main(string[] args)
        {
            GrpcEnvironment.SetLogger(new ConsoleLogger());

            Environment.SetEnvironmentVariable("GRPC_DNS_RESOLVER", "native");
            Environment.SetEnvironmentVariable("GRPC_TRACE", "all");
            Environment.SetEnvironmentVariable("GRPC_VERBOSITY", "info");

            short proxyPort = 8002;
            short destinationPort = 8003;
            var proxy = new ProxyTunnel(proxyPort, destinationPort);
            proxy.Start();

            var grpcServer = new Server
            {
                Ports = {new ServerPort(Environment.MachineName, destinationPort, ServerCredentials.Insecure)}
            };
            grpcServer.Services.Add(
                TestInterface.BindService(new Impl())
            );
            grpcServer.Start();

            var channel = new Channel(Environment.MachineName, proxyPort, ChannelCredentials.Insecure);
            var client = new TestInterface.TestInterfaceClient(channel);

            var reply = client.TestMethod(new TestMethodRequest {Input = "Hello world"});

            Console.WriteLine($"Client got reply: {reply.Output}");

            Console.ReadKey();
        }
    }
}

