using System;
using System.IO;
using YamlDotNet.Serialization;

namespace AutoCli.Yaml
{
	/// <summary>
	/// A <see cref="ICliSerializer"/> implementation which supports YAML input/output files.
	/// </summary>
	public class CliYamlSerializer : ICliSerializer
	{
		private readonly ISerializer serializer;

		/// <summary>
		/// Initializes a new instance of the <see cref="CliYamlSerializer"/> class.
		/// </summary>
		public CliYamlSerializer()
		{
			serializer = new SerializerBuilder().Build();
		}

		/// <summary>
		/// Returns a value indicating whether this serializer can write contents
		/// to a file with the specified extension.
		/// </summary>
		/// <param name="extension">The file extension.</param>
		/// <returns>
		/// True if can write, false otherwise.
		/// </returns>
		public bool CanWrite(string extension)
		{
			return extension == ".yml" || extension == ".yaml";
		}

		/// <summary>
		/// Attempts to read the provided input and output the parameter value of
		/// the appropriate type to use.
		/// </summary>
		/// <param name="input">The input to read.</param>
		/// <param name="type">The parameter type to convert to.</param>
		/// <param name="parameter">The parameter value.</param>
		/// <returns>
		/// True if the input was read, false otherwise.
		/// </returns>
		public bool TryReadParameter(string input, Type type, out object parameter)
		{
			parameter = null;
			return false;
		}

		/// <summary>
		/// Writes the provided content object to the output <see cref="Stream"/>.
		/// </summary>
		/// <remarks>
		/// The serializer must not attempt to close the stream.
		/// </remarks>
		/// <param name="stream">The output stream to write to.</param>
		/// <param name="content">The content to write.</param>
		public void Write(Stream stream, object content)
		{
			using (var writer = new StreamWriter(stream))
			{
				serializer.Serialize(writer, content);
			}
		}
	}
}
