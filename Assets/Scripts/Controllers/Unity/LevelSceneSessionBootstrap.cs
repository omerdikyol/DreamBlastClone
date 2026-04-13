using DreamBlastClone.Controllers;
using DreamBlastClone.Data;
using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class LevelSceneSessionBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelSessionHost sessionHost;
        [SerializeField] private LevelCatalogAsset levelCatalog;

        private readonly CurrentLevelStore currentLevelStore = new CurrentLevelStore();
        private readonly LevelJsonParser levelJsonParser = new LevelJsonParser();
        private readonly LevelSessionFactory levelSessionFactory = new LevelSessionFactory();

        private void Awake()
        {
            InitializeSession();
        }

        public bool InitializeSession()
        {
            var targetSessionHost = sessionHost is not null ? sessionHost : GetComponent<LevelSessionHost>();

            if (targetSessionHost is null || levelCatalog is null || levelCatalog.LevelCount == 0)
            {
                Debug.LogError($"{nameof(LevelSceneSessionBootstrap)} requires a {nameof(LevelSessionHost)} and a populated {nameof(LevelCatalogAsset)}.");
                return false;
            }

            var levelNumber = currentLevelStore.GetPlayableLevel(levelCatalog.LevelCount);
            var levelJson = levelCatalog.GetLevelJson(levelNumber);
            var levelDefinition = levelJsonParser.Parse(levelJson.text);
            targetSessionHost.SetSession(levelSessionFactory.Create(levelDefinition));
            EnsureMoveHintController(targetSessionHost);
            return true;
        }

        private void EnsureMoveHintController(LevelSessionHost targetSessionHost)
        {
            if (targetSessionHost is null || !targetSessionHost.TryGetComponent<BoardInputSessionBridge>(out var inputBridge))
            {
                return;
            }

            var moveHintController = targetSessionHost.GetComponent<MoveHintController>();
            if (moveHintController == null)
            {
                moveHintController = targetSessionHost.gameObject.AddComponent<MoveHintController>();
            }

            moveHintController.Configure(targetSessionHost, inputBridge);
        }
    }
}
