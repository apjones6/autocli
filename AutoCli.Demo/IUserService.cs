using AutoCli.Attributes;
using System;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	[CliService("Users", Description = "Manage users")]
	public interface IUserService
	{
		[CliIgnore]
		Task<Response<User>> CreateAsync(User user);

		[CliMethod(Description = "Delete a user")]
		Task<Response> DeleteAsync(Guid userId);

		[CliMethod(Description = "Get a user")]
		Task<Response<User>> GetAsync(Guid userId);

		[CliMethod(Description = "List users")]
		Task<Response<ResultSet<User>>> ListAsync();
	}
}
