﻿using Codebreak.Service.World.Game.Area;
using Codebreak.Service.World.Database.Repository;
using Codebreak.Service.World.Game.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codebreak.Service.World.Manager;
using Codebreak.Service.World.Game.Fight;
using Codebreak.Service.World.Network;
using Codebreak.Service.World.Game.Action;
using Codebreak.Service.World.Game.Job;

namespace Codebreak.Service.World.Game.Map
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MapInstance : MessageDispatcher, IMovementHandler
    {
        private static string HASH_CELL = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";

        /// <summary>
        /// 
        /// </summary>
        public FieldTypeEnum FieldType
        {
            get
            {
                return FieldTypeEnum.TYPE_MAP;
            }
        }

        public Pathmaker Pathmaker
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public FightManager FightManager
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Id
        {
            get;
            private set;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public int SubAreaId
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int X
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Y
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Width
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Height
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Data
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string DataKey
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string CreateTime
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public List<int> FightTeam0Cells
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public List<int> FightTeam1Cells
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public SubAreaInstance SubArea
        {
            get
            {
                if (_subArea == null)
                    _subArea = AreaManager.Instance.GetSubArea(SubAreaId);
                return _subArea;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<long, EntityBase> _entityById = new Dictionary<long, EntityBase>();
        private Dictionary<string, EntityBase> _entityByName = new Dictionary<string, EntityBase>();
        private Dictionary<int, MapCell> _cellById = new Dictionary<int, MapCell>();
        private List<MapCell> _cells = new List<MapCell>();
        private SubAreaInstance _subArea;
        private long _nextMonsterId = -1;

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<EntityBase> Entities
        {
            get
            {
                return _entityById.Values;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public List<MapCell> Cells
        {
            get
            {
                return _cells;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool CanAbortMovement
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int RandomTeleportCell
        {
            get
            {
                var actionCell = _cells.Find(cell => cell.NextMap != 0);
                if (actionCell != null)
                    return actionCell.Id;
                actionCell = _cells.Find(cell => cell.Walkable);
                if (actionCell != null)
                    return actionCell.Id;
                return -1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int RandomFreeCell
        {
            get
            {
                MapCell freeCell = null;
                do
                {
                    freeCell = _cells[Util.Next(70, _cells.Count - 100)];
                }
                while (freeCell == null || !freeCell.Walkable);
                return freeCell.Id;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subArea"></param>
        /// <param name="id"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="data"></param>
        /// <param name="dataKey"></param>
        /// <param name="createTime"></param>
        public MapInstance(int subAreaId, int id, int x, int y, int width, int height, string data, string dataKey, string createTime, List<int> f0teamCells, List<int> f1teamCells)
        {
            Id = id;
            SubAreaId = subAreaId;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Data = data;
            DataKey = dataKey;
            CreateTime = createTime;
            FightTeam0Cells = f0teamCells;
            FightTeam1Cells = f1teamCells;
            FightManager = new FightManager(this);

            Initialize();
        }
        
        /// <summary>
        /// 
        /// </summary>
        private void Initialize()
        {
            var triggers = MapTriggerRepository.Instance.GetTriggers(Id);

            for (int i = 0; i < Data.Length; i += 10)
            {
                string currentCell = Data.Substring(i, 10);
                var id = i / 10;

                byte[] cellData = new byte[currentCell.Length];
                for (int j = 0; j < currentCell.Length; j++)
                {
                    cellData[j] = (byte)HASH_CELL.IndexOf(currentCell[j]);
                }

                var nextMap = 0;
                var nextCell = 0;
                if (triggers != null)
                {
                    var trigger = triggers.Find(trig => trig.CellId == id);
                    if (trigger != null)
                    {
                        nextMap = trigger.NewMap;
                        nextCell = trigger.NewCell;
                    }
                }

                var cell = new MapCell(this, id, cellData, nextMap, nextCell);
                if(cell.InteractiveObject != null)
                {
                    base.AddUpdatable(cell.InteractiveObject);
                    cell.InteractiveObject.AddHandler(base.Dispatch);
                }
                _cellById.Add(id, cell);
                _cells.Add(cell);

            }

            Pathmaker = new Pathmaker(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public MapCell GetCell(int id)
        {
            MapCell cell = null;
            _cellById.TryGetValue(id, out cell);
            return cell;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellId"></param>
        /// <returns></returns>
        public int GetNearestCell(int cellId)
        {
            foreach(var currentCellId in CellZone.GetAdjacentCells(this, cellId))
            {
                var cell = GetCell(currentCellId);
                if(cell != null)
                {
                    if (cell.Walkable)
                        return currentCellId;
                }
            }
            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool HasTaxCollector()
        {
            return _entityById.Values.Any(entity => entity.Type == EntityTypeEnum.TYPE_TAX_COLLECTOR);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellId"></param>
        /// <returns></returns>
        public bool IsWalkable(int cellId)
        {
            MapCell cell = GetCell(cellId);
            if (cell != null)
            {
                return cell.Walkable;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public EntityBase GetEntity(long id)
        {
            if (_entityById.ContainsKey(id))
                return _entityById[id];
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SpawnMonsters()
        {
            SpawnMonsters(RandomFreeCell);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SpawnMonsters(int cellId)
        {
            var monsters = new MonsterGroupEntity(_nextMonsterId--, Id, cellId);
            monsters.StartAction(GameActionTypeEnum.MAP);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        public void SpawnEntity(EntityBase entity)
        {
            AddMessage(() =>
            {
                if (!_entityById.ContainsKey(entity.Id))
                {
                    _entityById.Add(entity.Id, entity);
                    _entityByName.Add(entity.Name.ToLower(), entity);

                    base.Dispatch(WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_ADD, entity));

                    base.AddUpdatable(entity);

                    if (entity.Type == EntityTypeEnum.TYPE_CHARACTER)
                    {
                        base.AddHandler(entity.Dispatch);
                        entity.CachedBuffer = true;
                        entity.Dispatch(WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_ADD, entity.Map.Entities.ToArray()));
                        entity.Dispatch(WorldMessage.GAME_DATA_SUCCESS());
                        entity.Dispatch(WorldMessage.FIGHT_COUNT(FightManager.FightCount));
                        foreach (var fight in FightManager.Fights)                        
                            fight.SendMapFightInfos(entity);                        
                        entity.CachedBuffer = false;
                    }
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        public void DestroyEntity(EntityBase entity)
        {
            if (_entityById.ContainsKey(entity.Id))
            {
                _entityById.Remove(entity.Id);
                _entityByName.Remove(entity.Name.ToLower());

                base.RemoveUpdatable(entity);
                base.RemoveHandler(entity.Dispatch);
                base.Dispatch(WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_REMOVE, entity));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="skillId"></param>
        public void InteractiveExecute(CharacterEntity character, int cellId, int skillId)
        {
            var cell = GetCell(cellId);
            if(cell != null)
            {
                if(cell.InteractiveObject != null)
                {
                    cell.InteractiveObject.UseWithSkill(character, (SkillIdEnum)skillId);
                }
                else
                {
                    character.Dispatch(WorldMessage.SERVER_INFO_MESSAGE("Cet objet interactif est en cours de developpement."));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellId"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public MovementPath DecodeMovement(int cellId, string path)
        {
            return Pathfinding.IsValidPath(this, cellId, path);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cellId"></param>
        /// <param name="movementPath"></param>
        public void Move(EntityBase entity, int cellId, string movementPath)
        {
            AddMessage(() =>
                {
                    var path = DecodeMovement(cellId, movementPath);
                    if (path != null)                    
                        entity.Move(path);                    
                });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="path"></param>
        /// <param name="cellId"></param>
        public void MovementFinish(EntityBase entity, MovementPath path, int cellId)
        {
            if (entity.CellId == cellId)
                return;

            entity.Orientation = path.GetDirection(path.LastStep);

            if (entity.Type == EntityTypeEnum.TYPE_CHARACTER)
            {
                var cell = GetCell(cellId);
                if (cell != null)
                {
                    if (cell.NextMap != 0 && cell.NextCell != 0)
                    {
                        entity.Teleport(cell.NextMap, cell.NextCell);
                        return;
                    }
                }                
                foreach (var monsterGroup in _entityById.Values.OfType<MonsterGroupEntity>())
                {                    
                    if (Pathfinding.GoalDistance(this, cellId, monsterGroup.CellId) <= monsterGroup.AggressionRange)
                    {
                        if (FightTeam0Cells.Count == 0 || FightTeam1Cells.Count == 1)
                        {
                            entity.Dispatch(WorldMessage.SERVER_ERROR_MESSAGE("Unable to start fight withouth fightCells"));
                        }
                        else
                        {
                            monsterGroup.StopAction(GameActionTypeEnum.MAP);
                            FightManager.StartMonsterFight(entity as CharacterEntity, monsterGroup);
                            return;
                        }
                    }
                }
            }

            entity.CellId = cellId;
        }
    }    
}