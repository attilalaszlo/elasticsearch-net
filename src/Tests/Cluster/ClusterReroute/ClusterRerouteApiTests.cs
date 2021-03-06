﻿using System;
using System.Collections.Generic;
using Elasticsearch.Net;
using FluentAssertions;
using Nest;
using Tests.Framework;
using Tests.Framework.Integration;
using Tests.Framework.MockData;
using Xunit;

namespace Tests.Cluster.ClusterReroute
{
	[Collection(IntegrationContext.ReadOnly)]
	public class ClusterRerouteApiTests : ApiIntegrationTestBase<IClusterRerouteResponse, IClusterRerouteRequest, ClusterRerouteDescriptor, ClusterRerouteRequest>
	{

		public ClusterRerouteApiTests(ReadOnlyCluster cluster, EndpointUsage usage) : base(cluster, usage) { }
		protected override LazyResponses ClientUsage() => Calls(
			fluent: (client, f) => client.ClusterReroute(f),
			fluentAsync: (client, f) => client.ClusterRerouteAsync(f),
			request: (client, r) => client.ClusterReroute(r),
			requestAsync: (client, r) => client.ClusterRerouteAsync(r)
		);

		protected override bool ExpectIsValid => false;
		protected override int ExpectStatusCode => 400;
		protected override HttpMethod HttpMethod => HttpMethod.POST;
		protected override string UrlPath => "/_cluster/reroute";

		protected override Func<ClusterRerouteDescriptor, IClusterRerouteRequest> Fluent => c => c
			.AllocateEmptyPrimary(a => a
				.Index<Project>()
				.Node("x")
				.Shard(0)
				.AcceptDataLoss(true)
			)
			.AllocateStalePrimary(a => a
				.Index<Project>()
				.Node("x")
				.Shard(0)
				.AcceptDataLoss(true)
			)
		    .AllocateReplica(a => a
				.Index<Project>()
				.Node("x")
				.Shard(0)
			)
			.Move(a => a
				.ToNode("y")
				.FromNode("x")
				.Index("project")
				.Shard(0)
			)
			.Cancel(a => a
				.Index("project")
				.Node("x")
				.Shard(1)
			);

		protected override ClusterRerouteRequest Initializer => new ClusterRerouteRequest
		{
			Commands = new List<IClusterRerouteCommand>
			{
				new AllocateEmptyPrimaryRerouteCommand { Index = IndexName.From<Project>(), Node = "x", Shard = 0, AcceptDataLoss = true },
				new AllocateStalePrimaryRerouteCommand { Index = IndexName.From<Project>(), Node = "x", Shard = 0, AcceptDataLoss = true },
				new AllocateReplicaClusterRerouteCommand { Index = IndexName.From<Project>(), Node = "x", Shard = 0 },
				new MoveClusterRerouteCommand { Index = IndexName.From<Project>(), FromNode = "x", ToNode = "y", Shard = 0},
				new CancelClusterRerouteCommand() { Index = "project", Node = "x", Shard = 1}
			}
		};

		protected override object ExpectJson => new
		{
			commands = new []
			{
				new Dictionary<string, object> { { "allocate_empty_primary", new
				{
					allow_primary = true,
					index = "project",
					node = "x",
					shard = 0,
					accept_data_loss = true
				} } },
				new Dictionary<string, object> { { "allocate_stale_primary", new
				{
					index = "project",
					node = "x",
					shard = 0,
					accept_data_loss = true
				} } },
				new Dictionary<string, object> { { "allocate_replica", new
				{
					allow_primary = false,
					index = "project",
					node = "x",
					shard = 0
				} } },
				new Dictionary<string, object> { { "move", new
				{
					to_node = "y",
					from_node = "x",
					index = "project",
					shard = 0
				} } },
				new Dictionary<string, object> { { "cancel", new
				{
					index = "project",
					node ="x",
					shard = 1
				} } },
			}
		};

		protected override void ExpectResponse(IClusterRerouteResponse response)
		{
			response.IsValid.Should().BeFalse();
			response.ServerError.Should().NotBeNull();
			response.ServerError.Status.Should().Be(400);
			response.ServerError.Error.Should().NotBeNull();
			response.ServerError.Error.Reason.Should().Contain("failed to resolve");
			response.ServerError.Error.Type.Should().Contain("illegal_argument_exception");

		}
	}


	//TODO simple integration test against isolated index to test happy flow
}
