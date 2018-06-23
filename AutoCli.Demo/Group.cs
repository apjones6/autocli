using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	public class Group
	{
		public Guid Id { get; set; }

		public User[] Members { get; set; }

		public string Name { get; set; }

		public GroupVisibility Visibility { get; set; }
	}
}
