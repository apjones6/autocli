using System;

namespace AutoCli.Demo
{
	class Program
	{
		static void Main(string[] args)
		{
			var cli = new Cli(GetService)
			{
				MethodStrategy = MethodStrategy.All
			};

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
