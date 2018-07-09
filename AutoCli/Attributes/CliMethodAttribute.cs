using System;

namespace AutoCli.Attributes
{
	/// <summary>
	/// Specifies that a method should be available to <see cref="Cli"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
    public sealed class CliMethodAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CliMethodAttribute"/> class.
		/// </summary>
		public CliMethodAttribute()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CliMethodAttribute"/> class with the custom name.
		/// </summary>
		/// <param name="name">The method name to use.</param>
		public CliMethodAttribute(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The name cannot be null, empty, or whitespace", nameof(name));
			Name = name;
		}

		/// <summary>
		/// Gets or sets the method description, for use when showing help information.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Gets the method name to use (if overridden).
		/// </summary>
		public string Name { get; }
    }
}
