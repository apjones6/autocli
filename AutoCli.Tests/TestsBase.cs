using AutoCli.Demo;
using Moq;
using NUnit.Framework;
using System;
using System.IO;

namespace AutoCli.Tests
{
	public abstract class TestsBase
	{
		private StringWriter standardOutput;
		private Cli cli;

		protected IGroupService GroupService { get; private set; }
		protected IUserService UserService { get; private set; }

		[SetUp]
		public void SetUp()
		{
			GroupService = Mock.Of<IGroupService>();
			UserService = Mock.Of<IUserService>();

			standardOutput = new StringWriter();
			Console.SetOut(standardOutput);
			cli = Cli.Builder
				.SetDescription("A test AutoCli application")
				.SetEntry(typeof(TestsBase).Assembly)
				.SetResolver(GetService)
				.AddService<IGroupService>()
				.AddService<IUserService>()
				.AddExtensions()
				.AddOutputs();

			SetUpCli(cli);

			SetUpMocks();
		}

		[TearDown]
		public void TearDown()
		{
			Console.SetOut(new StreamWriter(Console.OpenStandardOutput())
			{
				AutoFlush = true
			});
		}

		protected virtual void SetUpCli(Cli cli)
		{
		}

		protected virtual void SetUpMocks()
		{
		}

		protected string[] Execute(string input)
		{
			cli.Execute(input.Split(' '));

			standardOutput.Flush();
			var sb = standardOutput.GetStringBuilder();
			return sb.ToString().Replace("\r\n", "\n").Trim('\n').Split('\n');
		}

		private object GetService(Type serviceType)
		{
			if (serviceType == typeof(IGroupService))
			{
				return GroupService;
			}
			else if (serviceType == typeof(IUserService))
			{
				return UserService;
			}
			else
			{
				throw new ApplicationException("Unexpected service type.");
			}
		}
	}
}
