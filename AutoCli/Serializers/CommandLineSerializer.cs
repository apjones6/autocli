using System;

namespace AutoCli.Serializers
{
	/// <summary>
	/// A <see cref="ICliSerializer"/> implementation which supports input/output from the command line.
	/// </summary>
	internal class CommandLineSerializer : ICliSerializer
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
		public bool TryReadParameter(string input, Type type, out object parameter)
		{
			// Special cases we can't use Convert to handle
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				if (TryReadParameter(input, type.GetGenericArguments()[0], out parameter))
				{
					return true;
				}
			}
			else if (type.IsEnum)
			{
				try
				{
					parameter = Enum.Parse(type, input, true);
					return true;
				}
				catch (Exception)
				{
					parameter = null;
					return false;
				}
			}
			else if (type == typeof(Guid))
			{
				if (Guid.TryParse(input, out var guid))
				{
					parameter = guid;
					return true;
				}
			}
			
			// Convert will handle many scenarios
			try
			{
				parameter = Convert.ChangeType(input, type);
				return true;
			}
			catch (Exception)
			{
				parameter = null;
				return false;
			}
		}
	}
}
