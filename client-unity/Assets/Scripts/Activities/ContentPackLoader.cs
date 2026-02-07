using System.Linq;
using LinguaStars.Client.Content;
using UnityEngine;

namespace LinguaStars.Client.Activities
{
    public static class ContentPackLoader
    {
        public static ContentPack LoadDefault()
        {
            ContentPack pack = ContentAgent.Instance.BuildContentPack();
            return pack ?? new ContentPack { packId = "empty-pack" };
        }

        public static LevelDefinition FindLevel(ContentPack pack, string levelId)
        {
            if (pack == null || pack.levels == null || pack.levels.Count == 0)
            {
                return null;
            }

            return pack.levels.FirstOrDefault(level => level.levelId == levelId) ?? pack.levels[0];
        }
    }
}
