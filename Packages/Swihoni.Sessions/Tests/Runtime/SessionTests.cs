﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using NUnit.Framework;
using Swihoni.Components;
using Swihoni.Networking;
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
        public void TestSessionClone()
        {
            var s1 = new ServerSessionContainer(StandardComponents.StandardSessionElements.Append(typeof(ServerStampComponent)));
            IEnumerable<Type> playerElements = StandardComponents.StandardPlayerElements
                                                                 .Concat(StandardComponents.StandardPlayerCommandsElements)
                                                                 .Append(typeof(StampComponent));
            s1.Require<PlayerContainerArrayProperty>().SetAll(() => new Container(playerElements));
            s1.Zero();

            ServerSessionContainer s2 = s1.Clone();
            Assert.IsTrue(s1.EqualTo(s2));

            s1.Add(typeof(ServerTag));
            s2.Add(typeof(LocalizedClientStampComponent));

            float start = Time.realtimeSinceStartup;
            s1.FastCopyFrom(s2);
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
            var session = new ServerSessionContainer(StandardComponents.StandardSessionElements.Append(typeof(ServerStampComponent)));
            session.Require<PlayerContainerArrayProperty>().SetAll(() => new Container(StandardComponents.StandardPlayerElements
                                                                                                         .Append(typeof(ServerStampComponent))
                                                                                                         .Append(typeof(ClientStampComponent))));
            const float time = 0.241f;
            session.Require<ServerStampComponent>().time.Value = time;

            var localHost = new IPEndPoint(IPAddress.Loopback, 7777);

            using (var server = new ComponentServerSocket(localHost))
            using (var client = new ComponentClientSocket(localHost))
            {
                server.RegisterMessage(typeof(ServerSessionContainer), session);
                client.RegisterMessage(typeof(ServerSessionContainer), session);

                client.SendToServer(session);

                var received = 0;

                Thread.Sleep(100);

                server.PollReceived((ipEndPoint, component) =>
                {
                    switch (component)
                    {
                        case ServerSessionContainer container:
                            Assert.AreEqual(time, container.Require<ServerStampComponent>().time.Value, 1e-6f);
                            received++;
                            break;
                    }
                });

                Assert.AreEqual(1, received);
            }
        }

        [Test]
        public void TestCommandNetworking()
        {
            var clientCommands = new ClientCommandsContainer(StandardComponents.StandardPlayerElements
                                                                               .Concat(StandardComponents.StandardPlayerCommandsElements)
                                                                               .Append(typeof(StampComponent)));

            clientCommands.Require<StampComponent>().tick.Value = 35;
            clientCommands.Require<StampComponent>().duration.Value = 0.241f;

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

                server.PollReceived((ipEndPoint, component) =>
                {
                    switch (component)
                    {
                        case ClientCommandsContainer command:
                            Assert.IsTrue(clientCommands.EqualTo(command));
                            received++;
                            break;
                    }
                });

                Assert.AreEqual(send, received);
            }
        }
    }
}