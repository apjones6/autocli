using AutoCli.Attributes;
using System;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	[CliService("Users")]
	public interface IUserService
	{
		Task CreateAsync([CliParameter("name")] string displayName, int? age = null);
		Task CreateAsync(User user);
		Task DeleteAsync([CliParameter("user-id")] Guid userId);
	}
}
