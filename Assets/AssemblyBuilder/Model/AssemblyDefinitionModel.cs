using System.Collections.Generic;

namespace AssemblyBuilder.Model
{
    /// <summary>
    /// Approximate model for AssemblyDefinition file.
    /// Unity doesn't have public model for this files
    /// </summary>
    public class AssemblyDefinitionModel
    {
        public string name;
        public string rootNamespace;
        // AssemblyBuilder updates only references, everything else he saves
        public List<string> references;
        public List<string> includePlatforms;
        public List<string> excludePlatforms;
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public List<string> precompiledReferences;
        public bool autoReferenced;
        public List<string> defineConstraints;
        public List<VersionDefine> versionDefines;
        public bool noEngineReferences;
    }
}