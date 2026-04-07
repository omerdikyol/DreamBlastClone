using System;
using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    [CreateAssetMenu(menuName = "DreamBlastClone/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalogAsset : ScriptableObject
    {
        [SerializeField] private TextAsset[] levelJsonFiles;

        public int LevelCount => levelJsonFiles?.Length ?? 0;

        public TextAsset GetLevelJson(int levelNumber)
        {
            if (levelJsonFiles is null || levelJsonFiles.Length == 0)
            {
                throw new InvalidOperationException("Level catalog does not contain any level JSON files.");
            }

            if (levelNumber < 1 || levelNumber > levelJsonFiles.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(levelNumber), levelNumber, "Level number must be within the catalog range.");
            }

            var levelJson = levelJsonFiles[levelNumber - 1];

            if (levelJson is not null)
            {
                return levelJson;
            }

            throw new InvalidOperationException($"Level catalog entry {levelNumber} is missing its JSON asset reference.");
        }
    }
}
