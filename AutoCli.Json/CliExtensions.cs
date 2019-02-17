namespace AutoCli.Json
{
	/// <summary>
	/// Describes extensions to the <see cref="Cli"/> classes to simplify adding
	/// and using JSON.
	/// </summary>
	public static class CliExtensions
	{
		/// <summary>
		/// Adds a JSON serializer to this <see cref="Cli"/> instance.
		/// </summary>
		/// <param name="cli">The CLI instance to extend.</param>
		/// <returns>
		/// The <see cref="Cli"/> instance.
		/// </returns>
		public static Cli AddJson(this Cli cli)
		{
			return cli.AddSerializer(new CliJsonSerializer());
		}
	}
}
