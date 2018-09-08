using AutoCli.Attributes;
using AutoCli.Representation;
using AutoCli.Resolvers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AutoCli
{
	/// <summary>
	/// The command line interface root, which allows global configuration and service methods to
	/// be added, and executes the provided input arguments.
	/// </summary>
	public class Cli
	{
		private readonly List<CliService> services;

		private string description;
		private IResolver resolver;

		/// <summary>
		/// Initializes a new instance of the <see cref="Cli"/> class.
		/// </summary>
		private Cli()
		{
			resolver = new Resolver(Activator.CreateInstance);
			services = new List<CliService>();
		}

		/// <summary>
		/// Gets the application name currently executing (without path or extension).
		/// </summary>
		public static string AppName => Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

		/// <summary>
		/// Gets a new <see cref="Cli"/> instance to start fluent usage.
		/// </summary>
		public static Cli Builder => new Cli();

		/// <summary>
		/// Adds extension methods to the services added to this <see cref="Cli"/> instance from
		/// all available assemblies.
		/// </summary>
		/// <remarks>
		/// The extension classes must be decorated with <see cref="CliExtensionsAttribute"/> to be
		/// found using this method.
		/// </remarks>
		/// <returns>
		/// This <see cref="Cli"/> instance.
		/// </returns>
		public Cli AddExtensions()
		{
			var methods = AppDomain.CurrentDomain
				.GetAssemblies()
				.AsParallel()
				.SelectMany(x => x.GetExportedTypes().Where(t => t.GetCustomAttribute<CliExtensionsAttribute>() != null))
				.SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public))
				.Where(x => x.IsDefined(typeof(ExtensionAttribute), false))
				.Select(x => new { Method = x, Parameters = x.GetParameters() })
				.Where(x => x.Parameters.Length > 0 && services.Any(s => s.Type == x.Parameters[0].ParameterType))
				.GroupBy(x => x.Parameters[0].ParameterType, x => x.Method)
				.ToArray();

			foreach (var group in methods)
			{
				GetService(group.Key).AddMethods(group);
			}
			
			return this;
		}

		/// <summary>
		/// Adds the service type <typeparamref name="T"/> to this <see cref="Cli"/> instance.
		/// </summary>
		/// <returns>
		/// This <see cref="Cli"/> instance.
		/// </returns>
		public Cli AddService<T>()
		{
			return AddService(typeof(T));
		}

		/// <summary>
		/// Adds the service type to this <see cref="Cli"/> instance.
		/// </summary>
		/// <param name="serviceType">The service type.</param>
		/// <returns>
		/// This <see cref="Cli"/> instance.
		/// </returns>
		public Cli AddService(Type serviceType)
		{
			var methods = serviceType
				.GetMethods()
				.Where(x => x.GetCustomAttribute<CliMethodAttribute>(true) != null)
				.ToArray();

			GetService(serviceType).AddMethods(methods);

			return this;
		}

		/// <summary>
		/// Executes the provided input arguments against this <see cref="Cli"/> instance, either
		/// invoking the appropriate service or showing help information.
		/// </summary>
		/// <param name="args">The input arguments.</param>
		public void Execute(string[] args)
		{
			if (args.Length == 1)
			{
				if (args[0] == "--help")
				{
					ShowHelp();
					return;
				}
				else if (args[0] == "-v" || args[0] == "--version")
				{
					var version = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
					Console.WriteLine($"{AppName} version {version.FileVersion}");
					return;
				}
			}

			var handled = false;
			if (args.Length > 0)
			{
				var done = services.FirstOrDefault(x => x.Execute(args));
				if (done != null)
				{
					handled = true;
				}
			}

			if (!handled)
			{
				ShowHelp();
			}
		}

		/// <summary>
		/// Returns an instance of the specified service type using the configured resolver.
		/// </summary>
		/// <param name="serviceType">The service type.</param>
		/// <returns>
		/// An instance of the type.
		/// </returns>
		internal object Resolve(Type serviceType)
		{
			return resolver.Resolve(serviceType);
		}

		/// <summary>
		/// Sets the <see cref="Cli"/> description text, for showing help information.
		/// </summary>
		/// <param name="description">The CLI description text.</param>
		/// <returns>
		/// This <see cref="Cli"/> instance.
		/// </returns>
		public Cli SetDescription(string description)
		{
			if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("The description cannot be null, empty, or whitespace.", nameof(description));
			this.description = description;
			return this;
		}

		/// <summary>
		/// Sets the <see cref="Cli"/> resolver function, used to instantiate service instances to invoke.
		/// </summary>
		/// <param name="resolver">The CLI resolver function.</param>
		/// <returns>
		/// This <see cref="Cli"/> instance.
		/// </returns>
		public Cli SetResolver(Func<Type, object> resolver)
		{
			this.resolver = new Resolver(resolver) ?? throw new ArgumentNullException(nameof(resolver));
			return this;
		}

		/// <summary>
		/// Sets the <see cref="Cli"/> <see cref="IResolver"/>, used to instantiate service instances to invoke.
		/// </summary>
		/// <param name="resolver">The CLI <see cref="IResolver"/>.</param>
		/// <returns>
		/// This <see cref="Cli"/> instance.
		/// </returns>
		public Cli SetResolver(IResolver resolver)
		{
			this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
			return this;
		}

		/// <summary>
		/// Returns the <see cref="CliService"/> for the specified <see cref="Type"/>, creating it
		/// if it does not already exist.
		/// </summary>
		/// <param name="serviceType">The service type.</param>
		/// <returns>
		/// A new or existing <see cref="CliService"/> instance.
		/// </returns>
		private CliService GetService(Type serviceType)
		{
			var service = services.FirstOrDefault(x => x.Type == serviceType);
			if (service == null)
			{
				services.Add(service = new CliService(this, serviceType));
			}

			return service;
		}

		/// <summary>
		/// Writes the <see cref="Cli"/> help information to the console, which includes root
		/// options and services.
		/// </summary>
		private void ShowHelp()
		{
			Console.WriteLine($"\nUsage: {AppName} SERVICE METHOD\n");

			if (!string.IsNullOrWhiteSpace(description))
			{
				Console.WriteLine($"{description}\n");
			}

			Console.WriteLine("Options:");
			Console.WriteLine("      --help     Show help information");
			Console.WriteLine("  -v, --version  Show version");
			Console.WriteLine();

			Console.WriteLine("Services:");
			var padding = services.Max(x => x.Name.Length);
			foreach (var s in services.OrderBy(x => x.Name))
			{
				// TODO: Wrap description based on console width
				Console.WriteLine($"  {s.Name.PadRight(padding)}  {s.Description}");
			}
		}
	}
}
