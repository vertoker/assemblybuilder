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
            BuildInternal();
            AssetDatabase.Refresh();
        }
        
        internal override void BuildInternal()
        {
            foreach (var builder in _builders)
            {
                builder.BuildInternal();
            }
        }

        public int CountBuilders()
        {
            var sum = 0;
            foreach (var builder in _builders)
            {
                if (builder is AssemblyBuilderCollection builderCollection)
                    sum += builderCollection.CountBuilders();
                else sum++;
            }
            return sum;
        }
    }
}