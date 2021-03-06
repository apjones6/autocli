﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AutoCli.Demo
{
	public class GroupService : IGroupService
	{
		private const string FILENAME = "groups.json";
		private List<Group> groups = null;

		public async Task<Response> AddMemberAsync(Guid groupId, Guid userId)
		{
			await LoadAsync();

			var group = groups.Single(x => x.Id == groupId);
			group.MemberIds = group.MemberIds != null
				? group.MemberIds.Union(new[] { userId }).ToArray()
				: group.MemberIds = new[] { userId };

			await SaveAsync();

			return new Response();
		}
		
		public async Task<Response<Group>> CreateAsync(Group group)
		{
			await LoadAsync();

			group.Id = Guid.NewGuid();
			groups.Add(group);

			await SaveAsync();

			return new Response<Group>(group, HttpStatusCode.Created);
		}
		
		public async Task<Response> DeleteAsync(Guid groupId)
		{
			await LoadAsync();

			groups.RemoveAll(x => x.Id == groupId);

			await SaveAsync();

			return new Response(HttpStatusCode.NoContent);
		}

		public async Task<Response<Group>> GetAsync(Guid groupId)
		{
			await LoadAsync();

			var group = groups.FirstOrDefault(x => x.Id == groupId);
			if (group != null)
			{
				return new Response<Group>(group);
			}

			return new Response<Group>(HttpStatusCode.NotFound);
		}

		public async Task<Response<ResultSet<Group>>> ListAsync(int skip = 0, int take = 25)
		{
			await LoadAsync();
			
			var results = new ResultSet<Group>(groups.Skip(skip).Take(take).ToArray(), groups.Count);
			return new Response<ResultSet<Group>>(results);
		}

		public async Task<Response> RemoveMemberAsync(Guid groupId, Guid userId)
		{
			await LoadAsync();

			var group = groups.Single(x => x.Id == groupId);
			if (group.MemberIds != null)
			{
				group.MemberIds = group.MemberIds.Except(new[] { userId }).ToArray();
			}

			await SaveAsync();

			return new Response();
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
