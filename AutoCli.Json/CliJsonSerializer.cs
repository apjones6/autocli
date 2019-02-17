using Newtonsoft.Json;
using System;

namespace AutoCli.Json
{
	/// <summary>
	/// A <see cref="ICliSerializer"/> implementation which supports JSON parameter
	/// format from the command line, and input/output to JSON files.
	/// </summary>
	public class CliJsonSerializer : ICliSerializer
	{
		/// <summary>
		/// Attempts to read the provided input and output the parameter value of
		/// the appropriate type to use.
		/// </summary>
		/// <param name="input">The input to read.</param>
		/// <param name="type">The parameter type to convert to.</param>
		/// <param name="parameter">The parameter value.</param>
		/// <returns>
		/// True if the input was read, false otherwise.
		/// </returns>
		public bool TryReadParameter(string input, Type type, out object parameter)
		{
			try
			{
				parameter = JsonConvert.DeserializeObject(input, type);
				return true;
			}
			catch
			{
				parameter = null;
				return false;
			}
		}
	}
}
