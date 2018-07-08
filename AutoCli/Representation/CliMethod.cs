using AutoCli.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoCli.Representation
{
	internal class CliMethod
	{
		private readonly CliService service;
		private readonly CliMethodAttribute methodAttribute;
		private readonly List<CliParameters> parameters;
		private readonly MethodInfo info;

		public CliMethod(CliService service, MethodInfo info)
		{
			this.service = service ?? throw new ArgumentNullException(nameof(service));
			this.info = info ?? throw new ArgumentNullException(nameof(info));
			
			methodAttribute = info.GetCustomAttribute<CliMethodAttribute>(true);
			parameters = new List<CliParameters>();

			Name = GetMethodName(info);
		}
		
		public string Description => methodAttribute?.Description;
		public string Name { get; }

		public void AddMethod(MethodInfo info)
		{
			parameters.Add(new CliParameters(this, info));
		}

		public bool Execute(string[] args)
		{
			if (!args[0].Equals(Name, StringComparison.OrdinalIgnoreCase)) return false;

			args = args.Skip(1).ToArray();

			var handled = false;
			var done = parameters.FirstOrDefault(x => x.Execute(args));
			if (done != null)
			{
				handled = true;
			}

			if (!handled)
			{
				ShowHelp();
			}

			return true;
		}

		public object GetInstance()
		{
			return service.GetInstance();
		}

		public static string GetMethodName(MethodInfo info)
		{
			var attribute = info.GetCustomAttribute<CliMethodAttribute>(true);
			if (attribute?.Name != null)
			{
				return attribute.Name;
			}
			else if (info.Name.EndsWith("Async"))
			{
				return info.Name.Substring(0, info.Name.Length - 5);
			}
			else
			{
				return info.Name;
			}
		}

		public void ShowHelp()
		{
			Console.WriteLine($"\nUsage: {Cli.AppName} {service.Name} {Name} params...\n");

			if (!string.IsNullOrWhiteSpace(Description))
			{
				Console.WriteLine($"{Description}\n");
			}

			Console.WriteLine($"Parameters:");
			foreach (var method in parameters)
			{
				Console.Write(" ");
				foreach (var reqParam in method.RequiredParameters) Console.Write($" --{reqParam} <value>");
				foreach (var optParam in method.OptionalParameters) Console.Write($" [--{optParam} <value>]");
				Console.WriteLine();
			}
		}
	}
}
