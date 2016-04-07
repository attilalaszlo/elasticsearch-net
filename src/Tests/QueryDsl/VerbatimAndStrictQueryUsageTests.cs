﻿using FluentAssertions;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Framework;
using Tests.Framework.Integration;
using Tests.Framework.MockData;
using Xunit;

namespace Tests.QueryDsl
{
	public class CompoundVerbatimQueryUsageTests : QueryDslUsageTestsBase
	{
		protected override bool SupportsDeserialization => false;

		public CompoundVerbatimQueryUsageTests(ReadOnlyCluster cluster, EndpointUsage usage) : base(cluster, usage) { }

		protected override object QueryJson => new
		{
			@bool = new
			{
				must = new object[]
				{
					new
					{
						term = new
						{
							description = new
							{
								value = ""
							}
						}
					},
					new
					{
						term = new
						{
							name = new
							{
								value = "foo"
							}
						}
					}
				}
			}
		};

		protected override QueryContainer QueryInitializer =>
			new TermQuery
			{
				IsVerbatim = true,
				Field = "description",
				Value = ""
			}
			&& new TermQuery
			{
				Field = "name",
				Value = "foo"
			};

		protected override QueryContainer QueryFluent(QueryContainerDescriptor<Project> q) => q
			.Bool(b => b
				.Must(qt => qt
					.Term(t => t
						.Verbatim()
						.Field(p => p.Description)
						.Value("")
					), qt => qt
					.Term(t => t
						.Field(p => p.Name)
						.Value("foo")
					)
				)
			);
	}

	public class SingleVerbatimQueryUsageTests : QueryDslUsageTestsBase
	{
		public SingleVerbatimQueryUsageTests(ReadOnlyCluster cluster, EndpointUsage usage) : base(cluster, usage) { }

		protected override bool SupportsDeserialization => false;

		protected override object QueryJson => new
		{
			term = new
			{
				description = new
				{
					value = ""
				}
			}

		};

		protected override QueryContainer QueryInitializer => new TermQuery
		{
			IsVerbatim = true,
			Field = "description",
			Value = ""
		};


		protected override QueryContainer QueryFluent(QueryContainerDescriptor<Project> q) => q
			.Term(t => t
				.Verbatim()
				.Field(p => p.Description)
				.Value("")
			);
	}

	public class StrictQueryUsageTests
	{
		[U]
		public void FluentThrows()
		{
			var e = Assert.Throws<ArgumentException>(() =>
				new SearchDescriptor<Project>()
					.Query(q => q
						.Term(t => t
							.Strict()
							.Field("myfield")
							.Value("")
						)
					)
			);
			e.Message.Should().Be("Query is conditionless but strict is turned on");
		}

		[U]
		public void InitializerThrows()
		{
			var e = Assert.Throws<ArgumentException>(() =>
				new SearchRequest<Project>
				{
					Query = new TermQuery
					{
						IsStrict = true,
						Field = "myfield",
						Value = ""
					}
				}
			);
			e.Message.Should().Be("Query is conditionless but strict is turned on");
		}
	}
}
