using UnityEditor;
using UnityEngine;

namespace AssemblyBuilder
{
    [CustomEditor(typeof(BaseAssemblyBuilder), true)]
    [CanEditMultipleObjects]
    public class BaseAssemblyBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (GUILayout.Button("Build"))
            {
                foreach (var obj in targets)
                {
                    if (obj is BaseAssemblyBuilder baseAssemblyBuilder)
                        baseAssemblyBuilder.Build();
                }
            }

            if (targets.Length == 1 && target is AssemblyBuilderCollection collection)
            {
                using (new EditorGUI.DisabledScope(disabled: true))
                {
                    var count = collection.CountBuilders();
                    EditorGUILayout.IntField("Builders Count", count);
                }
            }
        }
    }
}