using System;

namespace AutoCli.Attributes
{
	[AttributeUsage(AttributeTargets.Method)]
    public class CliMethodAttribute : Attribute
	{
		public CliMethodAttribute()
		{
		}

		public CliMethodAttribute(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The name cannot be null, empty, or whitespace", nameof(name));
			Name = name;
		}

		public string Name { get; }
    }
}
