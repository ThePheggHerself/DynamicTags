using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;

namespace DynamicTags.Systems
{
	public class Reporting
	{
		[PluginEvent(ServerEventType.PlayerReport)]
		public void OnPlayerReport(Player player, Player target, string reason)
		{
			var reportDetails = new PlayerReportDetails
			{
				PlayerName = target.Nickname,
				PlayerID = target.UserId,
				PlayerRole = target.Role.ToString(),
				ReporterName = player.Nickname,
				ReporterID = player.UserId,
				ReporterRole = player.Role.ToString(),
				Reason = reason,
				ServerAddress = Server.ServerIpAddress,
				ServerPort = Server.Port.ToString(),
			};

			Extensions.Post(Plugin.Config.ApiEndpoint + "scpsl/report", new StringContent(JsonConvert.SerializeObject(reportDetails), Encoding.UTF8, "application/json"));
		}
	}
}
