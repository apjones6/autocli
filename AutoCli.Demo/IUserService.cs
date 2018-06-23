using AutoCli.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	[CliService("Users")]
	public interface IUserService
	{
		[CliMethod]
		Task CreateAsync([CliParameter("name")] string displayName, int? age = null);

		[CliMethod]
		Task CreateAsync(User user);

		[CliMethod]
		Task DeleteAsync([CliParameter("user-id")] Guid userId);
	}
}
