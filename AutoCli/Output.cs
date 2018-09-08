using System;

namespace AutoCli
{
	/// <summary>
	/// Describes the output from executing a method, and is responsible for writing the output to
	/// the destination (usually console).
	/// </summary>
	public abstract class Output
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
		public Object Result { get; internal set; }

		/// <summary>
		/// Writes the provided content of the specified type, using the standard output mechanisms
		/// to determine an appropriate <see cref="Output"/> type to use.
		/// </summary>
		/// <remarks>
		/// Misuse of this method can cause recursive invocations resulting in a stack overflow, so
		/// care should be taken in its implementation.
		/// </remarks>
		/// <param name="content">The content data.</param>
		/// <param name="contentType">The content type.</param>
		protected void Write(object content, Type contentType)
		{
			Cli.CreateOutput(content, contentType).Write();
		}

		/// <summary>
		/// Writes the result contained in this <see cref="Output"/> to the current console.
		/// </summary>
		public abstract void Write();
	}
}
