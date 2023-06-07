using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PluginAPI.Core;
using CommandSystem;
using RemoteAdmin;
using System.Net.Http;
using PluginAPI.Events;

namespace DynamicTags.Systems
{
	public class DynamicTags
	{
		[CommandHandler(typeof(RemoteAdminCommandHandler))]
		public class DynamicTagCommand : ICommand
		{
			public string Command => "dynamictag";

			public string[] Aliases { get; } = { "dtag", "dt" };

			public string Description => "Shows your dynamic tag";

			public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
			{
				if (sender is PlayerCommandSender pSender)
				{
					if (Tags.ContainsKey(pSender.ReferenceHub.characterClassManager.UserId))
					{
						TagData data = Tags[pSender.ReferenceHub.characterClassManager.UserId];

						//This is to stop situations where users have locally assigned perms but gets overridden by NULL perms from the external server.
						if (!string.IsNullOrEmpty(data.Group))
							pSender.ReferenceHub.serverRoles.SetGroup(ServerStatic.GetPermissionsHandler().GetGroup(data.Group), true);

						pSender.ReferenceHub.serverRoles.SetText(data.Tag);
						pSender.ReferenceHub.serverRoles.SetColor(data.Colour);

						if (data.Perms != 0)
							pSender.ReferenceHub.serverRoles.Permissions = data.Perms;

						response = "Dynamic tag loaded: " + data.Tag;
						return true;
					}
					response = "You have no tag";
					return true;
				}

				response = "This command must be run as a player command";
				return false;
			}
		}

		public static Dictionary<string, TagData> Tags = new Dictionary<string, TagData>();

		[PluginEvent(ServerEventType.WaitingForPlayers)]
		public async void OnWaitingForPlayers()
		{
			try
			{
				//Clears all previous tags held by the server (Prevents players from keeping tags when they have been removed from the external server).
				Tags.Clear();

				var response = await Extensions.Get(Plugin.Config.ApiEndpoint + "games/gettags");

				//Log.Info(await response.Content.ReadAsStringAsync());

				var tags = JsonConvert.DeserializeObject<TagData[]>(await response.Content.ReadAsStringAsync());

				foreach (var a in tags)
				{
					if (a.UserID.StartsWith("7656"))
						a.UserID = $"{a.UserID}@steam";
					else if(ulong.TryParse(a.UserID, out ulong result))
						a.UserID = $"{a.UserID}@discord";
					else 
						a.UserID = $"{a.UserID}@northwood";

					//Adds the tags to the tag list.
					Tags.Add(a.UserID, a);
				}

				Log.Info($"{Tags.Count} tags loaded");
			}
			catch(Exception e)
			{
				Log.Error(e.ToString());
			}
		}

		[PluginEvent(ServerEventType.PlayerCheckReservedSlot)]
		public PlayerCheckReservedSlotCancellationData OnReservedSlotCheck(string userid, bool hasReservedSlot)
		{
			if (hasReservedSlot)
				return PlayerCheckReservedSlotCancellationData.LeaveUnchanged();

			//Checks if the user has a reserved slot set by the external server. Northwood staff are automatically given a slot.
			//TODO: Make northwood bypass a config option
			if ((userid.ToLowerInvariant().Contains("northwood") && Plugin.Config.AutomaticNorthwoodReservedSlot) || (Tags.ContainsKey(userid) && Tags[userid].ReservedSlot))
			{
				hasReservedSlot = true;
				return PlayerCheckReservedSlotCancellationData.Override(true);
			}

			return PlayerCheckReservedSlotCancellationData.LeaveUnchanged();
		}

		[PluginEvent(ServerEventType.PlayerJoined)]
		public void OnPlayerJoin(Player player)
		{
			//Checks if the user has a tag
			if (Tags.ContainsKey(player.UserId))
			{
				TagData data = Tags[player.UserId];

				//This is to stop situations where users have locally assigned perms but gets overridden by NULL perms from the external server.
				if (!string.IsNullOrEmpty(data.Group))
					player.ReferenceHub.serverRoles.SetGroup(ServerStatic.GetPermissionsHandler().GetGroup(data.Group), true);

				player.ReferenceHub.serverRoles.SetText(data.Tag);
				player.ReferenceHub.serverRoles.SetColor(data.Colour);

				if (data.Perms != 0)
					player.ReferenceHub.serverRoles.Permissions = data.Perms;

				player.SendConsoleMessage("Dynamic tag loaded: " + data.Tag);
				Log.Info($"Tag found for {player.UserId}: {data.Tag}");
			}
		}
	}
}
