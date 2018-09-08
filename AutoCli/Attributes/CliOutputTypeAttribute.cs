using System;

namespace AutoCli.Attributes
{
	/// <summary>
	/// Describes how an <see cref="Output"/> class can be used by <see cref="Cli"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
    public sealed class CliOutputTypeAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the declared types which this <see cref="Output"/> knows.
		/// </summary>
		public Type[] DeclaredTypes { get; set; }

		/// <summary>
		/// Gets or sets the declared type which this <see cref="Output"/> knows.
		/// </summary>
		public Type DeclaredType { get; set; }
    }
}
