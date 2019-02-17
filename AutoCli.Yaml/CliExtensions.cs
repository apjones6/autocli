namespace AutoCli.Yaml
{
	/// <summary>
	/// Describes extensions to the <see cref="Cli"/> classes to simplify adding
	/// and using YAML.
	/// </summary>
	public static class CliExtensions
	{
		/// <summary>
		/// Adds a YAML serializer to this <see cref="Cli"/> instance.
		/// </summary>
		/// <param name="cli">The CLI instance to extend.</param>
		/// <returns>
		/// The <see cref="Cli"/> instance.
		/// </returns>
		public static Cli AddYaml(this Cli cli)
		{
			return cli.AddSerializer(new CliYamlSerializer());
		}
	}
}
