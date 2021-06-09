using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace healingpractices.src
{
    public class EntityBehaviorHealthEx : EntityBehaviorHealth
    {
        private Dictionary<long, Tuple<List<IAiTask>, double>> removedTasks = new Dictionary<long, Tuple<List<IAiTask>, double>>();
        public EntityBehaviorHealthEx(Entity entity) : base(entity)
        {
        }
        public override void OnEntityReceiveDamage(DamageSource damageSource, float damage)
        {
            if (damageSource.Type != EnumDamageType.Heal && entity.World.Side == EnumAppSide.Server)
            {
                if (Health < damage) // going to die
                {
                    var player = (entity as EntityPlayer).Player as IServerPlayer;
                    var data = JsonConvert.DeserializeObject<WoundsData>(Encoding.UTF8.GetString(player.WorldData.GetModdata(Constants.WoundsLeftField)));
                    data.WoundsLeft--;
                    data.LastWoundDate = entity.World.Calendar.TotalDays;
                    player.WorldData.SetModdata(Constants.WoundsLeftField, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)));
                    player.BroadcastPlayerData();

                    if (data.WoundsLeft > 0)
                    {
                        var hostileEntities = entity.Api.World.GetEntitiesAround(entity.SidedPos.XYZ, 100, 10, e =>
                         {
                             if (!(e is EntityAgent)) return false;
                             var agent = e as EntityAgent;
                             if (!agent.HasBehavior<EntityBehaviorTaskAI>()) return false;
                             var task = agent.GetBehavior<EntityBehaviorTaskAI>().taskManager.GetTask<AiTaskMeleeAttack>();
                             if (task == null) return false;
                             return true;
                         });

                        foreach (var he in hostileEntities)
                        {
                            var tasks = new List<IAiTask>();
                            var manager = (he as EntityAgent).GetBehavior<EntityBehaviorTaskAI>().taskManager;
                            IAiTask task = manager.GetTask<AiTaskMeleeAttack>();
                            manager.StopTask(typeof(AiTaskMeleeAttack));
                            manager.RemoveTask(task);
                            tasks.Add(task);
                            do
                            {
                                task = manager.GetTask<AiTaskSeekEntity>();
                                if (task != null)
                                {
                                    manager.StopTask(typeof(AiTaskSeekEntity));
                                    manager.RemoveTask(task);
                                    tasks.Add(task);
                                }
                            }
                            while (task != null);

                            removedTasks[he.EntityId] = new Tuple<List<IAiTask>, double>(tasks, entity.World.Calendar.TotalHours);
                        }

                        (entity.World.Api as ICoreServerAPI).SendMessage(player, 0, "You have been wounded.You need 3 days to recover", EnumChatType.Notification);
                        if (SecondChanceConfig.Current.DMUID != null)
                        {
                            var dm = entity.World.PlayerByUid(SecondChanceConfig.Current.DMUID);
                            (entity.World.Api as ICoreServerAPI).SendMessage(dm, 0, string.Format("Player {0} has been wounded", entity.GetBehavior<EntityBehaviorNameTag>().DisplayName), EnumChatType.Notification);
                        }

                        return;
                    }
                    else
                    {
                        (entity.World.Api as ICoreServerAPI).SendMessage(player, 0, "You have been killed. Good luck in your next life", EnumChatType.Notification);
                        if (SecondChanceConfig.Current.DMUID != null)
                        {
                            var dm = entity.World.PlayerByUid(SecondChanceConfig.Current.DMUID);
                            (entity.World.Api as ICoreServerAPI).SendMessage(dm, 0, string.Format("Player {0} is considered dead", entity.GetBehavior<EntityBehaviorNameTag>().DisplayName), EnumChatType.Notification);
                        }
                    }
                }
            }
            base.OnEntityReceiveDamage(damageSource, damage);
        }
        public override void OnGameTick(float deltaTime)
        {
            base.OnGameTick(deltaTime);
            if (entity.World.Side == EnumAppSide.Server)
            {
                {//recover from wound
                    var player = (entity as EntityPlayer).Player as IServerPlayer;
                    var rawData = player.WorldData.GetModdata(Constants.WoundsLeftField);
                    if (rawData == null)
                        return;
                    var data = JsonConvert.DeserializeObject<WoundsData>(Encoding.UTF8.GetString(rawData));
                    if (entity.World.Calendar.TotalDays - data.LastWoundDate > SecondChanceConfig.Current.HealWoundTime)
                    {
                        data.WoundsLeft++;
                        data.LastWoundDate = (data.WoundsLeft == SecondChanceConfig.Current.MaxLifes) ? (double?)null : entity.World.Calendar.TotalDays;
                        (entity.World.Api as ICoreServerAPI).SendMessage(player, 0, "You have recovered from wound", EnumChatType.Notification);
                        if (SecondChanceConfig.Current.DMUID != null)
                        {
                            var dm = entity.World.PlayerByUid(SecondChanceConfig.Current.DMUID);
                            (entity.World.Api as ICoreServerAPI).SendMessage(dm, 0, string.Format("Player {0} has recovered from wound", entity.GetBehavior<EntityBehaviorNameTag>().DisplayName), EnumChatType.Notification);
                        }
                    }
                }
                {
                    var time = entity.World.Calendar.TotalHours;
                    var entitiesToRevert = removedTasks.Where(e => time - e.Value.Item2 > 1).ToList();
                    foreach (var ent in entitiesToRevert)
                    {
                        if (!(entity.Api.World as IServerWorldAccessor).LoadedEntities.ContainsKey(ent.Key))
                        {
                            removedTasks.Remove(ent.Key);
                            continue;
                        }
                        var loadedEntity = ((entity.Api.World as IServerWorldAccessor).LoadedEntities.First(e => e.Key == ent.Key).Value as EntityAgent);
                        foreach (var task in ent.Value.Item1)
                        {
                            loadedEntity.GetBehavior<EntityBehaviorTaskAI>().taskManager.AddTask(task);
                        }
                        removedTasks.Remove(ent.Key);
                    }
                }
            }
        }
    }
}
