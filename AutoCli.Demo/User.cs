using AutoCli.Attributes;
using System;

namespace AutoCli.Demo
{
	public class User
	{
		[CliOutput(Order = 2)]
		public int? Age { get; set; }

		[CliOutput(Order = 1)]
		public string DisplayName { get; set; }

		[CliOutput(Key = true)]
		public Guid Id { get; set; }

		public override string ToString()
		{
			return $"{Id} ({DisplayName})";
		}
	}
}
