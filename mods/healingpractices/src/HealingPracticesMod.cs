using Foundation.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace healingpractices.src
{
    public class HealingPracticesMod: ModSystem
    {
        private ICoreServerAPI _serverAPI;
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            SecondChanceConfig.Current = api.LoadOrCreateConfig<SecondChanceConfig>("SecondChanceConfig.json");
            api.RegisterEntityBehaviorClass("healthex", typeof(EntityBehaviorHealthEx));
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            _serverAPI = api;
            _serverAPI.Event.PlayerJoin += OnPlayerJoin;
            _serverAPI.Event.PlayerCreate += OnPlayerCreate;

            api.RegisterCommand("iamdm", "Sets you as a current Dungeon Master", "", (IServerPlayer player, int groupId, CmdArgs args) =>
             {
                 SecondChanceConfig.Current.DMUID = player.PlayerUID;
                 //integrate with DM tools
                 api.SaveDataFile("SecondChanceConfig.json", SecondChanceConfig.Current);
             });
        }
        private void OnPlayerJoin(IServerPlayer player)
        {
            if (_serverAPI.Side == EnumAppSide.Server)
            {
                var saveGuid = _serverAPI.WorldManager.SaveGame.SavegameIdentifier;
                var data = new WoundsData { SaveGuid = saveGuid, WoundsLeft = SecondChanceConfig.Current.MaxLifes, LastWoundDate = null };
                var rawData = player.WorldData.GetModdata(Constants.WoundsLeftField);
                WoundsData playerServerData;
                if (rawData == null)
                {
                    playerServerData = data;
                    player.WorldData.SetModdata(Constants.WoundsLeftField, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(playerServerData)));
                    player.BroadcastPlayerData();
                }
            }
        }

        private void OnPlayerCreate(IServerPlayer player)
        {
            if (_serverAPI.Side == EnumAppSide.Server)
            {
                var saveGuid = _serverAPI.WorldManager.SaveGame.SavegameIdentifier;
                var data = new WoundsData { SaveGuid = saveGuid, WoundsLeft = SecondChanceConfig.Current.MaxLifes, LastWoundDate = null };
                var rawData = player.WorldData.GetModdata(Constants.WoundsLeftField);
                WoundsData playerServerData;
                if (rawData == null)
                {
                    playerServerData = data;
                    player.WorldData.SetModdata(Constants.WoundsLeftField, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(playerServerData)));
                    player.BroadcastPlayerData();
                }
            }
        }

    }
}
