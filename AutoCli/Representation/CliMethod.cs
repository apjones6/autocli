using AutoCli.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoCli.Representation
{
	/// <summary>
	/// A single <see cref="CliMethod"/> which matches a given method name, and includes one or
	/// more parameter combinations which can be invoked.
	/// </summary>
	internal class CliMethod
	{
		private readonly CliService service;
		private readonly List<CliParameters> parameters;

		/// <summary>
		/// Initializes a new instance of the <see cref="CliMethod"/> class for the specified
		/// <see cref="CliService"/> and <see cref="MethodInfo"/>.
		/// </summary>
		/// <param name="service">The <see cref="CliService"/> instance.</param>
		/// <param name="info">The method info.</param>
		public CliMethod(CliService service, MethodInfo info)
		{
			this.service = service ?? throw new ArgumentNullException(nameof(service));
			
			parameters = new List<CliParameters>();

			Description = info.GetCustomAttribute<CliMethodAttribute>(true)?.Description;
			Name = GetMethodName(info);
		}

		/// <summary>
		/// Gets the method description.
		/// </summary>
		public string Description { get; private set; }

		/// <summary>
		/// Gets the method name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Adds the provided <see cref="MethodInfo"/> to this <see cref="CliMethod"/> instance as
		/// an additional parameter combination which can be invoked.
		/// </summary>
		/// <param name="info">The method info.</param>
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

		/// <summary>
		/// Executes the provided input arguments against this <see cref="CliMethod"/> instance,
		/// either invoking the appropriate parameter combination or showing help information.
		/// </summary>
		/// <param name="args">The input arguments.</param>
		/// <returns>
		/// True if CLI execution should stop.
		/// </returns>
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

		/// <summary>
		/// Returns an instance of the service type using the configured <see cref="CliService"/>.
		/// </summary>
		/// <returns>
		/// An instance of the type.
		/// </returns>
		internal object Resolve()
		{
			return service.Resolve();
		}

		/// <summary>
		/// Returns the <see cref="CliMethod"/> name to use for the provided <see cref="MethodInfo"/>.
		/// </summary>
		/// <param name="info">The method info.</param>
		/// <returns>
		/// A string name.
		/// </returns>
		internal static string GetMethodName(MethodInfo info)
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

		/// <summary>
		/// Writes the <see cref="CliMethod"/> help information to the console, which includes
		/// any parameters combinations.
		/// </summary>
		private void ShowHelp()
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
