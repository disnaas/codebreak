﻿using Codebreak.Framework.Generic;
using Codebreak.Service.World.Game.Action;
using Codebreak.Service.World.Database.Repository;

namespace Codebreak.Service.World.Manager
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class NpcManager : Singleton<NpcManager>
    {
        /// <summary>
        /// 
        /// </summary>
        public void Initialize()
        {
            long npcCount = 0;
            foreach(var npcInstance in NpcInstanceRepository.Instance.GetAll())
            {
                EntityManager.Instance.CreateNpc(npcInstance);
                npcCount++;
            }

            Logger.Info("NpcManager : " + npcCount + " NpcInstance loaded.");
        }
    }
}