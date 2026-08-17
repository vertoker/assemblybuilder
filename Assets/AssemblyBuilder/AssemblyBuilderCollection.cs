using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssemblyBuilder
{
    [CreateAssetMenu(menuName = "Scripting/AssemblyBuilder/" + nameof(AssemblyBuilderCollection),
        fileName = nameof(AssemblyBuilderCollection), order = 102)]
    public class AssemblyBuilderCollection : BaseAssemblyBuilder
    {
        [SerializeField] internal List<BaseAssemblyBuilder> _builders = new();

        public override void Build()
        {
            BuildInternal(new HashSet<BaseAssemblyBuilder>());
            AssetDatabase.Refresh();
        }

        internal override void BuildInternal(HashSet<BaseAssemblyBuilder> visited)
        {
            // collection can contain itself (directly or through other collections)
            if (!visited.Add(this)) return;

            foreach (var builder in _builders)
            {
                if (!builder) continue;
                builder.BuildInternal(visited);
            }
        }

        internal override void FlattenInto(List<AssemblyBuilder> result, HashSet<BaseAssemblyBuilder> expanded)
        {
            // collection can contain itself (directly or through other collections)
            if (!expanded.Add(this)) return;

            foreach (var builder in _builders)
            {
                if (!builder) continue;
                builder.FlattenInto(result, expanded);
            }
        }

        public int CountBuilders()
        {
            return CountBuilders(new HashSet<BaseAssemblyBuilder>());
        }

        // counts unique builders, every one of them is built only once
        private int CountBuilders(HashSet<BaseAssemblyBuilder> visited)
        {
            if (!visited.Add(this)) return 0;

            var sum = 0;
            foreach (var builder in _builders)
            {
                if (!builder) continue;

                if (builder is AssemblyBuilderCollection builderCollection)
                    sum += builderCollection.CountBuilders(visited);
                else if (visited.Add(builder)) sum++;
            }
            return sum;
        }
    }
}
