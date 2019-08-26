using AutoCli.Demo;
using AutoCli.Json;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading.Tasks;

namespace AutoCli.Tests
{
	[TestFixture]
	public class Json_Tests : TestsBase
	{
		protected override void SetUpCli(Cli cli)
		{
			cli.AddJson();
		}

		protected override void SetUpMocks()
		{
			Mock.Get(GroupService).Setup(m => m.CreateAsync(It.IsAny<Group>())).Returns<Group>(x => { x.Id = new Guid("A891E363-D7D4-4B82-B11E-A451AB2346CD"); return Task.FromResult(new Response<Group>(x, HttpStatusCode.Created)); });
		}

		[Test]
		public void Groups_Create_CallsService()
		{
			var output = Execute("groups", "create", "--group", "{\"name\":\"Group A\",\"visibility\":\"Public\"}");
			Assert.That(output, Is.EqualTo(new[]
			{
				"         ID:  a891e363-d7d4-4b82-b11e-a451ab2346cd",
				"       NAME:  Group A",
				" VISIBILITY:  Public",
				"",
				" STATUS:  201 (Created)"
			}));

			Mock.Get(GroupService).Verify(m => m.CreateAsync(It.Is<Group>(x => x.Name == "Group A" && x.Visibility == GroupVisibility.Public)));
		}
	}
}
