using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssemblyBuilder.Model;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Serialization;

namespace AssemblyBuilder
{
    [CreateAssetMenu(menuName = "Scripting/AssemblyBuilder/" + nameof(AssemblyBuilder), 
        fileName = nameof(AssemblyBuilder), order = 101)]
    public class AssemblyBuilder : BaseAssemblyBuilder
    {
        [SerializeField] internal bool _readonly = false;
        
        [SerializeField] internal AssemblyInheritMode _inheritMode = AssemblyInheritMode.DeepInherit;
        
        [FormerlySerializedAs("_parents")]
        [SerializeField] internal List<AssemblyBuilder> _publicParents = new();
        [SerializeField] internal List<AssemblyBuilder> _privateParents = new();
        
        [SerializeField] internal List<AssemblyDefinitionAsset> _definitions = new();
        
        public override void Build()
        {
            BuildInternal();
            AssetDatabase.Refresh();
        }
        
        internal override void BuildInternal()
        {
            if (_readonly) return;
            
            foreach (var definitionAsset in _definitions)
            {
                var definitionModel = JsonUtility.FromJson<AssemblyDefinitionModel>(definitionAsset.text);
                
                BuildReferences(definitionModel);
                
                var definitionText = JsonUtility.ToJson(definitionModel, true);
                File.WriteAllText(AssetDatabase.GetAssetPath(definitionAsset), definitionText);
            }
        }

        private void BuildReferences(AssemblyDefinitionModel definitionModel)
        {
            var references = new HashSet<string>();
            var inherit = _inheritMode != AssemblyInheritMode.NoInherit;
            CollectReferences(references, inherit);
            definitionModel.references = references.ToList();
        }

        private void CollectReferences(HashSet<string> references, bool inherit, bool first = true)
        {
            // if NoInherit - return on first call (no parents)
            // if Inherit - return on second call (top layer parents)
            // if DeepInherit - never return, stop when every parent ends
            if (!inherit) return;
            
            // current inherit apply effect only on next recursion call
            var nextInherit = _inheritMode == AssemblyInheritMode.DeepInherit;
            
            // public parents, using everytime until recursion ends
            CollectReferences(_publicParents, references, nextInherit);

            if (!first) return;
            // private parents, using only once, on first recursion call
            CollectReferences(_privateParents, references, nextInherit);
        }

        private void CollectReferences(IReadOnlyList<AssemblyBuilder> parents, HashSet<string> references, bool nextInherit)
        {
            // collect is pre-recursion: begins with stack build, ends with collection  
            // order: parents first, children in end
            foreach (var parent in parents)
                parent.CollectReferences(references, nextInherit, false);

            foreach (var parent in parents)
            {
                foreach (var definitionAsset in parent._definitions)
                {
                    var assetPath = AssetDatabase.GetAssetPath(definitionAsset);
                    var guid = AssetDatabase.AssetPathToGUID(assetPath);
                    guid = $"GUID:{guid}";
                    references.Add(guid);
                }
            }
        }
    }
}