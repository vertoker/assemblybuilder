using System.Collections.Generic;
using UnityEngine;

namespace AssemblyBuilder
{
    public abstract class BaseAssemblyBuilder : ScriptableObject
    {
        public abstract void Build();
        // visited protects from infinite recursion on cyclic builder references
        internal abstract void BuildInternal(HashSet<BaseAssemblyBuilder> visited);
    }
}
