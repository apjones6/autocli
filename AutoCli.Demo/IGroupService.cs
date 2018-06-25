using AutoCli.Attributes;
using System;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	[CliService("Groups")]
	public interface IGroupService
	{
		Task AddMemberAsync([CliParameter("group-id")] Guid groupId, [CliParameter("user-id")] Guid userId);
		Task CreateAsync(string name, GroupVisibility visibilty = GroupVisibility.Authenticated);
		Task CreateAsync(Group group);
		Task DeleteAsync([CliParameter("group-id")] Guid groupId);
		Task RemoveMemberAsync([CliParameter("group-id")] Guid groupId, [CliParameter("user-id")] Guid userId);
	}
}
