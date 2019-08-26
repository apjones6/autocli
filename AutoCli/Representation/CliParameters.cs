using AutoCli.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AutoCli.Representation
{
	/// <summary>
	/// A single <see cref="CliParameters"/> which matches a given parameter combination which can
	/// be invoked.
	/// </summary>
	internal class CliParameters
	{
		private static readonly Dictionary<Type, string> ALIASES = new Dictionary<Type, string>()
		{
			{ typeof(byte), "byte" },
			{ typeof(sbyte), "sbyte" },
			{ typeof(short), "short" },
			{ typeof(ushort), "ushort" },
			{ typeof(int), "int" },
			{ typeof(uint), "uint" },
			{ typeof(long), "long" },
			{ typeof(ulong), "ulong" },
			{ typeof(float), "float" },
			{ typeof(double), "double" },
			{ typeof(decimal), "decimal" },
			{ typeof(object), "object" },
			{ typeof(bool), "bool" },
			{ typeof(char), "char" },
			{ typeof(string), "string" },
			{ typeof(void), "void" }
		};

		private readonly CliMethod method;
		private readonly MethodInfo info;
		private readonly Parameter[] parameters;

		/// <summary>
		/// Initializes a new instance of the <see cref="CliParameters"/> class for the specified
		/// <see cref="CliMethod"/> and <see cref="MethodInfo"/> to map.
		/// </summary>
		/// <param name="method">The <see cref="CliMethod"/> instance.</param>
		/// <param name="info">The <see cref="MethodInfo"/> instance.</param>
		public CliParameters(CliMethod method, MethodInfo info)
		{
			this.method = method ?? throw new ArgumentNullException(nameof(method));
			this.info = info ?? throw new ArgumentNullException(nameof(info));

			parameters = info
				.GetParameters()
				.Select(x => new Parameter(x, method.Cli))
				.ToArray();
		}

		/// <summary>
		/// Gets the <see cref="AutoCli.Cli"/> instance.
		/// </summary>
		public Cli Cli => method.Cli;

		/// <summary>
		/// Executes the provided input arguments against this <see cref="CliParameters"/> instance,
		/// either invoking the method and returning output, or returns false.
		/// </summary>
		/// <param name="args">The input arguments.</param>
		/// <returns>
		/// True if CLI execution should stop.
		/// </returns>
		public bool Execute(string[] args)
		{
			// Parse the provided arguments against out parameters. This will check for bad argument
			// orders, unknown parameter names, and inconvertable types
			IDictionary<Parameter, object> parsedArgs = null;
			try
			{
				parsedArgs = ParseArgs(parameters, args);
			}
			catch (ArgumentException ex)
			{
				// HACK: use object return value to describe errors (differentiate parameter mismatch from read errors etc)
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(ex.Message);
				Console.ResetColor();
				return false;
			}

			// Ensure all required parameters exist
			if (!parameters.Where(x => !x.IsThis && !x.IsDefault).All(x => parsedArgs.ContainsKey(x)))
			{
				return false;
			}

			// Apply defaults for missing optional parameters
			foreach (var param in parameters.Where(x => !parsedArgs.ContainsKey(x)))
			{
				parsedArgs[param] = param.IsThis
					? method.Resolve()
					: param.DefaultValue;
			}

			// Convert to invoke parameters
			var invokeParams = parameters.Select(x => parsedArgs[x]).ToArray();
			
			var result = parameters.Length == 0 || !parameters[0].IsThis
				? info.Invoke(method.Resolve(), invokeParams)
				: info.Invoke(null, invokeParams);

			// Inspect the return type to output data for Task<T> and T return methods, but not for Task and void methods
			// NOTE: Use declared type to safely handle Task<VoidTaskResult>
			Output output = null;
			if (typeof(Task).IsAssignableFrom(info.ReturnType))
			{
				var task = (Task)result;
				task.Wait();
				if (info.ReturnType.IsGenericType)
				{
					var declaredType = info.ReturnType.GetGenericArguments()[0];
					var taskResult = info.ReturnType.GetProperty("Result").GetValue(task);
					output = Cli.CreateOutput(taskResult, declaredType);
				}
			}
			else if (info.ReturnType != typeof(void))
			{
				output = Cli.CreateOutput(result, info.ReturnType);
			}

			// If an output was created, write it
			if (output != null)
			{
				Cli.Write(output);
			}

			return true;
		}

		/// <summary>
		/// Writes the <see cref="CliParameters"/> help information to the console, which includes
		/// the arguments combination for this set.
		/// </summary>
		public void ShowHelp()
		{
			Console.Write(" ");

			foreach (var param in parameters)
			{
				if (!param.IsThis)
				{
					var token = $"--{param.Name} <{GetTypeName(param.Type)}>";
					if (param.IsDefault)
					{
						token = $"[{token}]";
					}

					Console.Write(" " + token);
				}
			}
			
			Console.WriteLine();
		}

		/// <summary>
		/// Returns the type name to use for the specified <see cref="Type"/>, which accounts
		/// for nullable and system types.
		/// </summary>
		/// <param name="type">The type to name.</param>
		/// <returns>
		/// The name to use for the type.
		/// </returns>
		private static string GetTypeName(Type type)
		{
			// Get the inner type for nullable
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				type = type.GetGenericArguments()[0];
			}

			// If it's a system type use the alias, otherwise the name property
			if (!ALIASES.TryGetValue(type, out var name))
			{
				name = type.Name;
			}

			return name;
		}

		/// <summary>
		/// Converts the input string to the specified <see cref="Parameter"/> type, so that the
		/// method can be invoked.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <param name="parameter">The <see cref="Parameter"/> which determines the type.</param>
		/// <returns>
		/// An instance of the parameter type (can be null).
		/// </returns>
		private object ConvertType(string input, Parameter parameter)
		{
			if (input != null)
			{
				if (Cli.TryReadParameter(input, parameter.Type, out var value))
				{
					return value;
				}

				throw new ArgumentException($"Could not convert \"{input}\" to type \"{parameter.Type}\".");
			}
			else if (parameter.IsDefault)
			{
				return parameter.DefaultValue;
			}
			else
			{
				throw new ArgumentException($"Parameter \"{parameter.Name}\" is required.");
			}
		}
		
		/// <summary>
		/// Returns the default value of the specified <see cref="Type"/>.
		/// </summary>
		/// <param name="type">The <see cref="Type"/>.</param>
		/// <returns>
		/// A value of the type (can be null).
		/// </returns>
		private static object GetDefault(Type type)
		{
			if (type.IsValueType)
			{
				return Activator.CreateInstance(type);
			}

			return null;
		}

		/// <summary>
		/// Parses the provided CLI arguments against an enumerable of <see cref="Parameter"/>s, and
		/// outputs a dictionary with the value for each parameter.
		/// </summary>
		/// <param name="parameters">The <see cref="Parameter"/>s to parse into.</param>
		/// <param name="args">The arguments to parse.</param>
		/// <returns>
		/// A dictionary mapping each parameter to a value.
		/// </returns>
		private IDictionary<Parameter, object> ParseArgs(IEnumerable<Parameter> parameters, string[] args)
		{
			var results = new Dictionary<Parameter, object>();
			Parameter param = null;

			var expectOutputPath = false;

			foreach (var arg in args)
			{
				if (expectOutputPath)
				{
					Cli.SetOutputPath(arg);
					expectOutputPath = false;
				}
				else if (arg == "-o" || arg == "--output")
				{
					expectOutputPath = true;
				}
				else if (arg.StartsWith("--"))
				{
					if (param != null)
					{
						throw new ArgumentException($"Expected value for {param.Name}, but token was \"{arg}\".");
					}

					// Structs don't default to null, so check for a name
					param = parameters.FirstOrDefault(x => x.Name.Equals(arg.Substring(2), StringComparison.OrdinalIgnoreCase));
					if (param.Name == null)
					{
						throw new ArgumentException($"Unknown parameter \"{arg}\".");
					}
					else if (results.ContainsKey(param))
					{
						throw new ArgumentException($"Parameter \"{arg}\" has already been set.");
					}
				}
				else if (param == null)
				{
					throw new ArgumentException($"Expected parameter name, but token was \"{arg}\".");
				}
				else
				{
					results[param] = ConvertType(arg, param);
					param = null;
				}
			}

			if (expectOutputPath)
			{
				throw new ArgumentException("Expected output path, but found no more arguments.");
			}

			if (param != null)
			{
				throw new ArgumentException($"Expected value for {param.Name}, but found no more arguments.");
			}

			return results;
		}

		/// <summary>
		/// Describes a parameter with enough information to be used through the execute process.
		/// </summary>
		private class Parameter
		{
			private readonly Cli cli;
			private readonly string name;

			public Parameter(ParameterInfo info, Cli cli)
			{
				this.cli = cli ?? throw new ArgumentNullException(nameof(cli));

				DefaultValue = info.DefaultValue;
				IsDefault = info.HasDefaultValue;
				name = GetParameterName(info);
				Type = info.ParameterType;

				if (info.Member.IsDefined(typeof(ExtensionAttribute), false))
				{
					IsThis = true;
				}
			}

			public object DefaultValue { get; }
			public bool IsDefault { get; }
			public bool IsThis { get; }
			public string Name => cli.ApplyNameConvention(name);
			public Type Type { get; }

			/// <summary>
			/// Returns the name to use for the provided <see cref="ParameterInfo"/>, either from a
			/// <see cref="CliParameterAttribute"/> or the actual param name.
			/// </summary>
			/// <param name="info">The <see cref="ParameterInfo"/> instance.</param>
			/// <returns>
			/// A string value.
			/// </returns>
			private static string GetParameterName(ParameterInfo info)
			{
				var attr = info.GetCustomAttribute<CliParameterAttribute>(true);
				if (attr != null && attr.Name != null)
				{
					return attr.Name;
				}

				return info.Name;
			}
		}
	}
}
