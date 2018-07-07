﻿using AutoCli.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AutoCli.Representation
{
	internal class CliMethod
	{
		private readonly CliServiceAttribute serviceAttribute;
		private readonly CliMethodAttribute methodAttribute;
		private readonly MethodInfo info;
		private readonly bool isExtension;

		public CliMethod(Type serviceType, CliServiceAttribute serviceAttribute, MethodInfo info)
		{
			this.serviceAttribute = serviceAttribute;
			this.info = info;

			isExtension = info.IsDefined(typeof(ExtensionAttribute), false);

			ServiceType = serviceType;

			methodAttribute = info.GetCustomAttribute<CliMethodAttribute>(true);
			if (methodAttribute?.Name != null)
			{
				Method = methodAttribute.Name;
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

		public Type ServiceType { get; }
		public string Service => serviceAttribute.Name;
		public string ServiceDescription => serviceAttribute.Description;
		public string Method { get; }
		public string MethodDescription => methodAttribute?.Description;
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
					Output(info.ReturnType.GetProperty("Result").GetValue(task));
				}
			}
			else if (info.ReturnType != typeof(void))
			{
				Output(result);
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
			if (RequiredParameters.Any(x => !paramNames.Contains(x, StringComparer.OrdinalIgnoreCase))) return false;

			var otherParamNames = paramNames
				.Except(RequiredParameters, StringComparer.OrdinalIgnoreCase)
				.ToArray();

			// All others must be in optional parameters
			if (otherParamNames.Any(x => !OptionalParameters.Contains(x, StringComparer.OrdinalIgnoreCase))) return false;

			return true;
		}

		private static void Output(object result)
		{
			if (result is IEnumerable enumerable)
			{
				foreach (var entry in enumerable)
				{
					Console.WriteLine(entry);
				}
			}
			else
			{
				Console.WriteLine(result);
			}
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