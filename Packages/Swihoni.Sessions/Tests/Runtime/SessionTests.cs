using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using LiteNetLib;
using NUnit.Framework;
using Swihoni.Components;
using Swihoni.Components.Networking;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Tests
{
    public class SessionTests
    {
        [Test]
        public void TestItemInterpolation()
        {
            ItemAssetLink.Initialize();
            var i1 = new ItemComponent {id = new ByteProperty(1), status = new ByteStatusComponent {id = new ByteProperty(0), elapsedUs = new UIntProperty(1870000u)}};
            var i2 = new ItemComponent {id = new ByteProperty(1), status = new ByteStatusComponent {id = new ByteProperty(1), elapsedUs = new UIntProperty(38000u)}};
            var id = new ItemComponent();
            const float interpolation = 0.9f;
            Interpolator.InterpolateInto(i1, i2, id, interpolation);
            id.InterpolateFrom(i1, i2, interpolation);
            Assert.AreEqual(1, id.status.id.Value);
            Assert.AreEqual(21200u, id.status.elapsedUs.Value);
        }

        [Test]
        public void TestSessionClone()
        {
            var standardElements = SessionElements.NewStandardSessionElements();
            var s1 = new ServerSessionContainer(standardElements.elements.Append(typeof(ServerStampComponent)));
            IEnumerable<Type> playerElements = standardElements.playerElements
                                                               .Concat(standardElements.commandElements)
                                                               .Append(typeof(StampComponent));
            s1.Require<PlayerContainerArrayElement>().SetAll(() => new Container(playerElements));
            s1.Zero();

            ServerSessionContainer s2 = s1.Clone();
            Assert.IsTrue(s1.EqualTo(s2));

            s1.RegisterAppend(typeof(ServerTag));
            s2.RegisterAppend(typeof(LocalizedClientStampComponent));

            float start = Time.realtimeSinceStartup;
            s1.CopyFrom(s2);
            Debug.Log(1e3f * (Time.realtimeSinceStartup - start));
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
        public void TestSessionNetworking()
        {
            var standardElements = SessionElements.NewStandardSessionElements();
            var session = new ServerSessionContainer(standardElements.elements.Append(typeof(ServerStampComponent)));
            session.Require<PlayerContainerArrayElement>().SetAll(() => new Container(standardElements.playerElements
                                                                                                      .Append(typeof(ServerStampComponent))
                                                                                                      .Append(typeof(ClientStampComponent))));
            const uint time = 2424124u;
            session.Require<ServerStampComponent>().timeUs.Value = time;

            var localHost = new IPEndPoint(IPAddress.Loopback, 7777);

            using (var server = new ComponentServerSocket(localHost))
            using (var client = new ComponentClientSocket(localHost))
            {
                server.RegisterContainer(typeof(ServerSessionContainer), session);
                client.RegisterContainer(typeof(ServerSessionContainer), session);

                server.PollEvents();
                client.PollEvents();

                client.SendToServer(session, DeliveryMethod.Unreliable);

                var received = 0;

                Thread.Sleep(100);

                server.OnReceive = (peer, component) =>
                {
                    switch (component)
                    {
                        case ServerSessionContainer container:
                            Assert.AreEqual(time, container.Require<ServerStampComponent>().timeUs.Value, 1e-6f);
                            received++;
                            break;
                    }
                };
                server.PollEvents();

                Assert.AreEqual(1, received);
            }
        }

        [Test]
        public void TestCommandNetworking()
        {
            var standardElements = SessionElements.NewStandardSessionElements();
            var clientCommands = new ClientCommandsContainer(standardElements.playerElements
                                                                             .Concat(standardElements.commandElements)
                                                                             .Append(typeof(StampComponent)));

            clientCommands.Require<StampComponent>().tick.Value = 35;
            clientCommands.Require<StampComponent>().durationUs.Value = 24124124u;

            var localHost = new IPEndPoint(IPAddress.Loopback, 7777);

            using (var server = new ComponentServerSocket(localHost))
            using (var client = new ComponentClientSocket(localHost))
            {
                server.RegisterContainer(typeof(ClientCommandsContainer), clientCommands);
                client.RegisterContainer(typeof(ClientCommandsContainer), clientCommands);

                server.PollEvents();
                client.PollEvents();

                const int send = 120;

                for (var i = 0; i < send; i++)
                    client.SendToServer(clientCommands, DeliveryMethod.Unreliable);

                var received = 0;

                Thread.Sleep(100);

                server.OnReceive = (ipEndPoint, component) =>
                {
                    switch (component)
                    {
                        case ClientCommandsContainer command:
                            Assert.IsTrue(clientCommands.EqualTo(command));
                            received++;
                            break;
                    }
                };
                server.PollEvents();

                Assert.AreEqual(send, received);
            }
        }
    }
}