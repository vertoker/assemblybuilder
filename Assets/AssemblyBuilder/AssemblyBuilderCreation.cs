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

            if (targets == null)
            {
                Debug.LogWarning("A AssemblyDefinition file must first be selected in order to create a AssemblyBuilder");
                return;
            }

            foreach (var target in targets)
            {
                // Make sure the selection is a AssemblyDefinition file
                if (!target || target.GetType() != typeof(AssemblyDefinitionAsset))
                {
                    Debug.LogWarning("Selected Object [" + target.name + "] is not a AssemblyDefinition file. " +
                                     "A AssemblyDefinition file must be selected in order to create a AssemblyBuilder", target);
                    continue;
                }

                CreateAssemblyBuilderFromAssemblyDefinition(target);
            }
        }
        
        private static void CreateAssemblyBuilderFromAssemblyDefinition(Object target)
        {
            var assemblyDefinition = (AssemblyDefinitionAsset)target;
            
            var sourceAsmdefFilePath = AssetDatabase.GetAssetPath(target);
            var folderPath = Path.GetDirectoryName(sourceAsmdefFilePath);
            var assetName = Path.GetFileNameWithoutExtension(sourceAsmdefFilePath);
            
            var newAssetFilePathWithName = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + assetName + ".asset");

            var newBuilder = ScriptableObject.CreateInstance<AssemblyBuilder>();
            newBuilder._definitions.Add(assemblyDefinition);

            AssetDatabase.CreateAsset(newBuilder, newAssetFilePathWithName);
        }
    }
}