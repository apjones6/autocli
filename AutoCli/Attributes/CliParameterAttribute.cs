using System;

namespace AutoCli.Attributes
{
	[AttributeUsage(AttributeTargets.Parameter)]
    public class CliParameterAttribute : Attribute
	{
		public CliParameterAttribute()
		{
		}

		public CliParameterAttribute(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The name cannot be null, empty, or whitespace", nameof(name));
			Name = name;
		}

		public string Name { get; }
    }
}
