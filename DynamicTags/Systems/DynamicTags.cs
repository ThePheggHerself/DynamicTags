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
        [CommandHandler(typeof(ClientCommandHandler))]
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
                        {
                            pSender.ReferenceHub.serverRoles.SetGroup(ServerStatic.GetPermissionsHandler().GetGroup(data.Group), true);
                            pSender.ReferenceHub.serverRoles.RemoteAdmin = true;
                            pSender.ReferenceHub.serverRoles.RemoteAdminMode = ServerRoles.AccessMode.LocalAccess;

                            if (data.Perms != 0)
                                pSender.ReferenceHub.serverRoles.Permissions = data.Perms;

                        }

                        pSender.ReferenceHub.serverRoles.SetText(data.Tag);
                        pSender.ReferenceHub.serverRoles.SetColor(data.Colour);



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

        [CommandHandler(typeof(RemoteAdminCommandHandler))]
        public class DynamicTagListCommand : ICommand
        {
            public string Command => "dynamictaglist";

            public string[] Aliases { get; } = { "dtaglist", "dtl" };

            public string Description => "Lists all dynamic tags";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (sender is PlayerCommandSender pSender)
                {
                    if (sender.CheckPermission(PlayerPermissions.PermissionsManagement))
                    {
                        List<string> tags = new List<string>();

                        foreach(var tag in Tags)
                        {
                            tags.Add($"{tag.Key} | {tag.Value.Tag}");
                        }

                        response = string.Join("\n", tags);
                        return true;
                    }
                    response = "You cannot run this command";
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
                    else if (ulong.TryParse(a.UserID, out ulong result))
                        a.UserID = $"{a.UserID}@discord";
                    else
                        a.UserID = $"{a.UserID}@northwood";

                    //Adds the tags to the tag list.
                    Tags.Add(a.UserID, a);
                }

                Log.Info($"{Tags.Count} tags loaded");
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        [PluginEvent(ServerEventType.PlayerCheckReservedSlot)]
        public PlayerCheckReservedSlotCancellationData OnReservedSlotCheck(PlayerCheckReservedSlotEvent args)
        {
            if (args.HasReservedSlot)
                return PlayerCheckReservedSlotCancellationData.LeaveUnchanged();

            else if (args.Userid.ToLower().Contains("northwood") && Plugin.Config.AutomaticNorthwoodReservedSlot)
            {
                Log.Info($"Reserved slot bypass for {args.Userid} (Northwood ID detected)");
                return PlayerCheckReservedSlotCancellationData.BypassCheck();
            }
            else if (Tags.ContainsKey(args.Userid) && Tags[args.Userid].ReservedSlot)
            {
                Log.Info($"Reserved slot bypass for {args.Userid} (Dynamic Tag)");
                return PlayerCheckReservedSlotCancellationData.BypassCheck();
            }

            else return PlayerCheckReservedSlotCancellationData.LeaveUnchanged();
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void OnPlayerJoin(PlayerJoinedEvent args)
        {
            //Checks if the user has a tag
            if (Tags.ContainsKey(args.Player.UserId))
            {
                TagData data = Tags[args.Player.UserId];

                //This is to stop situations where users have locally assigned perms but gets overridden by NULL perms from the external server.
                if (!string.IsNullOrEmpty(data.Group))
                    args.Player.ReferenceHub.serverRoles.SetGroup(ServerStatic.GetPermissionsHandler().GetGroup(data.Group), true);

                args.Player.ReferenceHub.serverRoles.SetText(data.Tag);
                args.Player.ReferenceHub.serverRoles.SetColor(data.Colour);

                if (data.Perms != 0)
                    args.Player.ReferenceHub.serverRoles.Permissions = data.Perms;

                args.Player.SendConsoleMessage("Dynamic tag loaded: " + data.Tag);
                Log.Info($"Tag found for {args.Player.UserId}: {data.Tag}");
            }
        }
    }
}
