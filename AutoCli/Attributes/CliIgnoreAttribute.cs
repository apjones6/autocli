using System;

namespace AutoCli.Attributes
{
	/// <summary>
	/// Specified a member should be ignored by <see cref="Cli"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class CliIgnoreAttribute : Attribute
	{
    }
}
