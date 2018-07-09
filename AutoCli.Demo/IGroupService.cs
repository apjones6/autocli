using AutoCli.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	[CliService("Groups", Description = "Manage groups and memberships")]
	public interface IGroupService
	{
		[CliMethod(Description = "Add a user to the group")]
		Task AddMemberAsync([CliParameter("group-id")] Guid groupId, [CliParameter("user-id")] Guid userId);

		[CliMethod(Description = "Create a group")]
		Task<Group> CreateAsync(Group group);

		[CliMethod(Description = "Delete a group")]
		Task DeleteAsync([CliParameter("group-id")] Guid groupId);

		[CliMethod(Description = "Get a group")]
		Task<Group> GetAsync([CliParameter("group-id")] Guid groupId);

		[CliMethod(Description = "List groups")]
		Task<IEnumerable<Group>> ListAsync();

		[CliMethod(Description = "Remove a user from the group")]
		Task RemoveMemberAsync([CliParameter("group-id")] Guid groupId, [CliParameter("user-id")] Guid userId);
	}
}
