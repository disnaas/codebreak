﻿using Codebreak.Service.World.Game.Entity;
using Codebreak.Service.World.Game.Exchange;
using Codebreak.Service.World.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codebreak.Service.World.Game.Action
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class GamePlayerExchangeAction : GameExchangeActionBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="localEntity"></param>
        /// <param name="distantEntity"></param>
        public GamePlayerExchangeAction(CharacterEntity localEntity, CharacterEntity distantEntity)
            : base(new PlayerExchange(localEntity, distantEntity), localEntity, distantEntity)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Start()
        {
            Exchange.Dispatch(WorldMessage.EXCHANGE_REQUEST(Entity.Id, DistantEntity.Id));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public override void Stop(params object[] args)
        {
            IsFinished = true;

            if (int.Parse(args[0].ToString()) == Entity.Id)
                DistantEntity.StopAction(GameActionTypeEnum.EXCHANGE);
            else
                Entity.StopAction(GameActionTypeEnum.EXCHANGE);

            base.Leave(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public override void Abort(params object[] args)
        {
            IsFinished = true;

            if (int.Parse(args[0].ToString()) == Entity.Id)
                DistantEntity.AbortAction(GameActionTypeEnum.EXCHANGE);
            else
                Entity.AbortAction(GameActionTypeEnum.EXCHANGE);

            base.Leave();
        }
    }
}