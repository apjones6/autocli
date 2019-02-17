using System;

namespace AutoCli
{
	/// <summary>
	/// Describes a serializer for the <see cref="Cli"/>, which can serialize
	/// to output, and deserialize from input sources.
	/// </summary>
	public interface ICliSerializer
	{
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
	}
}
