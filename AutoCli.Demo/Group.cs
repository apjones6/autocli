using AutoCli.Attributes;
using System;

namespace AutoCli.Demo
{
	public class Group
	{
		public Group()
		{
			MemberIds = new Guid[0];
		}

		[CliOutput(Key = true)]
		public Guid Id { get; set; }

		public Guid[] MemberIds { get; set; }

		[CliOutput(Order = 1)]
		public string Name { get; set; }

		[CliOutput(Order = 2)]
		public GroupVisibility Visibility { get; set; }
	}
}
