using AutoCli.Attributes;
using System;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	[CliService("users", Description = "Manage users")]
	public interface IUserService
	{
		[CliMethod(Description = "Creates a user")]
		Task CreateAsync(User user);
		[CliMethod(Description = "Deletes a user")]
		Task DeleteAsync([CliParameter("user-id")] Guid userId);
	}
}
