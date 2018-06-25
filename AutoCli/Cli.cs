using AutoCli.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

		public void AddService<T>()
		{
			AddService(typeof(T));
		}

		public void AddService(Type serviceType)
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
				Console.WriteLine("No candidates found for the provided arguments.");

				// TODO: Also handle scenario where value matches nothing
				if (args.Length == 0)
				{
					Console.WriteLine("\nAvailable services:");
					foreach (var name in methods.Select(x => x.Service).Distinct().OrderBy(x => x))
					{
						Console.WriteLine($"  {name}");
					}
				}
				else if (args.Length == 1)
				{
					Console.WriteLine($"\nAvailable \"{args[0]}\" methods:");
					foreach (var name in methods.Where(x => x.Service.Equals(args[0], StringComparison.OrdinalIgnoreCase)).Select(x => x.Method).Distinct().OrderBy(x => x))
					{
						Console.WriteLine($"  {name}");
					}
				}

				return;
			}

			Console.WriteLine("Executing...");

			var service = Resolver.Resolve(matches[0].ServiceType);
			matches[0].Execute(service, args);
		}

		private static MethodStrategy? UnlessDefault(MethodStrategy strategy)
		{
			return strategy != MethodStrategy.Default
				? (MethodStrategy?)strategy
				: null;
		}
	}
}
