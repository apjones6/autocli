using AutoCli.Attributes;

namespace AutoCli.Demo.Outputs
{
	[CliOutputType(DeclaredType = typeof(ResultSet<>))]
	public class ResultSetOutput : Output
	{
		public override object GetConsoleContent()
		{
			var contents = new ConsoleContent();

			var prop = DeclaredType.GetProperty("Results");
			var resultsType = prop.PropertyType;
			var results = prop.GetValue(Result);

			contents.Add(CreateOutput(results, resultsType).GetConsoleContent());
			contents.Add(new { Total = (long)DeclaredType.GetProperty("Total").GetValue(Result) });

			return contents;
		}
	}
}
