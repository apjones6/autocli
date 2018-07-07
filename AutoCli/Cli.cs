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

		private Resolver resolver;

		public string Description { get; set; }
		
		public MethodStrategy MethodStrategy { get; set; }

		public Resolver Resolver
		{
			get { return resolver ?? (resolver = new Resolver(Activator.CreateInstance)); }
			set { resolver = value; }
		}

		public void AddServiceExtensions<T>(Type extensionsType)
		{
			AddServiceExtensions(typeof(T), extensionsType);
		}

		public void AddService<T>()
		{
			AddService(typeof(T));
		}

		public void AddServiceExtensions(Type serviceType, Type extensionsType)
		{
			var attr = GetServiceAttribute(serviceType);
			var strategy = UnlessDefault(attr.MethodStrategy) ?? UnlessDefault(MethodStrategy) ?? MethodStrategy.Explicit;
			var filter = strategy == MethodStrategy.Explicit
				? (Func<MethodInfo, bool>)((MethodInfo x) => x.GetCustomAttribute<CliMethodAttribute>(true) != null)
				: (MethodInfo x) => true;
			var methods = extensionsType
				.GetMethods(BindingFlags.Static | BindingFlags.Public)
				.Where(x => x.IsDefined(typeof(ExtensionAttribute), false) && x.GetParameters()[0].ParameterType == serviceType)
				.Where(filter)
				.Select(x => new CliMethod(serviceType, attr, x))
				.ToArray();

			this.methods.AddRange(methods);
		}

		public void AddService(Type serviceType)
		{
			var attr = GetServiceAttribute(serviceType);
			var strategy = UnlessDefault(attr.MethodStrategy) ?? UnlessDefault(MethodStrategy) ?? MethodStrategy.Explicit;
			var filter = strategy == MethodStrategy.Explicit
				? (Func<MethodInfo, bool>)((MethodInfo x) => x.GetCustomAttribute<CliMethodAttribute>(true) != null)
				: (MethodInfo x) => true;
			var methods = serviceType
				.GetMethods()
				.Where(filter)
				.Select(x => new CliMethod(serviceType, attr, x))
				.ToArray();

			this.methods.AddRange(methods);
		}

		public void Execute(string[] args)
		{
			var assembly = Assembly.GetEntryAssembly();
			var app = Path.GetFileName(assembly.Location);

			if (args.Length == 1)
			{
				if (args[0] == "-v" || args[0] == "--version")
				{
					var version = FileVersionInfo.GetVersionInfo(assembly.Location);
					Console.WriteLine($"{app} version {version.FileVersion}");
					return;
				}
			}

			var matches = methods.Where(x => x.IsMatch(args)).ToArray();

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

			Console.WriteLine("Executing...");

			var serviceInstance = Resolver.Resolve(matches[0].ServiceType);
			matches[0].Execute(serviceInstance, args);
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

		private static MethodStrategy? UnlessDefault(MethodStrategy strategy)
		{
			return strategy != MethodStrategy.Default
				? (MethodStrategy?)strategy
				: null;
		}
	}
}
