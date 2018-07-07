using AutoCli.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoCli.Representation
{
	internal class CliOutput
    {
		private readonly object result;
		private readonly Type declaredType;

		private bool initialized;
		private bool isTable;
		private OutputProp[] props;

		public CliOutput(object result, Type declaredType)
		{
			this.declaredType = declaredType ?? throw new ArgumentNullException(nameof(declaredType));
			this.result = result;
		}
		
		public void Write()
		{
			if (result == null) return;

			Initialize();

			if (props != null)
			{
				if (isTable)
				{
					// Table of cells, structured cells[row][col]
					var cells = new List<List<string>>
					{
						props.Select(x => x.Info.Name.ToUpper()).ToList()
					};

					// Add rows for all the values
					foreach (var item in (IEnumerable)result)
					{
						cells.Add(props
							.Select(x => x.Info.GetValue(item)?.ToString())
							.ToList());
					}

					// Find the width for each column
					var widths = cells[0].Select((x, i) => cells.Select(r => r[i]).Max(r => r?.Length ?? 0)).ToArray();
					
					// Write table
					for (var i = 0; i < cells.Count; ++i)
					{
						for (var j = 0; j < cells[i].Count; ++j)
						{
							Console.Write((cells[i][j] ?? string.Empty).PadRight(widths[j] + 2));
						}

						Console.WriteLine();
					}
				}
				else
				{
					var padding = props.Max(x => x.Info.Name.Length + 1);
					foreach (var prop in props)
					{
						var header = prop.Info.Name + ":";
						Console.WriteLine($"  {header.PadRight(padding)}  {prop.Info.GetValue(result)}");
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
		}

		private OutputProp[] GetProps(Type classType)
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
						throw new ApplicationException($"CliOutputAttribute cannot be applied to {prop.DeclaringType.FullName}.{prop.Name}. It can only be applied to value type and string properties.");
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

		private struct OutputProp
		{
			public CliOutputAttribute Attribute;
			public PropertyInfo Info;
		}
	}
}
