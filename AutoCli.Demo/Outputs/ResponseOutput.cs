using AutoCli.Attributes;

namespace AutoCli.Demo.Outputs
{
	[CliOutputType(DeclaredTypes = new[] { typeof(Response), typeof(Response<>) })]
	public class ResponseOutput : Output
	{
		public override object GetConsoleContent()
		{
			var response = (Response)Result;
			var contents = new ConsoleContent();

			if (response.IsSuccess && DeclaredType.IsGenericType)
			{
				var contentType = DeclaredType.GetGenericArguments()[0];
				var content = DeclaredType.GetProperty("Content").GetValue(response);
				contents.Add(CreateOutput(content, contentType).GetConsoleContent());
				contents.Add(ConsoleContent.SEPARATOR);
			}

			contents.Add(new { Status = $"{(int)response.StatusCode} ({response.StatusCode})" });
			if (!string.IsNullOrWhiteSpace(response.Message))
			{
				contents.Add(new { response.Message });
			}

			return contents;
		}

		public override object GetFileContent()
		{
			var response = (Response)Result;

			// Only return file content if the response is successful, has content,
			// and that content isn't null
			if (response.IsSuccess && DeclaredType.IsGenericType)
			{
				var contentType = DeclaredType.GetGenericArguments()[0];
				var content = DeclaredType.GetProperty("Content").GetValue(response);
				if (content != null)
				{
					return content;
				}
			}

			return null;
		}
	}
}
