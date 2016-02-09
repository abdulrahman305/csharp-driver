﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dse.Graph;
using Dse.Test.Integration.ClusterManagement;
using NUnit.Framework;

namespace Dse.Test.Integration.Graph
{
    public class GraphTests : BaseIntegrationTest
    {
        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            CcmHelper.Start(1);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            CcmHelper.Remove();
        }

        [Test]
        public void Should_Execute_Simple_Graph_Query()
        {
            using (var cluster = DseCluster.Builder().AddContactPoint(CcmHelper.InitialContactPoint).Build())
            {
                var session = cluster.Connect();
                //create graph1
                session.ExecuteGraph(new SimpleGraphStatement("system.createGraph('graph1').ifNotExist().build()"));
                var rs = session.ExecuteGraph(new SimpleGraphStatement("g.V()").SetGraphName("graph1"));
                Assert.NotNull(rs);
            }
        }

        [Test]
        public void Should_Get_Vertices_Of_Classic_Schema()
        {
            CreateClassicGraph(CcmHelper.InitialContactPoint, "classic1");
            using (var cluster = DseCluster.Builder()
                .AddContactPoint(CcmHelper.InitialContactPoint)
                .WithGraphOptions(new GraphOptions().SetName("classic1"))
                .Build())
            {
                var session = cluster.Connect();
                var rs = session.ExecuteGraph(new SimpleGraphStatement("g.V()"));
                var resultArray = rs.ToArray();
                Assert.Greater(resultArray.Length, 0);
                foreach (Vertex v in rs)
                {
                    Assert.NotNull(v);
                    Assert.AreEqual("vertex", v.Label);
                    Assert.True(v.Properties.ContainsKey("name"));
                }
                Assert.NotNull(rs);
            }
        }
    }
}
