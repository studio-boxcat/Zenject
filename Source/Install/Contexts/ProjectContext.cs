using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    public class ProjectContext : MonoBehaviour
    {
        static ProjectContext _instance;

        public static ProjectContext Instance
        {
            get
            {
                if (_instance == null)
                    InstantiateAndInitialize();
                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        public DiContainer Container;

        [InlineProperty, HideLabel]
        public InstallerCollection InstallerCollection;

        static void InstantiateAndInitialize()
        {
            Assert.IsTrue(FindObjectsOfType<ProjectContext>().Length == 0, "Tried to create multiple instances of ProjectContext!");

            var prefab = Resources.Load<GameObject>("ProjectContext");
            prefab.SetActive(false);
            _instance = Instantiate(prefab, null, false).GetComponent<ProjectContext>();
            prefab.SetActive(true);

            // Note: We use Initialize instead of awake here in case someone calls
            // ProjectContext.Instance while ProjectContext is initializing
            _instance.Initialize();

            // We always instantiate it as disabled so that Awake and Start events are triggered after inject
            _instance.gameObject.SetActive(true);
        }

        public void Awake()
        {
            if (Application.isPlaying)
                // DontDestroyOnLoad can only be called when in play mode and otherwise produces errors
                // ProjectContext is created during design time (in an empty scene) when running validation
                // and also when running unit tests
                // In these cases we don't need DontDestroyOnLoad so just skip it
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        void Initialize()
        {
            Assert.IsNull(Container);

            Container = new DiContainer();

            Container.Bind(typeof(MonoKernel),
                arguments: new ArgumentArray(gameObject),
                provider: (container, concreteType, args) => container.InstantiateComponent(concreteType, (GameObject) args.Arg1),
                nonLazy: true);

            InstallerCollection.InjectAndInstall(Container);

            Container.ResolveRoots();
        }
    }
}