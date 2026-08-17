using System.IO;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditorInternal;
using UnityEngine;

namespace AssemblyBuilder
{
    public static class AssemblyBuilderCreation
    {
        private const string BasePath = "Assets/Create/Scripting/AssemblyBuilder/";
        
        [MenuItem(BasePath + nameof(AssemblyBuilder) + " from AssemblyDefinition #%F11", false, 100)]
        private static void CreateAssemblyBuilderFromAssemblyDefinition()
        {
            var targets = Selection.objects;

            if (targets.Length == 0)
            {
                Debug.LogWarning("A AssemblyDefinition file must first be selected in order to create a AssemblyBuilder");
                return;
            }

            foreach (var target in targets)
            {
                if (!target) continue;

                // Make sure the selection is a AssemblyDefinition file
                if (target.GetType() != typeof(AssemblyDefinitionAsset))
                {
                    Debug.LogWarning("Selected Object [" + target.name + "] is not a AssemblyDefinition file. " +
                                     "A AssemblyDefinition file must be selected in order to create a AssemblyBuilder", target);
                    continue;
                }

                CreateAssemblyBuilderFromAssemblyDefinition(target);
            }
        }
        
        internal static void CreateAssemblyBuilderFromAssemblyDefinition(Object target)
        {
            var sourceAsmdefFilePath = AssetDatabase.GetAssetPath(target);
            // GetDirectoryName throws on empty path and returns '\' separators on Windows,
            // but AssetDatabase works only with '/'
            var folderPath = string.IsNullOrEmpty(sourceAsmdefFilePath)
                ? null
                : Path.GetDirectoryName(sourceAsmdefFilePath)?.Replace('\\', '/');
            var assetName = Path.GetFileNameWithoutExtension(sourceAsmdefFilePath);

            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogWarning("Can't resolve asset path of selected AssemblyDefinition, " +
                                 "AssemblyBuilder is not created", target);
                return;
            }

            var newAssetFilePathWithName = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + assetName + ".asset");

            var newBuilder = ScriptableObject.CreateInstance<AssemblyBuilder>();
            newBuilder._definitions.Add((AssemblyDefinitionAsset)target);

            AssetDatabase.CreateAsset(newBuilder, newAssetFilePathWithName);
        }
    }
}