using System;

namespace AutoCli.Demo
{
	class Program
	{
		static void Main(string[] args)
		{
			var cli = new Cli
			{
				MethodStrategy = MethodStrategy.All,
				Resolver = (Resolver)GetService
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
