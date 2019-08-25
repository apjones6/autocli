using AutoCli.Demo;
using Moq;
using NUnit.Framework;
using System;

namespace AutoCli.Tests
{
	[TestFixture]
	public class SimpleCommands_Tests : TestsBase
	{
		private readonly Group[] groups = new[]
		{
			new Group { Id = new Guid("71e3a4eb-dfe7-4f41-9e64-6b510afe3fbf"), Name = "Group A" },
			new Group { Id = new Guid("1318CAEF-265B-44F4-9253-C74895E769C5"), Name = "Group B", Visibility = GroupVisibility.Public }
		};

		protected override void SetUpMocks()
		{
			Mock.Get(GroupService).Setup(m => m.ListAsync(0, 25)).ReturnsAsync(new Response<ResultSet<Group>>(new ResultSet<Group>(groups)));
			Mock.Get(GroupService).Setup(m => m.GetAsync(groups[0].Id)).ReturnsAsync(new Response<Group>(groups[0]));
			Mock.Get(GroupService).Setup(m => m.GetAsync(groups[1].Id)).ReturnsAsync(new Response<Group>(groups[1]));
		}

		[Test]
		public void Groups_List_Output()
		{
			var output = Execute("groups list");
			Assert.That(output, Is.EqualTo(new[]
			{
				" ID                                     NAME      VISIBILITY  ",
				" 71e3a4eb-dfe7-4f41-9e64-6b510afe3fbf   Group A   Private     ",
				" 1318caef-265b-44f4-9253-c74895e769c5   Group B   Public      ",
				" TOTAL:  2",
				"",
				" STATUS:  200 (OK)"
			}));
		}

		[Test]
		public void Groups_Get_Output()
		{
			var output = Execute("groups get --group-id 1318CAEF-265B-44F4-9253-C74895E769C5");
			Assert.That(output, Is.EqualTo(new[]
			{
				"         ID:  1318caef-265b-44f4-9253-c74895e769c5",
				"       NAME:  Group B",
				" VISIBILITY:  Public",
				"",
				" STATUS:  200 (OK)"
			}));
		}
	}
}
