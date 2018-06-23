using AutoCli.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	[CliService("Groups")]
	public interface IGroupService
	{
		[CliMethod]
		Task AddMemberAsync([CliParameter("group-id")] Guid groupId, [CliParameter("user-id")] Guid userId);

		[CliMethod]
		Task CreateAsync(string name, GroupVisibility visibilty = GroupVisibility.Authenticated);

		[CliMethod]
		Task CreateAsync(Group group);

		[CliMethod]
		Task DeleteAsync([CliParameter("group-id")] Guid groupId);

		[CliMethod]
		Task RemoveMemberAsync([CliParameter("group-id")] Guid groupId, [CliParameter("user-id")] Guid userId);
	}
}
