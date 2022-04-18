namespace Zenject
{
    public class ProjectKernel : MonoKernel
    {
        public void DestroyEverythingInOrder()
        {
            ForceUnloadAllScenes(true);
            DestroyImmediate(gameObject);
        }

        public void ForceUnloadAllScenes(bool immediate = false)
        {
            // Destroy the scene contexts from bottom to top
            // Since this is the reverse order that they were loaded in
            for (var i = SceneContextRegistry.List.Count - 1; i >= 0; i--)
            {
                var sceneContext = SceneContextRegistry.List[i];

                if (immediate)
                {
                    DestroyImmediate(sceneContext.gameObject);
                }
                else
                {
                    Destroy(sceneContext.gameObject);
                }
            }
        }
    }
}