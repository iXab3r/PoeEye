using NUnit.Framework;
using AutoFixture;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive.Linq;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PoeShared.Services;
using Shouldly;

namespace PoeShared.Tests.Services
{
    [TestFixture]
    public class ProcessEtwResolverTests : FixtureBase
    {
        private static readonly IFluentLog Log = typeof(ProcessEtwResolverTests).PrepareLogger();

        [Test]
        public void ShouldCreate()
        {
            // Given
            // When 
            Action action = () => CreateInstance();

            // Then
            action.ShouldNotThrow();
        }

        [Test]
        public void ShouldInitialize()
        {
            //Given
            var instance = CreateInstance();
            instance.AddProcessById(Environment.ProcessId);

            //When
            instance.WaitForValue(x => x.IsActive, x => x == true, TimeSpan.FromSeconds(1));
            
            Log.Debug("Establishing TCP connection");
            var tcpClient = new TcpClient();
            tcpClient.Connect(IPAddress.Parse("1.1.1.1"), 443);
            Log.Debug("Established TCP connection");
            tcpClient.Client.Send(new byte[] { 1 });
            Log.Debug("Sent data via TCP connection");
            tcpClient.Dispose();
            Log.Debug("Destroyed TCP connection");
            var result = Observable.Timer(DateTimeOffset.Now, TimeSpan.FromMilliseconds(50))
                .Select(_ =>
                {
                    if (instance.TryGetProcessDataById(Environment.ProcessId, out var result))
                    {
                        return result;
                    }

                    return default;
                })
                .Where(x => x != default && x.ProcessName != default)
                .Take(1)
                .Timeout(TimeSpan.FromSeconds(1))
                .Wait();

            //Then
            result.ShouldNotBeNull();
        }
        
        private ProcessEtwResolver CreateInstance()
        {
            return Container.Build<ProcessEtwResolver>().Create();
        }
    }
}