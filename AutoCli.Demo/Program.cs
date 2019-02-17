using AutoCli.Json;
using System;

namespace AutoCli.Demo
{
	class Program
	{
		static void Main(string[] args)
		{
			Cli.Builder
				.SetDescription("A demo CLI application for the AutoCli package features")
				.SetNameConvention(NameConvention.KebabCase)
				.AddJson()
				.SetResolver(GetService)
				.AddService<IGroupService>()
				.AddService<IUserService>()
				.AddExtensions()
				.AddOutputs()
				.Execute(args);
		}

		static object GetService(Type serviceType)
		{
			if (serviceType == typeof(IGroupService) || serviceType == typeof(GroupService))
			{
				return new GroupService();
			}
			else if (serviceType == typeof(IUserService) || serviceType == typeof(UserService))
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
