using AutoCli.Attributes;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	public static class ServiceExtensions
	{
		public static async Task CreateAsync(this IUserService userService, [CliParameter("name")] string displayName, int? age = null)
		{
			var user = new User { Age = age, DisplayName = displayName };
			await userService.CreateAsync(user);
		}

		public static async Task CreateAsync(this IGroupService groupService, string name, GroupVisibility visibilty = GroupVisibility.Authenticated)
		{
			var group = new Group { Name = name, Visibility = visibilty };
			await groupService.CreateAsync(group);
		}
	}
}
