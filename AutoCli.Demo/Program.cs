using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	class Program
	{
		static void Main(string[] args)
		{
			var cli = new Cli(GetService);

			cli.AddService<IGroupService>();
			cli.AddService<IUserService>();

			cli.Execute(args);
		}

		static object GetService(Type serviceType)
		{
			if (serviceType == typeof(IGroupService))
			{
				return new GroupService();
			}
			else if (serviceType == typeof(IUserService))
			{
				return new UserService();
			}
			else
			{
				throw new ApplicationException("Unexpected service type.");
			}
		}
	}
}
