﻿using System.Text;
using System.Threading;
using Codebreak.Framework.Command;
using Codebreak.Framework.Configuration;
using Codebreak.Framework.Configuration.Providers;
using Codebreak.Framework.Network;
using Codebreak.Service.World.Commands;
using Codebreak.Service.World.Database;
using Codebreak.Service.World.Database.Repositories;
using Codebreak.Service.World.Frames;
using Codebreak.Service.World.Game;
using Codebreak.Service.World.Manager;
using Codebreak.Service.World.RPC;
using System;
using Codebreak.Framework.Database;
using System.Diagnostics;
using Codebreak.RPC.Protocol;
using System.Threading.Tasks;

namespace Codebreak.Service.World
{
    public class WorldService : TcpServerBase<WorldService, WorldClient>
    {
        [Configurable("WorldSaveInternal")]
        public static int WorldSaveInternal = 60 * 1000;

        [Configurable("WorldServiceIP")]
        public static string WorldServiceIP = "127.0.0.1";

        [Configurable("WorldServicePort")]
        public static int WorldServicePort = 5555;

        public ConfigurationManager ConfigurationManager
        {
            get;
            private set;
        }

        public CommandManager<WorldCommandContext> CommandManager
        {
            get;
            private set;
        }

        public MessageDispatcher Dispatcher
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        private Stopwatch _updateTimer;

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
        protected override void OnClientConnected(WorldClient client)
        {
            AddMessage(() =>
            {
                client.FrameManager.AddFrame(AuthentificationFrame.Instance);
                client.Send(WorldMessage.HELLO_GAME());
            });
        }

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

        protected override void OnDataReceived(WorldClient client, byte[] buffer, int offset, int count)
        {
            foreach (var message in client.Receive(buffer, offset, count))
            {
                AddMessage(() =>
                {
                    Logger.Debug("Client : " + message);

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
                });
            }
        }

        public void SendToAll(string message)
        {
            base.SendToAll(Encoding.Default.GetBytes(message + (char)0x00));
        }

        #endregion

        #region World Management

        public void UpdateWorld()
        {
            WorldService.Instance.AddMessage(() =>
            {
                WorldService.Instance.Dispatcher.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_WORLD_SAVING));

                RPCManager.Instance.UpdateState(GameState.STARTING);

                WorldService.Instance.AddMessage(() =>
                {
                    AreaManager.Instance.BlockQueues();

                    WorldService.Instance.AddMessage(() =>
                    {
                        try
                        {
                            _updateTimer = Stopwatch.StartNew();

                            Task.WaitAll
                            (
                                Task.Factory.StartNew(GuildRepository.Instance.UpdateAll),
                                Task.Factory.StartNew(TaxCollectorRepository.Instance.UpdateAll),
                                Task.Factory.StartNew(CharacterRepository.Instance.UpdateAll),
                                Task.Factory.StartNew(CharacterAlignmentRepository.Instance.UpdateAll),
                                Task.Factory.StartNew(CharacterGuildRepository.Instance.UpdateAll),
                                Task.Factory.StartNew(SpellBookEntryRepository.Instance.UpdateAll),
                                Task.Factory.StartNew(InventoryItemRepository.Instance.UpdateAll)
                            );

                            var updateTime = _updateTimer.ElapsedMilliseconds;
                            _updateTimer.Stop();

                            Logger.Info("WorldService : World update performed in : " + updateTime + " ms");
                        }
                        catch(Exception ex)
                        {
                            Logger.Error("WorldUpdate failed : " + ex.ToString());
                        }

                        WorldService.Instance.AddMessage(() =>
                        {
                            AreaManager.Instance.ResumeQueues();

                            RPCManager.Instance.UpdateState(GameState.ONLINE);

                            WorldService.Instance.AddMessage(() =>
                            {
                                WorldService.Instance.Dispatcher.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_WORLD_SAVING_FINISHED));
                            });
                        });
                    });
                });
            });
        }

        #endregion
    }
}
