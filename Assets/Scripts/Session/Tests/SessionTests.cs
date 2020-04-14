using System.Linq;
using System.Net;
using System.Threading;
using Components;
using Networking;
using NUnit.Framework;
using Session.Components;
using Session.Player.Components;

namespace Session.Tests
{
    public class SessionTests
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

        // [Test]
        // public void TestCommandSerialization()
        // {
        //     var p1 = new ClientCommandComponent {stamp = new StampComponent {tick = new UIntProperty(35)}};
        //     
        //     var stream = new MemoryStream();
        //     Serializer.SerializeFrom(p1, stream);
        //     
        //     var p2 = new ClientCommandComponent();
        //     stream.Position = 0;
        //     Serializer.DeserializeInto(p2, stream);
        //     
        //     Assert.AreEqual(p1.stamp.tick, p2.stamp.tick);
        // }

        [Test]
        public void TestCommand()
        {
            var clientCommands = new ClientCommandsContainer(StandardComponents.StandardPlayerComponents
                                                                               .Concat(StandardComponents.StandardPlayerCommandsComponents)
                                                                               .Append(typeof(StampComponent)));
            const int tick = 35;
            const float duration = 0.241f;

            clientCommands.Require<StampComponent>().tick.Value = tick;
            clientCommands.Require<StampComponent>().duration.Value = duration;

            var localHost = new IPEndPoint(IPAddress.Loopback, 7777);

            using (var server = new ComponentServerSocket(localHost))
            using (var client = new ComponentClientSocket(localHost))
            {
                server.RegisterMessage(typeof(ClientCommandsContainer), clientCommands);
                client.RegisterMessage(typeof(ClientCommandsContainer), clientCommands);

                const int send = 120;

                for (var i = 0; i < send; i++)
                    client.SendToServer(clientCommands);

                var received = 0;

                Thread.Sleep(100);

                server.PollReceived((id, component) =>
                {
                    switch (component)
                    {
                        case ClientCommandsContainer command:
                            Assert.AreEqual(tick, command.Require<StampComponent>().tick.Value);
                            Assert.AreEqual(duration, command.Require<StampComponent>().duration.Value);
                            received++;
                            break;
                    }
                });

                Assert.AreEqual(send, received);
            }
        }
    }
}