using System.Collections.Generic;
using UnityEngine;

namespace AssemblyBuilder
{
    public abstract class BaseAssemblyBuilder : ScriptableObject
    {
        public abstract void Build();
        /// <summary>
        /// Builds this asset and everything above it: parents of a builder
        /// and builders of a collection, through whole depth.
        /// visited protects from infinite recursion on cyclic builder references
        /// and keeps every builder built exactly once
        /// </summary>
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
