﻿using System.Text;
using System.Threading;
using Codebreak.Framework.Command;
using Codebreak.Framework.Configuration;
using Codebreak.Framework.Configuration.Providers;
using Codebreak.Framework.Network;
using Codebreak.Service.World.Command;
using Codebreak.Service.World.Database;
using Codebreak.Service.World.Database.Repository;
using Codebreak.Service.World.Frames;
using Codebreak.Service.World.Game;
using Codebreak.Service.World.Manager;
using Codebreak.Service.World.RPC;
using System;
using Codebreak.Framework.Database;
using System.Diagnostics;
using Codebreak.RPC.Protocol;
using System.Threading.Tasks;
using Codebreak.Framework.Generic;

namespace Codebreak.Service.World
{
    /// <summary>
    /// 
    /// </summary>
    public class WorldService : TcpServerBase<WorldService, WorldClient>
    {
        /// <summary>
        /// 
        /// </summary>
        [Configurable("WorldSaveInternal")]
        public static int WorldSaveInternal = 60 * 1000;

        /// <summary>
        /// 
        /// </summary>
        [Configurable("WorldServiceIP")]
        public static string WorldServiceIP = "127.0.0.1";

        /// <summary>
        /// 
        /// </summary>
        [Configurable("WorldServicePort")]
        public static int WorldServicePort = 5555;

        /// <summary>
        /// 
        /// </summary>
        public ConfigurationManager ConfigurationManager
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public CommandManager<WorldCommandContext> CommandManager
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public MessageDispatcher Dispatcher
        {
            get;
            private set;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configPath"></param>
        public void Start(string configPath)
        {
            ConfigurationManager = new ConfigurationManager();
            ConfigurationManager.RegisterAttributes();
            ConfigurationManager.Add(new JsonConfigurationProvider(configPath), true);
            ConfigurationManager.Load();

            CommandManager = new CommandManager<WorldCommandContext>();
            CommandManager.RegisterCommands();

            Dispatcher = new MessageDispatcher();
            AddUpdatable(Dispatcher);

            WorldDbMgr.Instance.Initialize();
            AccountManager.Instance.Initialize();
            SpellManager.Instance.Initialize();
            AreaManager.Instance.Initialize();
            MapManager.Instance.Initialize();
            NpcManager.Instance.Initialize();
            GuildManager.Instance.Initialize();
            RPCManager.Instance.Initialize();
            AddUpdatable(RPCManager.Instance);

            // TRIGGER LEECH
            //var lines = System.IO.File.ReadAllLines(@"D:\Codebreak\triggersFiltered.txt");
            //var triggers = new System.Collections.Generic.List<Database.Structure.MapTriggerDAO>();
            //foreach (var line in lines)
            //{
            //    //8049~0810171747:276>8036~0706131721:303
            //    var triggerData = line.Split('>');
            //    var fromData = triggerData[0].Split(':');
            //    var toData = triggerData[1].Split(':');

            //    var fromMap = int.Parse(fromData[0].Split('~')[0]);
            //    var fromCell = int.Parse(fromData[1]);

            //    var toMap = int.Parse(toData[0].Split('~')[0]);
            //    var toCell = int.Parse(toData[1]);

            //    var trigger = new Database.Structure.MapTriggerDAO() { MapId = fromMap, CellId = fromCell, NewMap = toMap, NewCell = toCell };
            //    if (MapManager.Instance.GetById(trigger.MapId) != null && MapManager.Instance.GetById(trigger.NewMap) != null)
            //        triggers.Add(trigger);
            //}

            //MapTriggerRepository.Instance.Insert(triggers);

            int minWorkingThreads = -1, minCompletionPortThreads = -1, maxWorkingThreads = -1, maxCompletionPortThreads = -1;

            ThreadPool.GetMinThreads(out minWorkingThreads, out minCompletionPortThreads);
            ThreadPool.GetMaxThreads(out maxWorkingThreads, out maxCompletionPortThreads);

            Logger.Info("Min Working Threads         : " + minWorkingThreads);
            Logger.Info("Min Completion Port Threads : " + minCompletionPortThreads);
            Logger.Info("Max Working Threads         : " + maxWorkingThreads);
            Logger.Info("Max Completion Port Threads : " + maxCompletionPortThreads);

            AddTimer(WorldSaveInternal, UpdateWorld);

            base.Start(WorldServiceIP, WorldServicePort);
        }

        #region Network

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        protected override void OnClientConnected(WorldClient client)
        {
            AddMessage(() =>
            {
                client.FrameManager.AddFrame(AuthentificationFrame.Instance);
                client.Send(WorldMessage.HELLO_GAME());
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        protected override void OnClientDisconnected(WorldClient client)
        {
            AddMessage(() =>
            {
                if (client.CurrentCharacter != null)
                {
                    EntityManager.Instance.CharacterDisconnected(client.CurrentCharacter);

                    client.Characters = null;
                    client.CurrentCharacter = null;
                }
                AccountManager.Instance.ClientDisconnected(client);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        protected override void OnDataReceived(WorldClient client, byte[] buffer, int offset, int count)
        {
            foreach (var message in client.Receive(buffer, offset, count))
            {
                AddMessage(() =>
                {
                    Logger.Debug("Client : " + message);

					Stopwatch sw = Stopwatch.StartNew();

                    if (client.CurrentCharacter != null)
                    {
                        if (!client.CurrentCharacter.FrameManager.ProcessMessage(message))
                        {
                            client.CurrentCharacter.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                        }
                    }
                    else
                    {
                        if (!client.FrameManager.ProcessMessage(message))
                        {
                            client.Send(WorldMessage.BASIC_NO_OPERATION());
                        }
                    }      

					Logger.Debug("Message processed in : " + sw.ElapsedMilliseconds);
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void SendToAll(string message)
        {
            base.SendToAll(Encoding.Default.GetBytes(message + (char)0x00));
        }

        #endregion

        #region World Management

        /// <summary>
        /// 
        /// </summary>
        public void UpdateWorld()
        {
            Stopwatch updateTimer = new Stopwatch();
            WorldService.Instance.AddLinkedMessages( 
                () => WorldService.Instance.Dispatcher.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_WORLD_SAVING)),
                () => RPCManager.Instance.UpdateState(GameState.STARTING),
                updateTimer.Start,
                GuildRepository.Instance.UpdateAll,
                TaxCollectorRepository.Instance.UpdateAll,
                CharacterRepository.Instance.UpdateAll,
                CharacterAlignmentRepository.Instance.UpdateAll,
                CharacterGuildRepository.Instance.UpdateAll,
                SpellBookEntryRepository.Instance.UpdateAll,
                InventoryItemRepository.Instance.UpdateAll,
                updateTimer.Stop,
                () => Logger.Info("WorldService : World update performed in : " + updateTimer.ElapsedMilliseconds + " ms"),
                () => RPCManager.Instance.UpdateState(GameState.ONLINE),
                () => WorldService.Instance.Dispatcher.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_WORLD_SAVING_FINISHED))
            );
        }

        #endregion
    }
}
