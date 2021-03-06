﻿using System;
using Codebreak.Framework.Network;
using Codebreak.Service.World.Game.Entity;
using Codebreak.Service.World.Game;
using Codebreak.Service.World.Network;

namespace Codebreak.Service.World.Frame
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MapFrame : AbstractNetworkFrame<MapFrame, CharacterEntity, string>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message.Length < 2)
                return null;

            switch (message[0])
            {
                case 'e':
                    switch(message[1])
                    {
                        case 'D': // onDirection
                            return EmoteDirection;

                        case 'U': // onUse
                            return EmoteUse;
                    }
                    break;

                case 'f':
                    switch (message[1])
                    {

                        case 'L':
                            return FightList;

                        case 'D':
                            return FightDetails;
                    }
                    break;

                case 'G':
                    switch(message[1])
                    {
                        case 'P': // alignment
                            if (message.Length < 3)
                                return null;

                            switch (message[2])
                            {
                                case '+':
                                    return AlignmentEnable;
                                    
                                case '-':
                                    return AlignmentDisable;

                                case '*':
                                    return AlignmentDisableCost;
                            }
                            break;
                    }
                    break;
            }

            return null;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void EmoteDirection(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
                {
                    character.ChangeDirection(int.Parse(message.Substring(2)));
                });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void EmoteUse(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
                {
                    character.EmoteUse(int.Parse(message.Substring(2)));
                });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void AlignmentDisableCost(CharacterEntity character, string message)
        {
            character.SafeDispatch(WorldMessage.ALIGNMENT_DISABLE_COST((character.Honour / 100) * 5));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void AlignmentDisable(CharacterEntity character, string message)
        {
            character.AddMessage(() => character.DisableAlignment());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void AlignmentEnable(CharacterEntity character, string message)
        {
            character.AddMessage(() => character.EnableAlignment());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="message"></param>
        private void FightList(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (character.Map.FightManager.FightCount > 0)
                {
                    character.Dispatch(WorldMessage.FIGHT_LIST(character.Map.FightManager.Fights));
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="message"></param>
        private void FightDetails(CharacterEntity character, string message)
        {
            if (message.Length < 3)
                return;

            var fightId = int.Parse(message.Substring(2));

            character.AddMessage(() =>
                {
                    var fight = character.Map.FightManager.GetFight(fightId);

                    if (fight == null)
                        return;

                    character.Dispatch(WorldMessage.FIGHT_DETAILS(fight));
                });
        }
    }
}
