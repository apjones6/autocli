using System;

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
		
		public string Description { get; set; }

		public MethodStrategy MethodStrategy { get; set; }

		public string Name { get; }
    }
}
