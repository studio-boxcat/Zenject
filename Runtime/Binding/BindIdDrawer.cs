#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Zenject
{
    [UsedImplicitly]
    class BindIdDrawer : OdinValueDrawer<BindId>
    {
        static int[] _ids;
        static GUIContent[] _names;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (_ids is null)
            {
                var ids = new List<int> { default };
                ids.AddRange(BindIdDict.Keys.Select(x => (int) x));
                _ids = ids.ToArray();

                var names = new List<GUIContent> { new("None") };
                names.AddRange(BindIdDict.Values.Select(x => new GUIContent(x)));
                _names = names.ToArray();
            }

            EditorGUI.BeginChangeCheck();
            var value = (int) ValueEntry.SmartValue;
            var newValue = EditorGUILayout.IntPopup(label, value, _names, _ids);
            if (EditorGUI.EndChangeCheck())
                ValueEntry.SmartValue = (BindId) newValue;
        }
    }
}
#endif