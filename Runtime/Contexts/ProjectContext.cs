using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    [RequireComponent(typeof(Kernel))]
    public class ProjectContext : MonoBehaviour
    {
        static ProjectContext _instance;

        public static ProjectContext Instance
        {
            get
            {
                if (_instance is null)
                    InstantiateAndInitialize();
                return _instance;
            }
        }

        public static bool HasInstance => _instance is not null;

        public readonly DiContainer Container = new(null, 64);

        [SerializeField, InlineProperty, HideLabel]
        InstallerCollection _installers;


        static void InstantiateAndInitialize()
        {
            Assert.IsTrue(
                FindAnyObjectByType<ProjectContext>(FindObjectsInactive.Include) is null,
                "Tried to create multiple instances of ProjectContext!");

            var prefab = Resources.Load<ProjectContext>("ProjectContext");
            _instance = Instantiate(prefab, null, false);
        }

        void Awake()
        {
            Assert.IsTrue(_instance is null);

            DontDestroyOnLoad(gameObject);

            _installers.InjectAndInstall(Container, default);

            GetComponent<Kernel>().RegisterServices(Container);

            Container.ResolveNonLazyProviders();
        }

        void OnDestroy()
        {
            _instance = null;
        }
    }
}