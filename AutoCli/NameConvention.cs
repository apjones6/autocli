namespace AutoCli
{
	/// <summary>
	/// An enumeration of supported name conventions.
	/// </summary>
	public enum NameConvention
	{
		/// <summary>
		/// The name should be converted to kebab case (e.g. "save-xml-file").
		/// </summary>
		KebabCase = 0,

		/// <summary>
		/// The name should be converted to snake case (e.g. "save_xml_file").
		/// </summary>
		SnakeCase = 1
	}
}
