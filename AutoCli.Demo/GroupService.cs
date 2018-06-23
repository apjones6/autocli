using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCli.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AutoCli.Demo
{
	public class GroupService : IGroupService
	{
		private const string FILENAME = "groups.json";
		private List<Group> groups = null;

		public Task AddMemberAsync(Guid groupId, Guid userId)
		{
			throw new NotImplementedException();
		}
		
		public Task CreateAsync(string name, GroupVisibility visibilty = GroupVisibility.Authenticated)
		{
			throw new NotImplementedException();
		}
		
		public Task CreateAsync(Group group)
		{
			throw new NotImplementedException();
		}
		
		public Task DeleteAsync(Guid groupId)
		{
			throw new NotImplementedException();
		}
		
		public Task RemoveMemberAsync(Guid groupId, Guid userId)
		{
			throw new NotImplementedException();
		}

		private async Task LoadAsync()
		{
			if (groups != null) return;

			if (File.Exists(FILENAME))
			{
				using (var reader = File.OpenText(FILENAME))
				{
					var text = await reader.ReadToEndAsync();
					if (!string.IsNullOrEmpty(text))
					{
						groups = JToken.Parse(text).ToObject<List<Group>>();
					}
				}
			}

			if (groups == null)
			{
				groups = new List<Group>();
			}
		}

		private async Task SaveAsync()
		{
			if (groups == null) throw new ApplicationException("The groups have not yet been loaded.");

			using (var writer = File.Open(FILENAME, FileMode.Create, FileAccess.Write))
			{
				var text = JToken.FromObject(groups).ToString(Formatting.None);
				var bytes = Encoding.UTF8.GetBytes(text);
				await writer.WriteAsync(bytes, 0, bytes.Length);
			}
		}
	}
}
