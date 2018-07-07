using System;

namespace AutoCli.Demo
{
	class Program
	{
		static void Main(string[] args)
		{
			Cli.Builder
				.SetDescription("A demo CLI application for the AutoCli package features")
				.SetResolver(GetService)
				.AddService<IGroupService>()
				.AddService<IUserService>()
				.AddExtensions()
				.Execute(args);
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
