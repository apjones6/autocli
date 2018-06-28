using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	public class Group
	{
		public Group()
		{
			MemberIds = new Guid[0];
		}

		public Guid Id { get; set; }

		public Guid[] MemberIds { get; set; }

		public string Name { get; set; }

		public GroupVisibility Visibility { get; set; }
	}
}
