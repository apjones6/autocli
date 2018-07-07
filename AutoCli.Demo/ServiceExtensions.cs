using AutoCli.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	public static class ServiceExtensions
	{
		[CliMethod(Description = "Create a user")]
		public static async Task<User> CreateAsync(this IUserService userService, [CliParameter("name")] string displayName, int? age = null)
		{
			var user = new User { Age = age, DisplayName = displayName };
			return await userService.CreateAsync(user);
		}

		[CliMethod(Description = "Create a group")]
		public static async Task<Group> CreateAsync(this IGroupService groupService, string name, GroupVisibility visibility = GroupVisibility.Authenticated)
		{
			var group = new Group { Name = name, Visibility = visibility };
			return await groupService.CreateAsync(group);
		}

		[CliMethod(Description = "List users in the group")]
		public static async Task<IEnumerable<User>> ListMembersAsync(this IGroupService groupService, [CliParameter("group-id")] Guid groupId)
		{
			var group = await groupService.GetAsync(groupId);
			if (group?.MemberIds?.Length > 0)
			{
				var userService = new UserService();
				return await Task.WhenAll(group.MemberIds.Select(x => userService.GetAsync(x)).ToArray());
			}

			return new User[0];
		}
	}
}
