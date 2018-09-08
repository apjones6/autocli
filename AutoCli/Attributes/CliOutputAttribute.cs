using System;

namespace AutoCli.Attributes
{
	/// <summary>
	/// Specifies that a property should be written when writing the output of an invoked method.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
    public sealed class CliOutputAttribute : Attribute
    {
		/// <summary>
		/// Gets or sets a value indicating whether this property is a 'key' for objects shown in a
		/// list or table. This affects the order and display.
		/// </summary>
		public bool Key { get; set; }

		/// <summary>
		/// Gets or sets the property order, to customize how output data is written.
		/// </summary>
		public int Order { get; set; }
    }
}
