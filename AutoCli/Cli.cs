using AutoCli.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AutoCli
{
	public class Cli
    {
		private readonly List<CliMethod> methods = new List<CliMethod>();

		private Resolver resolver;
		
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
				.Select(x => new CliMethod(serviceType, attr.Name, x))
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
				.Select(x => new CliMethod(serviceType, attr.Name, x))
				.ToArray();

			this.methods.AddRange(methods);
		}

		public void Execute(string[] args)
		{
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
					Console.WriteLine("\nUsage: app SERVICE METHOD\n");

					Console.WriteLine("Services:");
					foreach (var name in methods.Select(x => x.Service).Distinct().OrderBy(x => x))
					{
						Console.WriteLine($"  {name}");
					}
				}
				else if (args.Length == 1 || !methods.Any(x => x.Service.Equals(args[0], StringComparison.OrdinalIgnoreCase) && x.Method.Equals(args[1], StringComparison.OrdinalIgnoreCase)))
				{
					var serviceName = methods.First(x => x.Service.Equals(args[0], StringComparison.OrdinalIgnoreCase)).Service;
					Console.WriteLine($"\nUsage: app {serviceName} METHOD\n");

					Console.WriteLine($"Methods:");
					foreach (var name in methods.Where(x => x.Service == serviceName).Select(x => x.Method).Distinct().OrderBy(x => x))
					{
						Console.WriteLine($"  {name}");
					}
				}
				else
				{
					var serviceName = methods.First(x => x.Service.Equals(args[0], StringComparison.OrdinalIgnoreCase)).Service;
					var methodName = methods.First(x => x.Service == serviceName && x.Method.Equals(args[1], StringComparison.OrdinalIgnoreCase)).Method;
					Console.WriteLine($"\nUsage: app {serviceName} {methodName} params...\n");

					Console.WriteLine($"Parameters:");
					var methodOptions = methods
						.Where(x => x.Service == serviceName && x.Method == methodName)
						.ToArray();
					foreach (var method in methodOptions)
					{
						Console.Write(" ");
						foreach (var reqParam in method.RequiredParameters) Console.Write($" --{reqParam} <value>");
						foreach (var optParam in method.OptionalParameters) Console.Write($" [--{optParam} <value>]");
						Console.WriteLine();
					}
				}

				return;
			}

			Console.WriteLine("Executing...");

			var service = Resolver.Resolve(matches[0].ServiceType);
			matches[0].Execute(service, args);
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
