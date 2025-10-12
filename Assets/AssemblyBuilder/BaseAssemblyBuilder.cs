using UnityEngine;

namespace AssemblyBuilder
{
    public abstract class BaseAssemblyBuilder : ScriptableObject
    {
        public abstract void Build();
        internal abstract void BuildInternal();
    }
}