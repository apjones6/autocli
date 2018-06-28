using AutoCli.Attributes;
using System;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	[CliService("users")]
	public interface IUserService
	{
		Task CreateAsync(User user);
		Task DeleteAsync([CliParameter("user-id")] Guid userId);
	}
}
