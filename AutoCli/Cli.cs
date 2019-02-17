using AutoCli.Attributes;
using AutoCli.Representation;
using AutoCli.Resolvers;
using AutoCli.Serializers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace AutoCli
{
	/// <summary>
	/// The command line interface root, which allows global configuration and service methods to
	/// be added, and executes the provided input arguments.
	/// </summary>
	public class Cli
	{
		private readonly Dictionary<Type, Type> outputs;
		private readonly List<ICliSerializer> serializers;
		private readonly List<CliService> services;

		private string description;
		private NameConvention nameConvention = NameConvention.KebabCase;
		private string outputPath;
		private IResolver resolver;

		/// <summary>
		/// Initializes a new instance of the <see cref="Cli"/> class.
		/// </summary>
		private Cli()
		{
			outputs = new Dictionary<Type, Type>();
			resolver = new Resolver(Activator.CreateInstance);
			serializers = new List<ICliSerializer> { new CommandLineSerializer() };
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
		/// added using this method.
		/// </remarks>
		/// <returns>
		/// This <see cref="Cli"/> instance.
		/// </returns>
		public Cli AddExtensions()
		{
			// TODO: reuse assembly search between AddExtensions() and AddOutputs()
			var methods = AppDomain.CurrentDomain
				.GetAssemblies()
				.AsParallel()
				.SelectMany(x => x.GetExportedTypes().Where(t => t.GetCustomAttribute<CliExtensionsAttribute>() != null))
				.SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public))
				.Where(x => x.IsDefined(typeof(ExtensionAttribute), false) && !x.IsDefined(typeof(CliIgnoreAttribute)))
				.Select(x => new { Method = x, Parameters = x.GetParameters() })
				.Where(x => x.Parameters.Length > 0 && services.Any(s => x.Parameters[0].ParameterType.IsAssignableFrom(s.Type)))
				.GroupBy(x => x.Parameters[0].ParameterType, x => x.Method)
				.ToArray();

			foreach (var group in methods.Where(x => x.Any()))
			{
				var serviceType = services.First(s => group.Key.IsAssignableFrom(s.Type)).Type;
				GetService(serviceType).AddMethods(group);
			}
			
			return this;
		}

		/// <summary>
		/// Adds the output type <typeparamref name="T"/> to this <see cref="Cli"/> instance, to be
		/// used when the specified declared type must be output.
		/// </summary>
		/// <typeparam name="T">The output type.</typeparam>
		/// <param name="declaredType">The declared type.</param>
		/// <returns>
		/// This <see cref="Cli"/> instance.
		/// </returns>
		public Cli AddOutput<T>(Type declaredType)
		{
			return AddOutput(typeof(T), declaredType);
		}

		/// <summary>
		/// Adds the specified output type to this <see cref="Cli"/> instance, to be used when the
		/// specified declared type must be output.
		/// </summary>
		/// <param name="outputType">The output type.</param>
		/// <param name="declaredType">The declared type.</param>
		/// <returns>
		/// This <see cref="Cli"/> instance.
		/// </returns>
		public Cli AddOutput(Type outputType, Type declaredType)
		{
			outputs[declaredType] = outputType;

			return this;
		}

		/// <summary>
		/// Adds output types to this <see cref="Cli"/> instance from all available assemblies.
		/// </summary>
		/// <remarks>
		/// The output classes must inherit from <see cref="Output"/>, and be decorated with
		/// <see cref="CliOutputTypeAttribute"/> to be added using this method.
		/// </remarks>
		/// <returns>
		/// This <see cref="Cli"/> instance.
		/// </returns>
		public Cli AddOutputs()
		{
			// TODO: reuse assembly search between AddExtensions() and AddOutputs()
			var types = AppDomain.CurrentDomain
				.GetAssemblies()
				.AsParallel()
				.SelectMany(x => x.GetExportedTypes().Where(t => t.IsSubclassOf(typeof(Output)) && t.IsDefined(typeof(CliOutputTypeAttribute))))
				.ToArray();

			foreach (var outputType in types)
			{
				var attribute = outputType.GetCustomAttribute<CliOutputTypeAttribute>();
				var declaredTypes = attribute.DeclaredTypes ?? new[] { attribute.DeclaredType };
				foreach (var declaredType in declaredTypes)
				{
					AddOutput(outputType, declaredType);
				}
			}

			return this;
		}

		/// <summary>
		/// Adds a new instance of the specified <see cref="ICliSerializer"/> type to this
		/// <see cref="Cli"/> instance.
		/// </summary>
		/// <typeparam name="T">The serializer type.</typeparam>
		/// <returns>
		/// This <see cref="Cli"/> instance.
		/// </returns>
		public Cli AddSerializer<T>() where T : ICliSerializer, new()
		{
			serializers.Add(new T());
			return this;
		}

		/// <summary>
		/// Adds the provided <see cref="ICliSerializer"/> to this <see cref="Cli"/> instance.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>
		/// This <see cref="Cli"/> instance.
		/// </returns>
		public Cli AddSerializer(ICliSerializer serializer)
		{
			serializers.Add(serializer);
			return this;
		}

		/// <summary>
		/// Adds the service type <typeparamref name="T"/> to this <see cref="Cli"/> instance.
		/// </summary>
		/// <typeparam name="T">The service type.</typeparam>
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
			// Don't add methods from object, or methods decorated with [CliIgnore]
			var methods = serviceType
				.GetMethods()
				.Where(x => x.GetBaseDefinition().DeclaringType != typeof(object) && !x.IsDefined(typeof(CliIgnoreAttribute)))
				.ToArray();

			if (methods.Length > 0)
			{
				GetService(serviceType).AddMethods(methods);
			}

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
					// Extra newline for powershell usage
					Console.WriteLine();
					return;
				}
				else if (args[0] == "-v" || args[0] == "--version")
				{
					var version = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
					Console.WriteLine(AppName);
					Console.WriteLine(version.FileVersion);
					// Extra newline for powershell usage
					Console.WriteLine();
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
				// TODO: Also show an input error
				ShowHelp();
			}

			// Extra newline for powershell usage
			Console.WriteLine();
		}

		/// <summary>
		/// Returns a string name to use, by applying the active <see cref="NameConvention"/>
		/// rules to the provided candidate name.
		/// </summary>
		/// <param name="name">The input name.</param>
		/// <returns>
		/// A name which follows conventions.
		/// </returns>
		internal string ApplyNameConvention(string name)
		{
			// Splitting on convention is easy for now, but going forward we may want
			// to support specifying an implementation of a base class (strategy pattern)
			var separator = nameConvention == NameConvention.KebabCase ? '-' : '_';

			// In general we can put a hyphen in front of every uppercase character
			// except one at index 0. However we also guard against values which look
			// like acronyms (e.g. SaveXMLFile becomes save-xml-file)
			var sb = new StringBuilder(name.Length);
			var prevLower = false;
			for (var i = 0; i < name.Length; ++i)
			{
				var c = name[i];
				if (char.IsUpper(c))
				{
					// Insert hyphen then:
					//   - Going from lowercase to uppercase
					//   - Staying uppercase, and the next character is lowercase
					if (prevLower || (i != 0 && i < name.Length - 1 && char.IsUpper(name[i + 1]))) sb.Append(separator);
					sb.Append(char.ToLower(c));
					prevLower = false;
				}
				else
				{
					sb.Append(char.ToLower(c));
					prevLower = true;
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Returns a new instance of the <see cref="Output"/> class with the provided
		/// result data and its declared type.
		/// </summary>
		/// <param name="result">The output result data.</param>
		/// <param name="declaredType">The output declared type.</param>
		/// <returns>
		/// A new <see cref="Output"/> instance.
		/// </returns>
		internal Output CreateOutput(object result, Type declaredType)
		{
			// Look for a custom output for this type
			// If declared type is generic, also try looking for the generic definition
			// Otherwise use fallback
			if (!outputs.TryGetValue(declaredType, out var type) && !(declaredType.IsGenericType && outputs.TryGetValue(declaredType.GetGenericTypeDefinition(), out type)))
			{
				type = typeof(Output);
			}

			var output = (Output)Activator.CreateInstance(type);

			output.Cli = this;
			output.DeclaredType = declaredType;
			output.Result = result;

			return output;
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
		/// Attempts to read the provided input and output the parameter value of the appropriate type to use.
		/// </summary>
		/// <param name="input">The input to read.</param>
		/// <param name="type">The parameter type to convert to.</param>
		/// <param name="parameter">The parameter value.</param>
		/// <returns>
		/// True if the input was read, false otherwise.
		/// </returns>
		internal bool TryReadParameter(string input, Type type, out object parameter)
		{
			foreach (var serializer in serializers)
			{
				if (serializer.TryReadParameter(input, type, out parameter))
				{
					return true;
				}
			}
			
			parameter = null;
			return false;
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
		/// Sets the <see cref="Cli"/> name convention, which determines how names are formatted for use.
		/// </summary>
		/// <param name="nameConvention">The name convention to apply.</param>
		/// <returns>
		/// This <see cref="Cli"/> instance.
		/// </returns>
		public Cli SetNameConvention(NameConvention nameConvention)
		{
			this.nameConvention = nameConvention;
			return this;
		}

		/// <summary>
		/// Sets the output path to use, which causes method output to be written to the file instead of console.
		/// </summary>
		/// <param name="path">The output path.</param>
		/// <returns>
		/// This <see cref="Cli"/> instance.
		/// </returns>
		internal Cli SetOutputPath(string path)
		{
			// TODO: check the available serializers for one which supports this extension
			outputPath = Path.GetFullPath(path);
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
			Console.WriteLine("  -o, --output   Sets the output path for file output");
			Console.WriteLine("  -v, --version  Show version");
			Console.WriteLine();

			Console.WriteLine("Services:");
			if (services.Count > 0)
			{
				var padding = services.Max(x => x.Name.Length);
				foreach (var s in services.OrderBy(x => x.Name))
				{
					// TODO: Wrap description based on console width
					Console.WriteLine($"  {s.Name.PadRight(padding)}  {s.Description}");
				}
			}
		}

		/// <summary>
		/// Writes the provided <see cref="Output"/> instance to the configured output target,
		/// console by default but may be a file.
		/// </summary>
		/// <param name="output">The output to write.</param>
		internal void Write(Output output)
		{
			// If an output path is specified, writer to the first serializer
			// which can write to this file type
			ICliSerializer serializer = null;
			if (outputPath != null)
			{
				var extension = Path.GetExtension(outputPath);
				serializer = serializers.FirstOrDefault(x => x.CanWrite(extension));
				if (serializer == null)
				{
					throw new ApplicationException($"No serializer found which can write to \"{extension}\" files.");
				}

				// Ignore and fallback to standard serializer if no file content
				var content = output.GetFileContent();
				if (content != null)
				{
					using (var stream = File.Create(outputPath))
					{
						// TODO: Console confirmation the file was written, and the absolute path
						// TODO: Add FileContent class which can include ConsoleOutput to write in addition to the file
						serializer.Write(stream, content);
						return;
					}
				}
			}

			// Use the standard (console) serializer
			serializers[0].Write(Console.OpenStandardOutput(), output.GetConsoleContent());
		}
	}
}
