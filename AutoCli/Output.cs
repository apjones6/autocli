using System;

namespace AutoCli
{
	/// <summary>
	/// Describes the output from executing a method, and is responsible for writing the output to
	/// the destination (usually console).
	/// </summary>
	public class Output
	{
		/// <summary>
		/// Gets the <see cref="AutoCli.Cli"/> instance.
		/// </summary>
		public Cli Cli { get; internal set; }

		/// <summary>
		/// Gets the declared type for this output.
		/// </summary>
		public Type DeclaredType { get; internal set; }

		/// <summary>
		/// Gets the output object.
		/// </summary>
		public object Result { get; internal set; }

		/// <summary>
		/// Returns the object to write to console (when using console output), defaults to the result
		/// property value.
		/// </summary>
		/// <remarks>
		/// When a <see cref="ConsoleContent"/> instance is returned, it allows for richer console
		/// formatting by aggregating objects in the response.
		/// </remarks>
		/// <returns>
		/// The console content to write.
		/// </returns>
		public virtual object GetConsoleContent()
		{
			return Result;
		}

		/// <summary>
		/// Returns the object to write to a file (when using file output), defaults to the result
		/// property value.
		/// </summary>
		/// <remarks>
		/// This method allows modification of the object, or by returning null can prevent file
		/// content being written at all (command line will be used instead).
		/// </remarks>
		/// <returns>
		/// The file content to write.
		/// </returns>
		public virtual object GetFileContent()
		{
			return Result;
		}

		/// <summary>
		/// Returns a new instance of the <see cref="Output"/> class with the provided
		/// content data and its declared type.
		/// </summary>
		/// <param name="content">The output content data.</param>
		/// <param name="contentType">The output declared type.</param>
		/// <returns>
		/// A new <see cref="Output"/> instance.
		/// </returns>
		protected Output CreateOutput(object content, Type contentType)
		{
			return Cli.CreateOutput(content, contentType);
		}
	}
}
