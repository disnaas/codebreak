﻿using Codebreak.Framework.Configuration;
using Codebreak.Framework.Database;
using Codebreak.Service.World.Database.Repository;
using Codebreak.Service.World.Database.Structure;

namespace Codebreak.Service.World.Database
{
    public sealed class WorldDbMgr : DbManager<WorldDbMgr>
    {
        [Configurable("DbConnection")]
		public static string DbConnection = "Server=localhost;Database=codebreak_world;Uid=root;Pwd=;";

        public void Initialize()
        {
            base.AddRepository(ExperienceTemplateRepository.Instance);
            base.AddRepository(SubAreaRepository.Instance);
            base.AddRepository(AreaRepository.Instance);
            base.AddRepository(SuperAreaRepository.Instance);
            base.AddRepository(ItemSetRepository.Instance);
            base.AddRepository(ItemTemplateRepository.Instance);
            base.AddRepository(InventoryItemRepository.Instance);
            base.AddRepository(SpellBookEntryRepository.Instance);
            base.AddRepository(TaxCollectorRepository.Instance);
            base.AddRepository(GuildRepository.Instance);
            base.AddRepository(CharacterWaypointRepository.Instance);
            base.AddRepository(CharacterGuildRepository.Instance);
            base.AddRepository(CharacterAlignmentRepository.Instance);
            base.AddRepository(CharacterRepository.Instance);
            base.AddRepository(SocialRelationRepository.Instance);
            base.AddRepository(BankRepository.Instance);
            base.AddRepository(MapTriggerRepository.Instance);
            base.AddRepository(MapRepository.Instance);
            base.AddRepository(NpcTemplateRepository.Instance);
            base.AddRepository(NpcInstanceRepository.Instance);
            base.AddRepository(NpcQuestionRepository.Instance);
            base.AddRepository(NpcResponseRepository.Instance);
            base.AddRepository(MonsterRepository.Instance);
            base.AddRepository(MonsterGradeRepository.Instance);
            base.AddRepository(AuctionHouseRepository.Instance);
            base.AddRepository(AuctionHouseEntryRepository.Instance);
            base.AddRepository(AuctionHouseAllowedTypeRepository.Instance);
            base.AddRepository(SortsRepository.Instance);

            base.LoadAll(DbConnection);
        }
    }
}
