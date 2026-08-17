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
        [SerializeField] internal bool _readonly;

        [SerializeField] internal AssemblyInheritMode _inheritMode = AssemblyInheritMode.DeepInherit;

        [FormerlySerializedAs("_parents")]
        [SerializeField] internal List<BaseAssemblyBuilder> _publicParents = new();
        [SerializeField] internal List<BaseAssemblyBuilder> _privateParents = new();

        [SerializeField] internal List<AssemblyDefinitionAsset> _definitions = new();

        public IReadOnlyList<BaseAssemblyBuilder> PublicParents => _publicParents;
        public IReadOnlyList<BaseAssemblyBuilder> PrivateParents => _privateParents;

        public IReadOnlyList<AssemblyDefinitionAsset> Definitions => _definitions;

        public override void Build()
        {
            BuildInternal(new HashSet<BaseAssemblyBuilder>());
            AssetDatabase.Refresh();
        }

        internal override void BuildInternal(HashSet<BaseAssemblyBuilder> visited)
        {
            if (_readonly) return;
            // same builder can be reached from several collections, build it only once
            if (!visited.Add(this)) return;

            foreach (var definitionAsset in _definitions)
            {
                if (!definitionAsset) continue;

                var definitionModel = JsonUtility.FromJson<AssemblyDefinitionModel>(definitionAsset.text);

                BuildReferences(definitionModel);

                var definitionText = JsonUtility.ToJson(definitionModel, true);
                File.WriteAllText(AssetDatabase.GetAssetPath(definitionAsset), definitionText);
            }
        }

        private void BuildReferences(AssemblyDefinitionModel definitionModel)
        {
            var references = new HashSet<string>();
            // visited - every builder is collected once, path - current recursion branch
            var visited = new HashSet<AssemblyBuilder> { this };
            var path = new HashSet<AssemblyBuilder> { this };

            // NoInherit - no parents at all
            // Inherit - only top layer of parents
            // DeepInherit - every parent, until recursion ends
            // inherit mode of this builder defines whole traversal,
            // inherit modes of it's parents don't affect it
            if (_inheritMode != AssemblyInheritMode.NoInherit)
                CollectReferences(references, visited, path, _inheritMode == AssemblyInheritMode.DeepInherit);

            // cyclic parents can add this builder into it's own references
            foreach (var definitionAsset in _definitions)
            {
                if (!definitionAsset) continue;
                references.Remove(GetReferenceGuid(definitionAsset));
            }

            definitionModel.references = references.ToList();
        }

        private void CollectReferences(HashSet<string> references, HashSet<AssemblyBuilder> visited,
            HashSet<AssemblyBuilder> path, bool deep, bool first = true)
        {
            // public parents, using everytime until recursion ends
            CollectReferences(_publicParents, references, visited, path, deep);

            if (!first) return;
            // private parents, using only once, on first recursion call
            CollectReferences(_privateParents, references, visited, path, deep);
        }

        internal override void FlattenInto(List<AssemblyBuilder> result, HashSet<BaseAssemblyBuilder> expanded)
        {
            result.Add(this);
        }

        /// <summary>
        /// Collections inside parents are unwrapped into builders they contain,
        /// so traversal below works with plain builders only.
        /// expanded is local to one list: same collection reached through another branch
        /// is unwrapped again, exactly like a builder listed in two places
        /// </summary>
        private static List<AssemblyBuilder> Flatten(IReadOnlyList<BaseAssemblyBuilder> source)
        {
            var parents = new List<AssemblyBuilder>(source.Count);
            var expanded = new HashSet<BaseAssemblyBuilder>();

            foreach (var parent in source)
            {
                if (!parent) continue;
                parent.FlattenInto(parents, expanded);
            }

            return parents;
        }

        private void CollectReferences(IReadOnlyList<BaseAssemblyBuilder> source, HashSet<string> references,
            HashSet<AssemblyBuilder> visited, HashSet<AssemblyBuilder> path, bool deep)
        {
            var parents = Flatten(source);

            // collect is pre-recursion: begins with stack build, ends with collection
            // order: parents first, children in end
            // without deep it collects only definitions of this layer, without recursion
            if (deep)
            {
                foreach (var parent in parents)
                {
                    if (path.Contains(parent))
                    {
                        Debug.LogError($"Cyclic parent reference [{name}] -> [{parent.name}] detected, " +
                                       "inheritance stopped on this branch", this);
                        continue;
                    }

                    // parent is already collected through another branch, no need to walk it twice
                    if (!visited.Add(parent)) continue;

                    path.Add(parent);
                    parent.CollectReferences(references, visited, path, true, false);
                    path.Remove(parent);
                }
            }

            // parents are already flattened and filtered from nulls
            foreach (var parent in parents)
            {
                foreach (var definitionAsset in parent._definitions)
                {
                    if (!definitionAsset) continue;

                    var guid = GetReferenceGuid(definitionAsset);
                    if (guid == null)
                    {
                        Debug.LogWarning($"Can't resolve GUID of AssemblyDefinition in [{parent.name}], " +
                                         "reference skipped", parent);
                        continue;
                    }

                    references.Add(guid);
                }
            }
        }

        private static string GetReferenceGuid(AssemblyDefinitionAsset definitionAsset)
        {
            var assetPath = AssetDatabase.GetAssetPath(definitionAsset);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            return string.IsNullOrEmpty(guid) ? null : $"GUID:{guid}";
        }
    }
}
