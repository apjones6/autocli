using System;
using System.Collections.Generic;
using System.Text;

namespace AutoCli.Attributes
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
    public class CliServiceAttribute : Attribute
    {
		public CliServiceAttribute(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The name cannot be null, empty, or whitespace", nameof(name));
			Name = name;
		}

		public string Name { get; }
    }
}
