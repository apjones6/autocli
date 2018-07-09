using AutoCli.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoCli.Representation
{
	/// <summary>
	/// An output from an invocation of a <see cref="CliMethod"/>, which includes enough information
	/// to write the result in an appropriate format.
	/// </summary>
	internal class CliOutput
    {
		private readonly object result;
		private readonly Type declaredType;

		private List<List<string>> cells;
		private bool initialized;
		private bool isTable;
		private OutputProp[] props;

		/// <summary>
		/// Initializes a new instance of the <see cref="CliOutput"/> class with the provided
		/// result data and its declared type.
		/// </summary>
		/// <param name="result">The output result data.</param>
		/// <param name="declaredType">The output declared type.</param>
		public CliOutput(object result, Type declaredType)
		{
			this.declaredType = declaredType ?? throw new ArgumentNullException(nameof(declaredType));
			this.result = result;
		}
		
		/// <summary>
		/// Writes the result contained in this <see cref="CliOutput"/> to the current console.
		/// </summary>
		public void Write()
		{
			if (result == null) return;

			Initialize();

			Console.WriteLine();

			if (props != null)
			{
				if (isTable)
				{
					// Find the width for each column
					var widths = cells[0].Select((x, i) => cells.Select(r => r[i]).Max(r => r?.Length ?? 0)).ToArray();
					
					// Write table
					for (var i = 0; i < cells.Count; ++i)
					{
						for (var j = 0; j < cells[i].Count; ++j)
						{
							Console.Write(" " + (cells[i][j] ?? string.Empty).PadRight(widths[j] + 2));
						}

						Console.WriteLine();
					}
				}
				else
				{
					var width = cells[0].Max(x => x.Length);
					var len = cells[0].Count;
					for (var i = 0; i < len; ++i)
					{
						Console.WriteLine($" {cells[0][i].PadLeft(width)}:  {cells[1][i]}");
					}
				}
			}
			else if (typeof(IEnumerable).IsAssignableFrom(declaredType))
			{
				foreach (var item in (IEnumerable)result)
				{
					Console.WriteLine(item);
				}
			}
			else
			{
				Console.WriteLine(result);
			}
		}

		/// <summary>
		/// Initializes the display information for this <see cref="CliOutput"/> instance, which
		/// includes the properties to output (if class data), an whether to show as a table.
		/// </summary>
		private void Initialize()
		{
			if (initialized) return;
			initialized = true;
			
			if (typeof(IEnumerable).IsAssignableFrom(declaredType))
			{
				isTable = true;

				var enumerableType = declaredType.GetInterfaces().Union(new[] { declaredType }).FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
				if (enumerableType != null)
				{
					var itemType = enumerableType.GetGenericArguments()[0];
					if (itemType.IsClass)
					{
						props = GetProps(itemType);
					}
				}
			}
			else if (declaredType.IsClass)
			{
				props = GetProps(declaredType);
			}

			if (props != null)
			{
				// Table of cells, structured cells[row][col]
				cells = new List<List<string>>
				{
					props.Select(x => x.Info.Name.ToUpper()).ToList()
				};

				if (isTable)
				{
					// Add rows for all the values
					foreach (var item in (IEnumerable)result)
					{
						cells.Add(props
							.Select(x => x.Info.GetValue(item)?.ToString())
							.ToList());
					}
				}
				else
				{
					cells.Add(props
						.Select(x => x.Info.GetValue(result)?.ToString())
						.ToList());
				}
			}
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
			}

			// If no decorated properties, don't use this strategy
			if (props.Count == 0)
			{
				return null;
			}

			return props
				.OrderBy(x => x.Attribute.Order)
				.ThenBy(x => !x.Attribute.Key)
				.ToArray();
		}

		/// <summary>
		/// Describes a single property of a class type, which controls how invoke results are written.
		/// </summary>
		private struct OutputProp
		{
			/// <summary>
			/// The <see cref="CliOutputAttribute"/> the property is decorated with.
			/// </summary>
			public CliOutputAttribute Attribute;

			/// <summary>
			/// The <see cref="PropertyInfo"/> for the property.
			/// </summary>
			public PropertyInfo Info;
		}
	}
}
