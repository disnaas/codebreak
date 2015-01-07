﻿using Codebreak.Service.World.Database.Repository;
using Codebreak.Service.World.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codebreak.Service.World.Game.Entity
{
    /// <summary>
    /// 
    /// </summary>
    public class PersistentInventory : InventoryBag
    {
        /// <summary>
        /// 
        /// </summary>
        public long ActualUser
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int OwnerType
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long OwnerId
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public override long Kamas
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public override List<InventoryItemDAO> Items
        {
            get 
            { 
                return m_items; 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private List<InventoryItemDAO> m_items;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ownerType"></param>
        /// <param name="ownerId"></param>
        public PersistentInventory(int ownerType, long ownerId)
        {
            m_items = new List<InventoryItemDAO>();
            m_items.AddRange(InventoryItemRepository.Instance.GetByOwner(ownerType, ownerId));

            OwnerType = ownerType;
            OwnerId = ownerId;
            ActualUser = -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public override void OnItemAdded(InventoryItemDAO item)
        {
            item.OwnerId = OwnerId;
            item.OwnerType = OwnerType;
        }
    }
}