using System.Collections.Generic;
using System.Linq;

namespace AutoCli.Demo
{
	public class ResultSet<T>
	{
		public ResultSet()
		{
			Results = new T[0];
			Total = 0;
		}

		public ResultSet(IEnumerable<T> results)
		{
			Results = results;
			Total = results.Count();
		}

		public ResultSet(IEnumerable<T> results, long total)
		{
			Results = results;
			Total = total;
		}

		public IEnumerable<T> Results { get; set; }

		public long Total { get; set; }
	}
}
