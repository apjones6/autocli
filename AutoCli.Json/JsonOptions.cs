using Newtonsoft.Json;

namespace AutoCli.Json
{
	/// <summary>
	/// Describes options which can be provided to the JSON serializer.
	/// </summary>
	public class JsonOptions
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JsonOptions"/> class.
		/// </summary>
		public JsonOptions()
		{
			DefaultValueHandling = DefaultValueHandling.Ignore;
			Formatting = Formatting.Indented;
			NullValueHandling = NullValueHandling.Ignore;
		}

		/// <summary>
		/// Gets or sets the default value handling strategy.
		/// </summary>
		public DefaultValueHandling DefaultValueHandling { get; set; }

		/// <summary>
		/// Gets or sets the formatting strategy.
		/// </summary>
		public Formatting Formatting { get; set; }

		/// <summary>
		/// Gets or sets the null value handling strategy.
		/// </summary>
		public NullValueHandling NullValueHandling { get; set; }
	}
}
