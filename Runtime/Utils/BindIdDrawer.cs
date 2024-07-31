#if UNITY_EDITOR
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Zenject
{
    [UsedImplicitly]
    public class BindIdDrawer : OdinValueDrawer<BindId>
    {
        static BindId[] _ids;
        static string[] _names;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (_ids is null)
            {
                var ids = new List<BindId> { default };
                ids.AddRange(BindIdDict.Keys);
                _ids = ids.ToArray();

                var names = new List<string> { "None" };
                names.AddRange(BindIdDict.Values);
                _names = names.ToArray();
            }

            ValueEntry.SmartValue = SirenixEditorFields.Dropdown(label, ValueEntry.SmartValue, _ids, _names);
        }
    }
}
#endif