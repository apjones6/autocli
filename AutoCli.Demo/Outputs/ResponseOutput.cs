using AutoCli.Attributes;
using System;

namespace AutoCli.Demo.Outputs
{
	[CliOutputType(DeclaredTypes = new[] { typeof(Response), typeof(Response<>) })]
	public class ResponseOutput : Output
	{
		public override void Write()
		{
			var response = (Response)Result;

			if (DeclaredType.IsGenericType)
			{
				var contentType = DeclaredType.GetGenericArguments()[0];
				var content = DeclaredType.GetProperty("Content").GetValue(response);
				Write(content, contentType);
				Console.WriteLine();
			}

			Console.WriteLine($" STATUS:  {(int)response.StatusCode} ({response.StatusCode})");
			if (!string.IsNullOrWhiteSpace(response.Message))
			{
				Console.WriteLine($" MESSAGE:  {response.Message}");
			}
		}
	}
}
