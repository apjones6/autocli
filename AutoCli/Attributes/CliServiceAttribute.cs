using System;

namespace AutoCli.Attributes
{
	/// <summary>
	/// Specifies that a service should be available to <see cref="Cli"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class CliServiceAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CliServiceAttribute"/> class with the custom name.
		/// </summary>
		/// <param name="name">The service name to use.</param>
		public CliServiceAttribute(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The name cannot be null, empty, or whitespace", nameof(name));
			Name = name;
		}

		/// <summary>
		/// Gets or sets the service description, for use when showing help information.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Gets the service name.
		/// </summary>
		public string Name { get; }
    }
}
