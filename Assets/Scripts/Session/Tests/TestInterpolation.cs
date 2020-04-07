using System.Net;
using System.Threading;
using Components;
using Networking;
using NUnit.Framework;
using Session.Player.Components;

namespace Session.Tests
{
    public class InterpolationTest
    {
        [Test]
        public void TestInterpolation()
        {
            var i1 = new ItemComponent {id = new ByteProperty(1), status = new ByteStatusComponent {id = new ByteProperty(0), elapsed = new FloatProperty(1.870f)}};
            var i2 = new ItemComponent {id = new ByteProperty(1), status = new ByteStatusComponent {id = new ByteProperty(1), elapsed = new FloatProperty(0.038f)}};
            var id = new ItemComponent();
            const float interpolation = 0.9f;
            Interpolator.InterpolateInto(i1, i2, id, interpolation);
            id.InterpolateFrom(i1, i2, interpolation);
            Assert.AreEqual(1, id.status.id.Value);
            Assert.AreEqual(0.021f, id.status.elapsed.Value, 1e-3f);
        }

        [Test]
        public void TestCommand()
        {
            var p = new ClientCommandComponent {tick = new UIntProperty(32)};

            var localHost = new IPEndPoint(IPAddress.Loopback, 7777);

            using (var server = new ComponentServerSocket(localHost, SessionBase.TypeToId))
            using (var client = new ComponentClientSocket(localHost, SessionBase.TypeToId))
            {
                server.StartReceiving();
                client.StartReceiving();

                client.SendToServer(p);

                var received = false;

                Thread.Sleep(100);

                server.PollReceived((id, component) =>
                {
                    switch (component)
                    {
                        case ClientCommandComponent _:
                            received = true;
                            break;
                    }
                });

                Assert.True(received);
            }
        }
    }
}