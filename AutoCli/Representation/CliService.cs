using AutoCli.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoCli.Representation
{
	/// <summary>
	/// A single <see cref="CliService"/> which matches a given service name, and includes one or
	/// more methods which can be invoked.
	/// </summary>
	internal class CliService
    {
		private readonly CliServiceAttribute attribute;
		private readonly List<CliMethod> methods;

		/// <summary>
		/// Initializes a new instance of the <see cref="CliService"/> class for the specified
		/// <see cref="Cli"/> and service type.
		/// </summary>
		/// <param name="cli">The CLI instance.</param>
		/// <param name="serviceType">The service type.</param>
		public CliService(Cli cli, Type serviceType)
		{
			Cli = cli ?? throw new ArgumentNullException(nameof(cli));
			Type = serviceType ?? throw new ArgumentNullException(nameof(serviceType));

			attribute = serviceType.GetCustomAttribute<CliServiceAttribute>(false);
			methods = new List<CliMethod>();
		}

		/// <summary>
		/// Gets the <see cref="AutoCli.Cli"/> instance.
		/// </summary>
		public Cli Cli { get; }

		/// <summary>
		/// Gets the service description.
		/// </summary>
		public string Description => attribute?.Description;
		
		/// <summary>
		/// Gets the service name.
		/// </summary>
		public string Name => Cli.ApplyNameConvention(attribute?.Name ?? GetServiceName(Type));

		/// <summary>
		/// Gets the service instance type.
		/// </summary>
		public Type Type { get; }
		
		/// <summary>
		/// Adds one or more methods to the service, using the provided <see cref="MethodInfo"/>
		/// instances.
		/// </summary>
		/// <param name="infos">The <see cref="MethodInfo"/> instances to add.</param>
		public void AddMethods(IEnumerable<MethodInfo> infos)
		{
			foreach (var info in infos)
			{
				GetMethod(info).AddMethod(info);
			}
		}

		/// <summary>
		/// Executes the provided input arguments against this <see cref="CliService"/> instance,
		/// either invoking the appropriate method or showing help information.
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
			if (args.Length > 0)
			{
				var done = methods.FirstOrDefault(x => x.Execute(args));
				if (done != null)
				{
					handled = true;
				}
			}

			if (!handled)
			{
				ShowHelp();
			}

			return true;
		}

		/// <summary>
		/// Returns an instance of the service type using the configured <see cref="Cli"/>.
		/// </summary>
		/// <returns>
		/// An instance of the type.
		/// </returns>
		internal object Resolve()
		{
			return Cli.Resolve(Type);
		}

		/// <summary>
		/// Returns the <see cref="CliMethod"/> for the provided <see cref="MethodInfo"/>, creating
		/// it if it does not already exist.
		/// </summary>
		/// <param name="info">The method info.</param>
		/// <returns>
		/// A new or existing <see cref="CliMethod"/> instance.
		/// </returns>
		private CliMethod GetMethod(MethodInfo info)
		{
			var name = CliMethod.GetMethodName(info);
			var method = methods.FirstOrDefault(x => x.Name == name);
			if (method == null)
			{
				methods.Add(method = new CliMethod(this, info));
			}

			return method;
		}

		/// <summary>
		/// Returns the CLI argument name to use for the specified service type, if the type has
		/// not been decorated with <see cref="CliServiceAttribute"/>.
		/// </summary>
		/// <param name="serviceType">The service type.</param>
		/// <returns>
		/// The service name to use.
		/// </returns>
		private static string GetServiceName(Type serviceType)
		{
			var name = serviceType.Name;

			// Trim 'I' prefix from interfaces
			if (serviceType.IsInterface && name[0] == 'I')
			{
				name = name.Substring(1);
			}

			// Trim standard service suffixes
			if (name.EndsWith("Service"))
			{
				name = name.Substring(0, name.Length - 7);
			}

			return name;
		}

		/// <summary>
		/// Writes the <see cref="CliService"/> help information to the console, which includes
		/// methods.
		/// </summary>
		private void ShowHelp()
		{
			Console.WriteLine($"\nUsage: {Cli.AppName} {Name} METHOD\n");

			var description = Description;
			if (!string.IsNullOrWhiteSpace(description))
			{
				Console.WriteLine($"{description}\n");
			}
			
			Console.WriteLine($"Methods:");
			var padding = methods.Max(x => x.Name.Length);
			foreach (var m in methods.GroupBy(x => x.Name).Select(x => x.First()).OrderBy(x => x.Name))
			{
				Console.WriteLine($"  {m.Name.PadRight(padding)}  {m.Description}");
			}
		}
	}
}
