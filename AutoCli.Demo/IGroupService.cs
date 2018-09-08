using AutoCli.Attributes;
using System;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	[CliService("Groups", Description = "Manage groups and memberships")]
	public interface IGroupService
	{
		[CliMethod(Description = "Add a user to the group")]
		Task<Response> AddMemberAsync([CliParameter("group-id")] Guid groupId, [CliParameter("user-id")] Guid userId);

		[CliMethod(Description = "Create a group")]
		Task<Response<Group>> CreateAsync(Group group);

		[CliMethod(Description = "Delete a group")]
		Task<Response> DeleteAsync([CliParameter("group-id")] Guid groupId);

		[CliMethod(Description = "Get a group")]
		Task<Response<Group>> GetAsync([CliParameter("group-id")] Guid groupId);

		[CliMethod(Description = "List groups")]
		Task<Response<ResultSet<Group>>> ListAsync(int skip = 0, int take = 25);

		[CliMethod(Description = "Remove a user from the group")]
		Task<Response> RemoveMemberAsync([CliParameter("group-id")] Guid groupId, [CliParameter("user-id")] Guid userId);
	}
}
