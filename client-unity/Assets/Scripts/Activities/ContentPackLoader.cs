using System.Linq;
using UnityEngine;

namespace LinguaStars.Client.Activities
{
    public static class ContentPackLoader
    {
        public const string DefaultResourcePath = "Content/demo_content";

        public static ContentPack LoadDefault()
        {
            TextAsset asset = Resources.Load<TextAsset>(DefaultResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"Content pack not found at Resources/{DefaultResourcePath}.");
                return new ContentPack { packId = "empty-pack" };
            }

            ContentPack pack = JsonUtility.FromJson<ContentPack>(asset.text);
            if (pack == null)
            {
                pack = new ContentPack { packId = "empty-pack" };
            }

            return pack;
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
