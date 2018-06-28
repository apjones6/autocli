using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	public class UserService : IUserService
	{
		private const string FILENAME = "users.json";
		private List<User> users = null;
		
		public async Task CreateAsync(User user)
		{
			await LoadAsync();

			user.Id = Guid.NewGuid();
			users.Add(user);

			await SaveAsync();
		}
		
		public async Task DeleteAsync(Guid userId)
		{
			await LoadAsync();
			
			users.RemoveAll(x => x.Id == userId);

			await SaveAsync();
		}

		private async Task LoadAsync()
		{
			if (users != null) return;

			if (File.Exists(FILENAME))
			{
				using (var reader = File.OpenText(FILENAME))
				{
					var text = await reader.ReadToEndAsync();
					if (!string.IsNullOrEmpty(text))
					{
						users = JToken.Parse(text).ToObject<List<User>>();
					}
				}
			}

			if (users == null)
			{
				users = new List<User>();
			}
		}

		private async Task SaveAsync()
		{
			if (users == null) throw new ApplicationException("The users have not yet been loaded.");

			using (var writer = File.Open(FILENAME, FileMode.Create, FileAccess.Write))
			{
				var text = JToken.FromObject(users).ToString(Formatting.None);
				var bytes = Encoding.UTF8.GetBytes(text);
				await writer.WriteAsync(bytes, 0, bytes.Length);
			}
		}
	}
}
