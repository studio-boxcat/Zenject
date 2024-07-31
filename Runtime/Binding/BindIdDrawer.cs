#if UNITY_EDITOR
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Zenject
{
    [UsedImplicitly]
    class BindIdDrawer : OdinValueDrawer<BindId>
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

            var bindId = ValueEntry.SmartValue;
            if (BindIdDict.Valid(bindId) is false)
                SirenixEditorGUI.ErrorMessageBox("BindId is not valid: " + bindId);
            ValueEntry.SmartValue = SirenixEditorFields.Dropdown(label, bindId, _ids, _names);
        }
    }
}
#endif