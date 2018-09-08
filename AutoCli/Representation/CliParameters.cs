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
				.Select(x => new Parameter { DefaultValue = x.DefaultValue, IsDefault = x.HasDefaultValue, Name = GetParameterName(x), Type = x.ParameterType })
				.ToArray();

			if (info.IsDefined(typeof(ExtensionAttribute), false))
			{
				parameters[0].IsThis = true;
			}
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
			catch (ArgumentException)
			{
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
			if (typeof(Task).IsAssignableFrom(info.ReturnType))
			{
				var task = (Task)result;
				task.Wait();
				if (info.ReturnType.IsGenericType)
				{
					var declaredType = info.ReturnType.GetGenericArguments()[0];
					var taskResult = info.ReturnType.GetProperty("Result").GetValue(task);
					var output = Cli.CreateOutput(taskResult, declaredType);
					Console.WriteLine();
					output.Write();
				}
			}
			else if (info.ReturnType != typeof(void))
			{
				var output = Cli.CreateOutput(result, info.ReturnType);
				Console.WriteLine();
				output.Write();
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
					var typeName = param.Type.IsGenericType && param.Type.GetGenericTypeDefinition() == typeof(Nullable<>)
						? param.Type.GetGenericArguments()[0].Name
						: param.Type.Name;
					var token = $"--{param.Name} <{typeName}>";
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

		/// <summary>
		/// Converts the input string to the specified <see cref="Parameter"/> type, so that the
		/// method can be invoked.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <param name="parameter">The <see cref="Parameter"/> which determines the type.</param>
		/// <returns>
		/// An instance of the parameter type (can be null).
		/// </returns>
		private static object ConvertType(string input, Parameter parameter)
		{
			if (input != null)
			{
				var paramValue = ConvertType(input, parameter.Type);
				if (paramValue.Item1)
				{
					return paramValue.Item2;
				}
				else
				{
					throw new ArgumentException($"Could not convert \"{input}\" to type \"{parameter.Type}\".");
				}
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
		/// Converts the input string to the specified <see cref="Type"/>, so that the method can
		/// be invoked.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <param name="type">The <see cref="Type"/>..</param>
		/// <returns>
		/// An instance of this type (can be null).
		/// </returns>
		private static Tuple<bool, object> ConvertType(string input, Type type)
		{
			Tuple<bool, object> result;

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				result = ConvertType(input, type.GetGenericArguments()[0]);
				if (result.Item1)
				{
					return result;
				}
			}

			if (type.IsEnum)
			{
				try
				{
					return Tuple.Create(true, Enum.Parse(type, input, true));
				}
				catch (Exception)
				{
					return new Tuple<bool, object>(false, null);
				}
			}

			if (type == typeof(Guid))
			{
				if (Guid.TryParse(input, out var guid))
				{
					return Tuple.Create(true, (object)guid);
				}
			}

			try
			{
				return Tuple.Create(true, Convert.ChangeType(input, type));
			}
			catch (Exception)
			{
				return new Tuple<bool, object>(false, null);
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
		private static IDictionary<Parameter, object> ParseArgs(IEnumerable<Parameter> parameters, string[] args)
		{
			var results = new Dictionary<Parameter, object>();
			Parameter? param = null;
			foreach (var arg in args)
			{
				if (arg.StartsWith("--"))
				{
					if (param != null)
					{
						throw new ArgumentException($"Expected value for {param.Value.Name}, but token was \"{arg}\".");
					}

					// Structs don't default to null, so check for a name
					param = parameters.FirstOrDefault(x => x.Name.Equals(arg.Substring(2), StringComparison.OrdinalIgnoreCase));
					if (param.Value.Name == null)
					{
						throw new ArgumentException($"Unknown parameter \"{arg}\".");
					}
					else if (results.ContainsKey(param.Value))
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
					results[param.Value] = ConvertType(arg, param.Value);
					param = null;
				}
			}

			if (param != null)
			{
				throw new ArgumentException($"Expected value for {param.Value.Name}, but no more arguments.");
			}

			return results;
		}

		/// <summary>
		/// Describes a parameter with enough information to be used through the execute process.
		/// </summary>
		private struct Parameter
		{
			public object DefaultValue;
			public bool IsDefault;
			public bool IsThis;
			public string Name;
			public Type Type;
		}
	}
}
