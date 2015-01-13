﻿using Codebreak.Framework.Database;
using Codebreak.Service.World.Database.Repository;
using PropertyChanged;

namespace Codebreak.Service.World.Database.Structure
{
    /// <summary>
    /// 
    /// </summary>
    [Table("npcinstance")]
    public sealed class NpcInstanceDAO : DataAccessObject<NpcInstanceDAO>
    {
        [Key]
        public int Id
        {
            get;
            set;
        }
        public int MapId
        {
            get;
            set;
        }
        public int TemplateId
        {
            get;
            set;
        }
        public int CellId
        {
            get;
            set;
        }
        public int Orientation
        {
            get;
            set;
        }
        public int QuestionId
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        private NpcTemplateDAO m_template;

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        [DoNotNotify]
        public NpcTemplateDAO Template
        {
            get
            {
                if (m_template == null)
                    m_template = NpcTemplateRepository.Instance.GetTemplate(TemplateId);
                return m_template;
            }
        }
    }
}
