using System.Net;

namespace AutoCli.Demo
{
	public class Response
	{
		public Response()
			: this(HttpStatusCode.OK)
		{
		}

		public Response(HttpStatusCode status)
		{
			StatusCode = status;
		}

		public Response(HttpStatusCode status, string message)
			: this(status)
		{
			Message = message;
		}

		public bool IsSuccess => (int)StatusCode < 400;
		
		public HttpStatusCode StatusCode { get; set; }

		public string Message { get; set; }
	}
}
