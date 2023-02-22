using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    /// <summary>
    /// Stage is a parent for instantiating objects.
    /// Stage is always inactivated, so the instantiated object's Awake or Start method is called after the injection.
    /// </summary>
    static class Stage
    {
        static RectTransform _stage;

        public static RectTransform Get()
        {
            if (ReferenceEquals(_stage, null) == false)
            {
                // XXX: Stage GameObject must not be destroyed.
                Assert.IsNotNull(_stage);
                return _stage;
            }

            var go = new GameObject("Stage", typeof(RectTransform)) {hideFlags = HideFlags.HideAndDontSave};
            Object.DontDestroyOnLoad(go);
            go.SetActive(false);

            _stage = (RectTransform) go.transform;
            _stage.sizeDelta = new Vector2(1024, 1024);
            return _stage;
        }

#if UNITY_EDITOR
        static Stage()
        {
            UnityEditor.EditorApplication.playModeStateChanged += _ => _stage = null;
        }
#endif
    }
}