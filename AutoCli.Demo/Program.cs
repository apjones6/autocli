using System;

namespace AutoCli.Demo
{
	class Program
	{
		static void Main(string[] args)
		{
			var cli = new Cli
			{
				Description = "A demo CLI application for the AutoCli package features",
				MethodStrategy = MethodStrategy.All,
				Resolver = new Resolver(GetService)
			};

			cli.AddService<IGroupService>();
			cli.AddServiceExtensions<IGroupService>(typeof(ServiceExtensions));
			cli.AddService<IUserService>();
			cli.AddServiceExtensions<IUserService>(typeof(ServiceExtensions));

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
