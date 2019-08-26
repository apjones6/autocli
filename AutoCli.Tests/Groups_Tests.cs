using AutoCli.Demo;
using Moq;
using NUnit.Framework;
using System;

namespace AutoCli.Tests
{
	[TestFixture]
	public class Groups_Tests : TestsBase
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
		public void Get_Output()
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

		[TestCase("--help")]
		[TestCase("-h")]
		public void Help_Output(string command)
		{
			var output = Execute($"groups {command}");
			Assert.That(output, Is.EqualTo(new[]
			{
				"Usage: AutoCli.Tests groups METHOD",
				"",
				"Manage groups and memberships",
				"",
				"Methods:",
				"  add-member     Add a user to the group",
				"  create         Create a group",
				"  delete         Delete a group",
				"  get            Get a group",
				"  list           List groups",
				"  list-members   List users in the group",
				"  remove-member  Remove a user from the group"
			}));
		}
		
		[Test]
		public void Help_AddMember_Output()
		{
			var output = Execute("groups add-member");
			Assert.That(output, Is.EqualTo(new[]
			{
				"Usage: AutoCli.Tests groups add-member params...",
				"",
				"Add a user to the group",
				"",
				"Parameters:",
				"  --group-id <Guid> --user-id <Guid>"
			}));
		}

		[Test]
		public void Help_Create_Output()
		{
			var output = Execute("groups create");
			Assert.That(output, Is.EqualTo(new[]
			{
				"Usage: AutoCli.Tests groups create params...",
				"",
				"Create a group",
				"",
				"Parameters:",
				"  --group <Group>",
				"  --name <string> [--visibility <GroupVisibility>]"
			}));
		}

		[Test]
		public void Help_List_Output()
		{
			// TODO: don't output an error for -h/--help as it's clear what the user wants
			var output = Execute("groups list --help");
			Assert.That(output, Is.EqualTo(new[]
			{
				"Unknown parameter \"--help\".",
				"",
				"Usage: AutoCli.Tests groups list params...",
				"",
				"List groups",
				"",
				"Parameters:",
				"  [--skip <int>] [--take <int>]"
			}));
		}

		[Test]
		public void List_Output()
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
	}
}
