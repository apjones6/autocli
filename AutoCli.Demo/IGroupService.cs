using AutoCli.Attributes;
using System;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	[CliService("groups", Description = "Manage groups")]
	public interface IGroupService
	{
		[CliMethod(Description = "Adds a user to the group")]
		Task AddMemberAsync([CliParameter("group-id")] Guid groupId, [CliParameter("user-id")] Guid userId);
		[CliMethod(Description = "Creates a group")]
		Task CreateAsync(Group group);
		[CliMethod(Description = "Deletes a group")]
		Task DeleteAsync([CliParameter("group-id")] Guid groupId);
		[CliMethod(Description = "Removes a user from the group")]
		Task RemoveMemberAsync([CliParameter("group-id")] Guid groupId, [CliParameter("user-id")] Guid userId);
	}
}
