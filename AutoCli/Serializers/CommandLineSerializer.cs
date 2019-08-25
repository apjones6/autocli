using AutoCli.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AutoCli.Serializers
{
	/// <summary>
	/// A <see cref="ICliSerializer"/> implementation which supports input/output from the command line.
	/// </summary>
	internal class CommandLineSerializer : ICliSerializer
	{
		/// <summary>
		/// Returns a value indicating whether this serializer can write contents
		/// to a file with the specified extension.
		/// </summary>
		/// <param name="extension">The file extension.</param>
		/// <returns>
		/// True if can write, false otherwise.
		/// </returns>
		public bool CanWrite(string extension)
		{
			return false;
		}

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

		/// <summary>
		/// Writes the provided content object to the output <see cref="Stream"/>.
		/// </summary>
		/// <remarks>
		/// The serializer must not attempt to close the stream.
		/// </remarks>
		/// <param name="stream">The output stream to write to.</param>
		/// <param name="c">The content to write.</param>
		public void Write(Stream stream, object c)
		{
			if (c == null) return;

			// If content isn't already wrapped, wrap it. This allows us to safely distinguish
			// enumerable objects from multiple content 'items'
			var contents = c as ConsoleContent ?? new ConsoleContent(c);
			
			Console.WriteLine();

			foreach (var content in contents.Contents)
			{
				// Handle separator special const to allow formatting
				if (content == ConsoleContent.SEPARATOR)
				{
					Console.WriteLine();
					continue;
				}

				var details = Initialize(content);
				if (details.Props != null)
				{
					if (details.IsTable)
					{
						// Find the width for each column
						var widths = details.Cells[0].Select((x, i) => details.Cells.Select(r => r[i]).Max(r => r?.Length ?? 0)).ToArray();

						// Write table
						for (var i = 0; i < details.Cells.Count; ++i)
						{
							for (var j = 0; j < details.Cells[i].Count; ++j)
							{
								Console.Write(" " + (details.Cells[i][j] ?? string.Empty).PadRight(widths[j] + 2));
							}

							Console.WriteLine();
						}
					}
					else
					{
						var width = details.Cells[0].Max(x => x.Length);
						var len = details.Cells[0].Count;
						for (var i = 0; i < len; ++i)
						{
							Console.WriteLine($" {details.Cells[0][i].PadLeft(width)}:  {details.Cells[1][i]}");
						}
					}
				}
				else if (typeof(IEnumerable).IsAssignableFrom(content.GetType()))
				{
					foreach (var item in (IEnumerable)content)
					{
						Console.WriteLine(item);
					}
				}
				else
				{
					Console.WriteLine(content);
				}
			}
		}

		/// <summary>
		/// Initializes the display information for this content, to determine
		/// whether to write a table, list, or singular value.
		/// </summary>
		/// <param name="content">The content object.</param>
		/// <returns>
		/// The output details for this content.
		/// </returns>
		private OutputDetails Initialize(object content)
		{
			var details = new OutputDetails();
			var type = content.GetType();
			if (typeof(IEnumerable).IsAssignableFrom(type))
			{
				details.IsTable = true;

				var enumerableType = type.GetInterfaces().Union(new[] { type }).FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
				if (enumerableType != null)
				{
					var itemType = enumerableType.GetGenericArguments()[0];
					if (itemType.IsClass)
					{
						details.Props = GetProps(itemType);
					}
				}
			}
			else if (type.IsClass)
			{
				details.Props = GetProps(type);
			}

			if (details.Props != null)
			{
				// Table of cells, structured cells[row][col]
				details.Cells = new List<List<string>>
				{
					details.Props.Select(x => x.Info.Name.ToUpper()).ToList()
				};

				if (details.IsTable)
				{
					// Add rows for all the values
					foreach (var item in (IEnumerable)content)
					{
						details.Cells.Add(details
							.Props
							.Select(x => x.Info.GetValue(item)?.ToString())
							.ToList());
					}
				}
				else
				{
					details.Cells.Add(details
						.Props
						.Select(x => x.Info.GetValue(content)?.ToString())
						.ToList());
				}
			}

			return details;
		}

		/// <summary>
		/// Returns the <see cref="OutputProp"/>s to use for the specified <see cref="Type"/>, to
		/// control how output is written, or null if not supported.
		/// </summary>
		/// <param name="classType">The class type to get properties for.</param>
		/// <returns>
		/// An array of <see cref="OutputProp"/>, or null.
		/// </returns>
		private static OutputProp[] GetProps(Type classType)
		{
			var props = new List<OutputProp>();

			foreach (var prop in classType.GetProperties())
			{
				var attribute = prop.GetCustomAttributes(typeof(CliOutputAttribute), true).OfType<CliOutputAttribute>().FirstOrDefault();
				if (attribute != null)
				{
					// TODO: add support
					if (!(prop.PropertyType.IsValueType || prop.PropertyType == typeof(string)))
					{
						throw new ApplicationException($"{typeof(CliOutputAttribute).FullName} cannot be applied to {prop.DeclaringType.FullName}.{prop.Name}. It can only be applied to value type and string properties.");
					}

					props.Add(new OutputProp
					{
						Attribute = attribute,
						Info = prop
					});
				}
				else if (prop.GetCustomAttribute<CliIgnoreAttribute>(true) == null)
				{
					props.Add(new OutputProp { Info = prop });
				}
			}

			// If no decorated properties, don't use this strategy
			if (props.Count == 0)
			{
				return null;
			}

			return props
				.OrderBy(x => x.Attribute?.Order ?? int.MaxValue)
				.ThenBy(x => !(x.Attribute?.Key ?? false))
				.ToArray();
		}

		private struct OutputDetails
		{
			public List<List<string>> Cells;
			public bool IsTable;
			public OutputProp[] Props;
		}
		
		private struct OutputProp
		{
			public CliOutputAttribute Attribute;
			public PropertyInfo Info;
		}
	}
}
