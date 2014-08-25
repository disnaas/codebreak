﻿using Codebreak.Service.World.Game.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codebreak.Service.World.Game.Fight.Challenges
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class BoldChallenge : ChallengeBase
    {
        /// <summary>
        /// 
        /// </summary>
        public BoldChallenge()
            : base(ChallengeTypeEnum.BOLD)
        {
            BasicDropBonus = 10;
            BasicXpBonus = 10;

            TeamDropBonus = 15;
            TeamXpBonus = 15;

            ShowTarget = false;
            TargetId = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fighter"></param>
        public override void EndTurn(FighterBase fighter)
        {
            var nearestEnnemis = Pathfinding.GetEnnemiesNear(fighter.Fight, fighter.Team, fighter.Cell.Id);
            if(nearestEnnemis.Count() == 0)
            {
                base.OnFailed();
            }
        }
    }
}