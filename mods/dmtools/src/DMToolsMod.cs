using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace dmtools.src
{
    public class DMToolsMod: ModSystem
    {
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            api.Event.PlayerNowPlaying += Event_PlayerJoin;
            api.RegisterCommand("setrpname", "sets visible player name", "",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    var playerName = args.PopWord(player.PlayerName);
                    var tag = args.PopAll();
                    if (string.IsNullOrWhiteSpace(tag))
                    {
                        player.SendMessage(groupId, "You have to provide name", EnumChatType.CommandError);
                        return;
                    }
                    var client = player.Entity.World.AllPlayers.FirstOrDefault(p => p.PlayerName == playerName);
                    if (client == null)
                    {
                        player.SendMessage(groupId, "User not found", EnumChatType.CommandError);
                        return;
                    }
                    client.Entity.GetBehavior<EntityBehaviorNameTag>().SetName(tag);
                    client.WorldData.SetModdata("rpname", Encoding.UTF8.GetBytes(tag));
                }, Privilege.controlserver);
            api.RegisterCommand("roll", "Rolls a die", "",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    var die = args.PopWord("1d20");
                    var regex = new Regex(@"(\d{0,2})d(\d{1,2})");
                    var match = regex.Match(die.ToLower());
                    var num = 1;
                    var sides = 20;
                    if (!match.Success)
                    {
                        player.SendMessage(groupId, "Wrong die format", EnumChatType.CommandError);
                        return;
                    }
                    else
                    {
                        num = string.IsNullOrEmpty(match.Groups[1].Value) ? 1 : int.Parse(match.Groups[1].Value);
                        sides = int.Parse(match.Groups[2].Value);
                        if (sides < 2)
                        {
                            player.SendMessage(groupId, "A die has to have at least 2 sides", EnumChatType.CommandError);
                            return;
                        }
                        var rand = new Random();
                        var sum = 0;
                        for (var i = 0; i < num; i++)
                        {
                            sum += rand.Next(1, sides);
                        }
                        api.BroadcastMessageToAllGroups(string.Format("User {0} rolled {1} dice and got {2}", player.Entity.GetBehavior<EntityBehaviorNameTag>().DisplayName, die, sum), EnumChatType.Notification);
                    }
                }, Privilege.chat);
        }
        private void Event_PlayerJoin(IServerPlayer byPlayer)
        {
            var data = byPlayer.WorldData.GetModdata("rpname");
            if (data != null)
            {
                byPlayer.Entity.GetBehavior<EntityBehaviorNameTag>().SetName(Encoding.UTF8.GetString(data));
            }
        }
    }
}
