using System;

namespace AutoCli.Attributes
{
	/// <summary>
	/// Specifies configuration options for a method parameter.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
    public sealed class CliParameterAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CliParameterAttribute"/> class with the custom name.
		/// </summary>
		/// <param name="name">The parameter name to use.</param>
		public CliParameterAttribute(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The name cannot be null, empty, or whitespace", nameof(name));
			Name = name;
		}

		/// <summary>
		/// Gets the parameter name to use (if overridden).
		/// </summary>
		public string Name { get; }
    }
}
