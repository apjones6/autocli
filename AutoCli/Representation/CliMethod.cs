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
		private readonly List<CliParameters> parameters;

		public CliMethod(CliService service, MethodInfo info)
		{
			this.service = service ?? throw new ArgumentNullException(nameof(service));
			
			parameters = new List<CliParameters>();

			Description = info.GetCustomAttribute<CliMethodAttribute>(true)?.Description;
			Name = GetMethodName(info);
		}
		
		public string Description { get; private set; }
		public string Name { get; }

		public void AddMethod(MethodInfo info)
		{
			var description = info.GetCustomAttribute<CliMethodAttribute>(true)?.Description;
			if (string.IsNullOrWhiteSpace(Description))
			{
				Description = description;
			}
			else if (!string.IsNullOrWhiteSpace(description) && Description != description)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine($"Multiple descriptions found for method \"{Name}\":");
				Console.WriteLine($"  {Description}");
				Console.WriteLine($"  {description}");
				Console.ResetColor();
			}

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
			foreach (var p in parameters)
			{
				p.ShowHelp();
			}
		}
	}
}
