using AutoCli.Attributes;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AutoCli.Representation
{
	internal class CliParameters
	{
		private readonly CliMethod method;
		private readonly MethodInfo info;
		private readonly bool isExtension;

		public CliParameters(CliMethod method, MethodInfo info)
		{
			this.method = method ?? throw new ArgumentNullException(nameof(method));
			this.info = info ?? throw new ArgumentNullException(nameof(info));

			isExtension = info.IsDefined(typeof(ExtensionAttribute), false);
			
			var parameters = info.GetParameters();

			if (isExtension)
			{
				parameters = parameters.Skip(1).ToArray();
			}

			RequiredParameters = parameters
				.Where(x => !x.HasDefaultValue)
				.Select(x => GetParameterName(x))
				.ToArray();
			OptionalParameters = parameters
				.Where(x => x.HasDefaultValue)
				.Select(x => GetParameterName(x))
				.ToArray();
		}
		
		public string[] RequiredParameters { get; }
		public string[] OptionalParameters { get; }

		public void Execute(object service, string[] args)
		{
			var infos = info.GetParameters();
			var parameters = new object[infos.Length];

			int i = 0;

			if (isExtension)
			{
				parameters[0] = service;
				++i;
			}

			for (; i < infos.Length; ++i)
			{
				var info = infos[i];
				var name = GetParameterName(info);
				var argIndex = Array.FindIndex(args, x => x.Equals($"--{name}", StringComparison.OrdinalIgnoreCase));
				if (argIndex != -1 && args.Length > argIndex + 1)
				{
					var paramValue = ConvertType(args[argIndex + 1], info.ParameterType);
					if (paramValue.Item1)
					{
						parameters[i] = paramValue.Item2;
					}
					else
					{
						parameters[i] = GetDefault(info.ParameterType);
					}
				}
				else
				{
					parameters[i] = info.HasDefaultValue ? info.DefaultValue : GetDefault(info.ParameterType);
				}
			}

			var result = info.Invoke(service, parameters);

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
					var output = new CliOutput(taskResult, declaredType);
					output.Write();
				}
			}
			else if (info.ReturnType != typeof(void))
			{
				var output = new CliOutput(result, info.ReturnType);
				output.Write();
			}
		}

		public bool Execute(string[] args)
		{
			// TODO: Include value and check if convertable to parameter type
			// TODO: Use a state machine strategy
			var paramNames = args
				.Where(x => x.StartsWith("--"))
				.Select(x => x.Substring(2))
				.ToArray();

			// All required parameters must exist to match
			if (RequiredParameters.Any(x => !paramNames.Contains(x, StringComparer.OrdinalIgnoreCase))) return false;

			var otherParamNames = paramNames
				.Except(RequiredParameters, StringComparer.OrdinalIgnoreCase)
				.ToArray();

			// All others must be in optional parameters
			if (otherParamNames.Any(x => !OptionalParameters.Contains(x, StringComparer.OrdinalIgnoreCase))) return false;

			// EXECUTE

			var infos = info.GetParameters();
			var parameters = new object[infos.Length];

			int i = 0;

			if (isExtension)
			{
				parameters[0] = method.GetInstance();
				++i;
			}

			for (; i < infos.Length; ++i)
			{
				var info = infos[i];
				var name = GetParameterName(info);
				var argIndex = Array.FindIndex(args, x => x.Equals($"--{name}", StringComparison.OrdinalIgnoreCase));
				if (argIndex != -1 && args.Length > argIndex + 1)
				{
					var paramValue = ConvertType(args[argIndex + 1], info.ParameterType);
					if (paramValue.Item1)
					{
						parameters[i] = paramValue.Item2;
					}
					else
					{
						parameters[i] = GetDefault(info.ParameterType);
					}
				}
				else
				{
					parameters[i] = info.HasDefaultValue ? info.DefaultValue : GetDefault(info.ParameterType);
				}
			}

			var result = !isExtension
				? info.Invoke(method.GetInstance(), parameters)
				: info.Invoke(null, parameters);

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
					var output = new CliOutput(taskResult, declaredType);
					output.Write();
				}
			}
			else if (info.ReturnType != typeof(void))
			{
				var output = new CliOutput(result, info.ReturnType);
				output.Write();
			}

			return true;
		}
		
		private static string GetParameterName(ParameterInfo info)
		{
			var attr = info.GetCustomAttribute<CliParameterAttribute>(true);
			if (attr != null && attr.Name != null)
			{
				return attr.Name;
			}

			return info.Name;
		}

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

		private static object GetDefault(Type type)
		{
			if (type.IsValueType)
			{
				return Activator.CreateInstance(type);
			}

			return null;
		}
	}
}
