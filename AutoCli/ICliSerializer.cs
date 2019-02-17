using System;
using System.IO;

namespace AutoCli
{
	/// <summary>
	/// Describes a serializer for the <see cref="Cli"/>, which can serialize
	/// to output, and deserialize from input sources.
	/// </summary>
	public interface ICliSerializer
	{
		/// <summary>
		/// Returns a value indicating whether this serializer can write contents
		/// to a file with the specified extension.
		/// </summary>
		/// <param name="extension">The file extension.</param>
		/// <returns>
		/// True if can write, false otherwise.
		/// </returns>
		bool CanWrite(string extension);

		/// <summary>
		/// Attempts to read the provided input and output the parameter value
		/// of the appropriate type to use.
		/// </summary>
		/// <param name="input">The input to read.</param>
		/// <param name="type">The parameter type to convert to.</param>
		/// <param name="parameter">The parameter value.</param>
		/// <returns>
		/// True if the input was read, false otherwise.
		/// </returns>
		bool TryReadParameter(string input, Type type, out object parameter);

		/// <summary>
		/// Writes the provided content object to the output <see cref="Stream"/>.
		/// </summary>
		/// <remarks>
		/// The serializer must not attempt to close the stream.
		/// </remarks>
		/// <param name="stream">The output stream to write to.</param>
		/// <param name="content">The content to write.</param>
		void Write(Stream stream, object content);
	}
}
