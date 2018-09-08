using AutoCli.Attributes;
using System;

namespace AutoCli.Demo.Outputs
{
	[CliOutputType(DeclaredType = typeof(ResultSet<>))]
	public class ResultSetOutput : Output
	{
		public override void Write()
		{
			var prop = DeclaredType.GetProperty("Results");
			var resultsType = prop.PropertyType;
			var results = prop.GetValue(Result);
			Write(results, resultsType);

			var total = (long)DeclaredType.GetProperty("Total").GetValue(Result);
			Console.WriteLine($" TOTAL:  {total}");
		}
	}
}
