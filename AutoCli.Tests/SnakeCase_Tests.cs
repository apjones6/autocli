using NUnit.Framework;

namespace AutoCli.Tests
{
	[TestFixture]
	public class SnakeCase_Tests : TestsBase
	{
		protected override void SetUpCli(Cli cli)
		{
			cli.SetNameConvention(NameConvention.SnakeCase);
		}
		
		[Test]
		public void Help_Output()
		{
			var output = Execute("groups --help");
			Assert.That(output, Is.EqualTo(new[]
			{
				"Usage: AutoCli.Tests groups METHOD",
				"",
				"Manage groups and memberships",
				"",
				"Methods:",
				"  add_member     Add a user to the group",
				"  create         Create a group",
				"  delete         Delete a group",
				"  get            Get a group",
				"  list           List groups",
				"  list_members   List users in the group",
				"  remove_member  Remove a user from the group"
			}));
		}

		[Test]
		public void Help_AddMember_Output()
		{
			var output = Execute("groups add_member");
			Assert.That(output, Is.EqualTo(new[]
			{
				"Usage: AutoCli.Tests groups add_member params...",
				"",
				"Add a user to the group",
				"",
				"Parameters:",
				"  --group_id <Guid> --user_id <Guid>"
			}));
		}
	}
}
