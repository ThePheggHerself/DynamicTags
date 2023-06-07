using PluginAPI.Enums;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerStatsSystem;
using PlayerRoles;
using InventorySystem.Items.Usables;
using MapGeneration.Distributors;
using PluginAPI.Core.Zones;
using static RoundSummary;
using Newtonsoft.Json;
using System.Net.Http;
using Mirror;

namespace DynamicTags.Systems
{
	public class TrackedStats
	{
		public TrackedStats(Player player)
		{
			UserID = player.UserId;
			Username = player.Nickname;
			JoinTime = DateTime.Now;
			DNT = player.DoNotTrack;
		}

		public TrackedStats()
		{

		}

		public string UserID { get; set; }
		public string Username { get; set; }

		public DateTime JoinTime { get; set; }

		public int SecondsPlayed { get; set; }

		public int SCPKills { get; set; }
		public int MTFKills { get; set; }
		public int ChaosKills { get; set; }
		public int ClassDKills { get; set; }

		public int SCPDeaths { get; set; }
		public int MTFDeaths { get; set; }
		public int ChaosDeaths { get; set; }
		public int ClassDDeaths { get; set; }

		public int PocketDimensionEscapes { get; set; }
		public int Resurrections { get; set; }
		public int SCPItemsUsed { get; set; }
		public int MedicalItemsUsed { get; set; }
		public int PlayersDisarmed { get; set; }
		public int GeneratorsUnlocked { get; set; }
		public int ZoneBlackouts { get; set; }

		public bool StartedWarhead { get; set; }
		public bool Escaped { get; set; }
		public bool RoundWon { get; set; }
		public bool DNT { get; set; }
	}

	public class EventTracker
	{
		public static Dictionary<string, TrackedStats> Stats = new Dictionary<string, TrackedStats>();

		[PluginEvent(ServerEventType.PlayerJoined)]
		public void PlayerJoinEvent(Player player)
		{
			if (player.DoNotTrack || player.UserId == null)
				return;

			if (Stats.ContainsKey(player.UserId))
				Stats[player.UserId].JoinTime = DateTime.Now;
			else
				Stats.Add(player.UserId, new TrackedStats(player));
		}

		[PluginEvent(ServerEventType.PlayerLeft)]
		public void PlayerLeaveEvent(Player player)
		{
			if (player == null || player.UserId == null || player.IsServer || player.DoNotTrack)
				return;

			if (Stats.ContainsKey(player.UserId))
				Stats[player.UserId].SecondsPlayed = ((int)(DateTime.Now - Stats[player.UserId].JoinTime).TotalSeconds);
		}

		[PluginEvent(ServerEventType.PlayerDying)]
		public void PlayerKilledEvent(Player victim, Player attacker, DamageHandlerBase damageHandler)
		{
			if(!(damageHandler is AttackerDamageHandler))
				return;

			if (attacker != null && !attacker.IsServer && !attacker.DoNotTrack && Stats.ContainsKey(attacker.UserId))
			{
				if (!victim.Role.IsHuman())
					Stats[attacker.UserId].SCPKills++;
				else if (victim.ReferenceHub.roleManager.CurrentRole.Team == Team.FoundationForces || victim.ReferenceHub.roleManager.CurrentRole.Team == Team.Scientists)
					Stats[attacker.UserId].MTFKills++;
				else if (victim.ReferenceHub.roleManager.CurrentRole.Team == Team.ChaosInsurgency)
					Stats[attacker.UserId].ChaosKills++;
				else if (victim.ReferenceHub.roleManager.CurrentRole.Team == Team.ClassD)
					Stats[attacker.UserId].ClassDKills++;
			}

			if (victim != null && !victim.IsServer && !victim.DoNotTrack && Stats.ContainsKey(victim.UserId))
			{
				if (!victim.Role.IsHuman())
					Stats[victim.UserId].SCPDeaths++;
				else if (victim.ReferenceHub.roleManager.CurrentRole.Team == Team.FoundationForces || victim.ReferenceHub.roleManager.CurrentRole.Team == Team.Scientists)
					Stats[victim.UserId].MTFDeaths++;
				else if (victim.ReferenceHub.roleManager.CurrentRole.Team == Team.ChaosInsurgency)
					Stats[victim.UserId].ChaosDeaths++;
				else if (victim.ReferenceHub.roleManager.CurrentRole.Team == Team.ClassD)
					Stats[victim.UserId].ClassDDeaths++;
			}
		}


		[PluginEvent(ServerEventType.PlayerExitPocketDimension)]
		public void ExitPocketDimensionEvent(Player player, bool isSuccessful)
		{
			if (isSuccessful)
			{
				if (player.DoNotTrack)
					return;

				if (Stats.ContainsKey(player.UserId))
					Stats[player.UserId].PocketDimensionEscapes++;
			}
		}

		[PluginEvent(ServerEventType.Scp049ResurrectBody)]
		public void SCP049ResurrectEvent(Player player, Player target, BasicRagdoll ragdoll)
		{
			if (player.DoNotTrack)
				return;

			if (Stats.ContainsKey(player.UserId))
				Stats[player.UserId].Resurrections++;
		}

		[PluginEvent(ServerEventType.PlayerUseItem)]
		public void UseItemEvent(Player player, UsableItem item)
		{
			if (item.Category == ItemCategory.SCPItem)
			{
				if (player.DoNotTrack)
					return;

				if (Stats.ContainsKey(player.UserId))
					Stats[player.UserId].SCPItemsUsed++;
			}
		}

		[PluginEvent(ServerEventType.PlayerHandcuff)]
		public void PlayerDisarmEvent(Player player, Player target)
		{
			if (player.DoNotTrack)
				return;

			if (Stats.ContainsKey(player.UserId))
				Stats[player.UserId].PlayersDisarmed++;
		}

		[PluginEvent(ServerEventType.PlayerUnlockGenerator)]
		public void GeneratorUnlockedEvent(Player player, Scp079Generator generator)
		{
			if (player.DoNotTrack)
				return;

			if (Stats.ContainsKey(player.UserId))
				Stats[player.UserId].GeneratorsUnlocked++;
		}

		[PluginEvent(ServerEventType.Scp079BlackoutZone)]
		public void BlackoutZoneEvent(Player player, FacilityZone zone)
		{
			if (player.DoNotTrack)
				return;

			if (Stats.ContainsKey(player.UserId))
				Stats[player.UserId].ZoneBlackouts++;
		}


		[PluginEvent(ServerEventType.WarheadStart)]
		public void WarheadStartEvent(bool isAutomatic, Player player, bool isResumed)
		{
			if (!isAutomatic && !isResumed)
			{
				if (player.DoNotTrack)
					return;

				if (Stats.ContainsKey(player.UserId))
					Stats[player.UserId].StartedWarhead = true;
			}
		}

		[PluginEvent(ServerEventType.PlayerEscape)]
		public void PlayerEscapeEvent(Player player, RoleTypeId newRole)
		{
			if (player.DoNotTrack)
				return;

			if (Stats.ContainsKey(player.UserId))
				Stats[player.UserId].Escaped = true;
		}

		[PluginEvent(ServerEventType.RoundEnd)]
		public void RoundEndEvent(RoundSummary.LeadingTeam leadingTeam)
		{
			foreach (var a in Server.GetPlayers())
			{
				if (a.IsServer || a.DoNotTrack || !Stats.ContainsKey(a.UserId))
					continue;

				switch (leadingTeam)
				{
					case RoundSummary.LeadingTeam.FacilityForces:
						{
							if (a.ReferenceHub.roleManager.CurrentRole.Team == Team.FoundationForces || a.ReferenceHub.roleManager.CurrentRole.Team == Team.Scientists)
							{
								Stats[a.UserId].RoundWon = true;
							}
							break;
						}
					case RoundSummary.LeadingTeam.ChaosInsurgency:
						{
							if (a.ReferenceHub.roleManager.CurrentRole.Team == Team.ChaosInsurgency || a.ReferenceHub.roleManager.CurrentRole.Team == Team.ClassD)
							{
								Stats[a.UserId].RoundWon = true;
							}
							break;
						}
					case RoundSummary.LeadingTeam.Anomalies:
						{
							if (a.ReferenceHub.roleManager.CurrentRole.Team == Team.SCPs)
							{
								Stats[a.UserId].RoundWon = true;
							}
							break;
						}
					default:
						break;
				}

				Log.Info($"{a.UserId} {Stats[a.UserId].ChaosKills}");

				Stats[a.UserId].SecondsPlayed = ((int)(DateTime.Now - Stats[a.UserId].JoinTime).TotalSeconds);
			}

			var statsToSend = Stats.Values.Where(r => r.DNT == false).ToArray();
			//Log.Info(JsonConvert.SerializeObject(statsToSend));

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			Extensions.Post(Plugin.Config.ApiEndpoint + "scpsl/stattracker", new StringContent(JsonConvert.SerializeObject(statsToSend), Encoding.UTF8, "application/json"));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

			Stats.Clear();
		}

		public void RoundStartEvent()
		{
			foreach (var a in Stats)
			{
				a.Value.JoinTime = DateTime.Now;
			}
		}
	}
}
