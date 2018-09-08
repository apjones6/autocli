using System.Net;

namespace AutoCli.Demo
{
	public class Response<T> : Response
	{
		public Response(HttpStatusCode status)
			: base(status)
		{
		}

		public Response(HttpStatusCode status, string message)
			: base(status, message)
		{
		}

		public Response(T content, HttpStatusCode status)
			: base(status)
		{
			Content = content;
		}

		public Response(T content)
		{
			Content = content;
		}

		public T Content { get; set; }
	}
}
