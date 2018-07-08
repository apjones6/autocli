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
		private readonly List<CliService> services = new List<CliService>();

		private Resolver resolver;

		public string Description { get; set; }
		
		public Resolver Resolver
		{
			get { return resolver ?? (resolver = new Resolver(Activator.CreateInstance)); }
			set { resolver = value; }
		}

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
		
		public Cli AddService<T>()
		{
			return AddService(typeof(T));
		}
		
		public Cli AddService(Type serviceType)
		{
			var methods = serviceType
				.GetMethods()
				.Where(x => x.GetCustomAttribute<CliMethodAttribute>(true) != null)
				.ToArray();

			GetService(serviceType).AddMethods(methods);

			return this;
		}

		public static string AppName => Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

		public static Cli Builder => new Cli();
		
		public void Execute(string[] args)
		{
			var assembly = Assembly.GetEntryAssembly();

			if (args.Length == 1)
			{
				if (args[0] == "--help")
				{
					ShowHelp();
					return;
				}
				else if (args[0] == "-v" || args[0] == "--version")
				{
					var version = FileVersionInfo.GetVersionInfo(assembly.Location);
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

		private CliService GetService(Type serviceType)
		{
			var service = services.FirstOrDefault(x => x.Type == serviceType);
			if (service == null)
			{
				services.Add(service = new CliService(this, serviceType));
			}

			return service;
		}

		private void ShowHelp()
		{
			Console.WriteLine($"\nUsage: {AppName} SERVICE METHOD\n");

			if (!string.IsNullOrWhiteSpace(Description))
			{
				Console.WriteLine($"{Description}\n");
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
