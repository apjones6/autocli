using AutoCli.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	[CliService("Users", Description = "Manage users")]
	public interface IUserService
	{
		[CliMethod(Description = "Create a user")]
		Task<User> CreateAsync(User user);

		[CliMethod(Description = "Delete a user")]
		Task DeleteAsync([CliParameter("user-id")] Guid userId);

		[CliMethod(Description = "Get a user")]
		Task<User> GetAsync([CliParameter("user-id")] Guid userId);

		[CliMethod(Description = "List users")]
		Task<IEnumerable<User>> ListAsync();
	}
}
