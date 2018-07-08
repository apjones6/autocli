using AutoCli.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoCli.Representation
{
	internal class CliService
    {
		private readonly Cli cli;
		private readonly CliServiceAttribute attribute;
		private readonly List<CliMethod> methods;

		public CliService(Cli cli, Type serviceType)
		{
			this.cli = cli ?? throw new ArgumentNullException(nameof(cli));
			Type = serviceType ?? throw new ArgumentNullException(nameof(serviceType));

			attribute = GetServiceAttribute(serviceType);
			methods = new List<CliMethod>();
		}

		public string Description => attribute.Description;
		public string Name => attribute.Name;
		public Type Type { get; }
		
		public void AddMethods(IEnumerable<MethodInfo> infos)
		{
			foreach (var info in infos)
			{
				GetMethod(info).AddMethod(info);
			}
		}

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

		public object GetInstance()
		{
			return cli.Resolver.Resolve(Type);
		}

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

		public static CliServiceAttribute GetServiceAttribute(Type serviceType)
		{
			if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

			// Find explicit attribute
			var attr = serviceType.GetCustomAttribute<CliServiceAttribute>(false);
			if (attr == null)
			{
				throw new ArgumentException($"The type must be decorated with {typeof(CliServiceAttribute).FullName}.", nameof(serviceType));
			}

			return attr;
		}

		public void ShowHelp()
		{
			Console.WriteLine($"\nUsage: {Cli.AppName} {Name} METHOD\n");

			if (!string.IsNullOrWhiteSpace(attribute.Description))
			{
				Console.WriteLine($"{attribute.Description}\n");
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
