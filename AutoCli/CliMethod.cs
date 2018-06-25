using AutoCli.Attributes;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AutoCli
{
	internal class CliMethod
	{
		private readonly MethodInfo info;
		
		private readonly string[] requiredParameters;
		private readonly string[] optionalParameters;
		
		public CliMethod(Type serviceType, string service, MethodInfo info)
		{
			this.info = info;

			ServiceType = serviceType;
			Service = service;

			var methodAttr = info.GetCustomAttribute<CliMethodAttribute>(true);
			if (methodAttr?.Name != null)
			{
				Method = methodAttr.Name;
			}
			else if (info.Name.EndsWith("Async"))
			{
				Method = info.Name.Substring(0, info.Name.Length - 5);
			}
			else
			{
				Method = info.Name;
			}

			var parameters = info.GetParameters();

			requiredParameters = parameters
				.Where(x => !x.HasDefaultValue)
				.Select(x => GetParameterName(x))
				.ToArray();
			optionalParameters = parameters
				.Where(x => x.HasDefaultValue)
				.Select(x => GetParameterName(x))
				.ToArray();
		}

		public Type ServiceType { get; }
		public string Service { get; }
		public string Method { get; }

		public void Execute(object service, string[] args)
		{
			var infos = info.GetParameters();
			var parameters = new object[infos.Length];

			int i = 0;
			foreach (var info in infos)
			{
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

				++i;
			}

			var result = info.Invoke(service, parameters);
			if (result is Task)
			{
				((Task)result).Wait();
			}
		}

		public bool IsMatch(string[] args)
		{
			if (args == null || args.Length < 2) return false;

			if (!args[0].Equals(Service, StringComparison.OrdinalIgnoreCase)) return false;
			if (!args[1].Equals(Method, StringComparison.OrdinalIgnoreCase)) return false;

			// TODO: Include value and check if convertable to parameter type
			var paramNames = args
				.Skip(2)
				.Where(x => x.StartsWith("--"))
				.Select(x => x.Substring(2))
				.ToArray();

			// All required parameters must exist to match
			if (requiredParameters.Any(x => !paramNames.Contains(x, StringComparer.OrdinalIgnoreCase))) return false;

			var otherParamNames = paramNames
				.Except(requiredParameters, StringComparer.OrdinalIgnoreCase)
				.ToArray();

			// All others must be in optional parameters
			if (otherParamNames.Any(x => !optionalParameters.Contains(x, StringComparer.OrdinalIgnoreCase))) return false;

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
