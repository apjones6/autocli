using AutoCli.Attributes;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	[CliExtensions]
	public static class ServiceExtensions
	{
		[CliMethod]
		public static async Task<Response<User>> CreateAsync(this IUserService userService, [CliParameter("name")] string displayName, int? age = null)
		{
			var user = new User { Age = age, DisplayName = displayName };
			return await userService.CreateAsync(user);
		}

		[CliMethod]
		public static async Task<Response<Group>> CreateAsync(this IGroupService groupService, string name, GroupVisibility visibility = GroupVisibility.Authenticated)
		{
			var group = new Group { Name = name, Visibility = visibility };
			return await groupService.CreateAsync(group);
		}

		[CliMethod(Description = "List users in the group")]
		public static async Task<Response<ResultSet<User>>> ListMembersAsync(this IGroupService groupService, [CliParameter("group-id")] Guid groupId)
		{
			var response = await groupService.GetAsync(groupId);
			if (response.StatusCode != HttpStatusCode.OK)
			{
				return new Response<ResultSet<User>>(HttpStatusCode.NotFound);
			}

			var group = response.Content;
			if (group.MemberIds?.Length > 0)
			{
				var userService = new UserService();
				var responses = await Task.WhenAll(group.MemberIds.Select(x => userService.GetAsync(x)).ToArray());

				var error = responses.FirstOrDefault(x => x.StatusCode != HttpStatusCode.OK);
				if (error != null)
				{
					return new Response<ResultSet<User>>(HttpStatusCode.InternalServerError, error.Message);
				}

				var users = new ResultSet<User>(responses.Select(x => x.Content), group.MemberIds.Length);
				return new Response<ResultSet<User>>(users);
			}

			return new Response<ResultSet<User>>(new ResultSet<User>());
		}
	}
}
