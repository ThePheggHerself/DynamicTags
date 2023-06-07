using LiteNetLib.Utils;
using LiteNetLib;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using PluginAPI.Core.Attributes;
using PluginAPI.Core;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using RemoteAdmin;
using CommandSystem;

namespace DynamicTags.Systems
{
	public class StaffTracker
	{
		[PluginEvent(ServerEventType.PlayerJoined)]
		public void OnPlayerJoin(Player player)
		{
			var details = new PlayerDetails
			{
				UserId = player.UserId,
				UserName = player.Nickname,
				Address = player.IpAddress,
				ServerAddress = Server.ServerIpAddress,
				ServerPort = Server.Port.ToString()
			};

			Extensions.Post(Plugin.Config.ApiEndpoint + "scpsl/playerjoin", new StringContent(JsonConvert.SerializeObject(details), Encoding.UTF8, "application/json"));
		}

		[PluginEvent(ServerEventType.PlayerLeft)]
		public void OnPlayerLeave(Player player)
		{
			var details = new PlayerDetails
			{
				UserId = player.UserId,
				UserName = player.Nickname,
				Address = player.IpAddress,
				ServerAddress = Server.ServerIpAddress,
				ServerPort = Server.Port.ToString()
			};

			Extensions.Post(Plugin.Config.ApiEndpoint + "scpsl/playerleave", new StringContent(JsonConvert.SerializeObject(details), Encoding.UTF8, "application/json"));
		}

		[PluginEvent(ServerEventType.PlayerBanned)]
		public void OnPlayerBanned(Player player, CommandSender commandSender, string reason, long duration)
		{
			Console.WriteLine(commandSender is PlayerCommandSender);

			if (!(commandSender is PlayerCommandSender pCS))
				return;

			var admin = Player.Get(pCS.PlayerId);

			var details = new PlayerBanDetails
			{
				PlayerName = player.Nickname.Replace(':', ' '),
				PlayerID = player.UserId,
				AdminName = admin.Nickname.Replace(':', ' '),
				AdminID = admin.UserId,
				Duration = (duration / 60).ToString(),
				Reason = string.IsNullOrEmpty(reason) ? "No reason provided" : reason
			};

			Extensions.Post(Plugin.Config.ApiEndpoint + "scpsl/playerban", new StringContent(JsonConvert.SerializeObject(details), Encoding.UTF8, "application/json"));
		}
	}
}
