using NUnit.Framework;

namespace AutoCli.Tests
{
	[TestFixture]
	public class DefaultCommands_Tests : TestsBase
	{
		[Test]
		public void Help_WritesHelp()
		{
			var output = Execute("--help");
			Assert.That(output, Is.EqualTo(new[]
			{
				"Usage: AutoCli.Tests SERVICE METHOD",
				"",
				"A test AutoCli application",
				"",
				"Options:",
				"      --help     Show help information",
				"  -o, --output   Sets the output path for file output",
				"  -v, --version  Show version",
				"",
				"Services:",
				"  groups  Manage groups and memberships",
				"  users   Manage users"
			}));
		}

		[TestCase("--version")]
		[TestCase("-v")]
		public void Version_WritesVersion(string input)
		{
			var output = Execute(input);
			Assert.That(output, Is.EqualTo(new[]
			{
				"AutoCli.Tests",
				"1.2.3.0"
			}));
		}
	}
}
