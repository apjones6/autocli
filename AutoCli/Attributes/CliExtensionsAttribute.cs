using System;

namespace AutoCli.Attributes
{
	/// <summary>
	/// Specifies that a class contains extension methods available to <see cref="Cli"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
    public sealed class CliExtensionsAttribute : Attribute
    {
    }
}
