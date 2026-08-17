using System.Collections.Generic;
using UnityEngine;

namespace AssemblyBuilder
{
    public abstract class BaseAssemblyBuilder : ScriptableObject
    {
        public abstract void Build();
        // visited protects from infinite recursion on cyclic builder references
        internal abstract void BuildInternal(HashSet<BaseAssemblyBuilder> visited);

        /// <summary>
        /// Unwraps builder into plain parents of <see cref="AssemblyBuilder"/>.
        /// Collection inside parents is transparent: it stands for builders inside it,
        /// not for a layer of inheritance.
        /// expanded protects from infinite recursion on cyclic collections
        /// </summary>
        internal abstract void FlattenInto(List<AssemblyBuilder> result, HashSet<BaseAssemblyBuilder> expanded);
    }
}
