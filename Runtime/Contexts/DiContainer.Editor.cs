using System;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

#if UNITY_EDITOR
namespace Zenject
{
    public partial class DiContainer
    {
        [ShowInInspector, LabelText("Bindings"), ListDrawerSettings(IsReadOnly = true)]
        EditorBinding[] _editor_Bindings
        {
            get
            {
                var bindings = new EditorBinding[_bindingCount];
                for (var i = 0; i < _bindingCount; i++)
                    bindings[i] = new EditorBinding(_bindings[i]);
                bindings.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
                return bindings;
            }
        }

        readonly struct EditorBinding
        {
            [ShowInInspector, DisplayAsString, HideLabel]
            public readonly string Key;

            public EditorBinding(Binding binding)
            {
                Key = BindKey.ToString(binding.Key);
            }
        }
    }
}
#endif