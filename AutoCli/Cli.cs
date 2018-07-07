using AutoCli.Attributes;
using AutoCli.Representation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AutoCli
{
	public class Cli
    {
		private readonly List<CliMethod> methods = new List<CliMethod>();
		private readonly ISet<Type> serviceTypes = new HashSet<Type>();

		private Resolver resolver;

		public string Description { get; set; }
		
		public Resolver Resolver
		{
			get { return resolver ?? (resolver = new Resolver(Activator.CreateInstance)); }
			set { resolver = value; }
		}

		public Cli AddExtensions()
		{
			var serviceAttributes = serviceTypes.ToDictionary(x => x, x => GetServiceAttribute(x));
			var methods = AppDomain.CurrentDomain
				.GetAssemblies()
				.AsParallel()
				.SelectMany(x => x.GetExportedTypes().Where(t => t.GetCustomAttribute<CliExtensionsAttribute>() != null))
				.SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public))
				.Where(x => x.IsDefined(typeof(ExtensionAttribute), false))
				.Select(x => new { Method = x, Parameters = x.GetParameters() })
				.Where(x => x.Parameters.Length > 0 && serviceTypes.Contains(x.Parameters[0].ParameterType))
				.Select(x => new CliMethod(x.Parameters[0].ParameterType, serviceAttributes[x.Parameters[0].ParameterType], x.Method))
				.ToArray();

			this.methods.AddRange(methods);

			return this;
		}
		
		public Cli AddService<T>()
		{
			return AddService(typeof(T));
		}
		
		public Cli AddService(Type serviceType)
		{
			var attr = GetServiceAttribute(serviceType);
			var cliMethods = serviceType
				.GetMethods()
				.Where(x => x.GetCustomAttribute<CliMethodAttribute>(true) != null)
				.Select(x => new CliMethod(serviceType, attr, x))
				.ToArray();

			serviceTypes.Add(serviceType);
			methods.AddRange(cliMethods);

			return this;
		}

		public static Cli Builder => new Cli();
		
		public void Execute(string[] args)
		{
			var assembly = Assembly.GetEntryAssembly();
			var app = Path.GetFileNameWithoutExtension(assembly.Location);
			var showHelp = false;

			if (args.Length == 1)
			{
				if (args[0] == "--help")
				{
					showHelp = true;
				}
				else if (args[0] == "-v" || args[0] == "--version")
				{
					var version = FileVersionInfo.GetVersionInfo(assembly.Location);
					Console.WriteLine($"{app} version {version.FileVersion}");
					return;
				}
			}
			
			var matches = !showHelp
				? methods.Where(x => x.IsMatch(args)).ToArray()
				: new CliMethod[0];

			if (matches.Length > 1)
			{
				Console.WriteLine("Multiple candidates found for the provided arguments.");
				return;
			}
			else if (matches.Length == 0)
			{
				if (args.Length == 0 || !methods.Any(x => x.Service.Equals(args[0], StringComparison.OrdinalIgnoreCase)))
				{
					Console.WriteLine($"\nUsage: {app} SERVICE METHOD\n");

					if (!string.IsNullOrWhiteSpace(Description))
					{
						Console.WriteLine($"{Description}\n");
					}

					Console.WriteLine("Options:");
					Console.WriteLine("      --help     Show help information");
					Console.WriteLine("  -v, --version  Show version");
					Console.WriteLine();

					Console.WriteLine("Services:");
					var padding = methods.Max(x => x.Service.Length);
					foreach (var s in methods.GroupBy(x => x.Service).Select(x => x.First()).OrderBy(x => x.Service))
					{
						// TODO: Wrap description based on console width
						Console.WriteLine($"  {s.Service.PadRight(padding)}  {s.ServiceDescription}");
					}
				}
				else if (args.Length == 1 || !methods.Any(x => x.Service.Equals(args[0], StringComparison.OrdinalIgnoreCase) && x.Method.Equals(args[1], StringComparison.OrdinalIgnoreCase)))
				{
					var method = methods.First(x => x.Service.Equals(args[0], StringComparison.OrdinalIgnoreCase));
					Console.WriteLine($"\nUsage: {app} {method.Service} METHOD\n");

					if (!string.IsNullOrWhiteSpace(method.ServiceDescription))
					{
						Console.WriteLine($"{method.ServiceDescription}\n");
					}

					Console.WriteLine($"Methods:");
					var filteredMethods = methods.Where(x => x.Service == method.Service);
					var padding = filteredMethods.Max(x => x.Method.Length);
					foreach (var m in filteredMethods.GroupBy(x => x.Method).Select(x => x.First()).OrderBy(x => x.Method))
					{
						Console.WriteLine($"  {m.Method.PadRight(padding)}  {m.MethodDescription}");
					}
				}
				else
				{
					var method = methods.First(x => x.Service.Equals(args[0], StringComparison.OrdinalIgnoreCase) && x.Method.Equals(args[1], StringComparison.OrdinalIgnoreCase));
					Console.WriteLine($"\nUsage: {app} {method.Service} {method.Method} params...\n");

					if (!string.IsNullOrWhiteSpace(method.MethodDescription))
					{
						Console.WriteLine($"{method.MethodDescription}\n");
					}

					Console.WriteLine($"Parameters:");
					var methodOptions = methods
						.Where(x => x.Service == method.Service && x.Method == method.Method)
						.ToArray();
					foreach (var m in methodOptions)
					{
						Console.Write(" ");
						foreach (var reqParam in m.RequiredParameters) Console.Write($" --{reqParam} <value>");
						foreach (var optParam in m.OptionalParameters) Console.Write($" [--{optParam} <value>]");
						Console.WriteLine();
					}
				}

				return;
			}

			object serviceInstance;
			try
			{
				serviceInstance = Resolver.Resolve(matches[0].ServiceType);
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"Unable to resolve service type: {matches[0].ServiceType}");
				Console.WriteLine(ex);
				Console.ResetColor();
				return;
			}

			matches[0].Execute(serviceInstance, args);
		}

		public Cli SetDescription(string description)
		{
			Description = description;
			return this;
		}
		
		public Cli SetResolver(Func<Type, object> resolver)
		{
			Resolver = new Resolver(resolver);
			return this;
		}

		public Cli SetResolver(Resolver resolver)
		{
			Resolver = resolver;
			return this;
		}

		private static CliServiceAttribute GetServiceAttribute(Type serviceType)
		{
			if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

			// Find explicit attribute, else find on the interfaces
			var attr = serviceType.GetCustomAttribute<CliServiceAttribute>(true);
			if (attr == null)
			{
				attr = serviceType.GetInterfaces().Select(x => x.GetCustomAttribute<CliServiceAttribute>(true)).FirstOrDefault();
				if (attr == null)
				{
					throw new ArgumentException("The service must be decorated with AutoCli.Attributes.CliServiceAttribute.", nameof(serviceType));
				}
			}

			return attr;
		}
	}
}
