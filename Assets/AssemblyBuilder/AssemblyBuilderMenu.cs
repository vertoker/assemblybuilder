using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssemblyBuilder
{
    public static class AssemblyBuilderMenu
    {
        private const string BasePath = "Tools/" + nameof(AssemblyBuilder) + "/";
        
        [MenuItem(BasePath + "Build All", false, 100)]
        private static void BuildAll()
        {
            var builders = FindAllBuilders();

            if (builders.Count == 0)
            {
                Debug.LogWarning($"No {nameof(BaseAssemblyBuilder)} assets found in project, nothing to build");
                return;
            }

            var roots = CollectRoots(builders);
            var visited = new HashSet<BaseAssemblyBuilder>();

            foreach (var root in roots)
            {
                root.BuildInternal(visited);
            }

            AssetDatabase.Refresh();

            // readonly builders are skipped by build itself, so visited can be smaller than found
            Debug.Log($"{nameof(AssemblyBuilder)}: built {visited.Count} of {builders.Count} builder(s) " +
                      $"from {roots.Count} root(s)");
        }
        
        [MenuItem(BasePath + "Build Selected", false, 101)]
        private static void BuildSelected()
        {
            var visited = new HashSet<BaseAssemblyBuilder>();
            var selected = 0;

            foreach (var target in Selection.objects)
            {
                if (target is not BaseAssemblyBuilder builder) continue;

                builder.BuildInternal(visited);
                selected++;
            }

            if (selected == 0)
            {
                Debug.LogWarning($"A {nameof(BaseAssemblyBuilder)} asset must first be selected in order to build it");
                return;
            }

            AssetDatabase.Refresh();

            Debug.Log($"{nameof(AssemblyBuilder)}: built {selected} selected builder(s)");
        }

        private static List<BaseAssemblyBuilder> FindAllBuilders()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(BaseAssemblyBuilder)}");
            var builders = new List<BaseAssemblyBuilder>(guids.Length);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                var builder = AssetDatabase.LoadAssetAtPath<BaseAssemblyBuilder>(path);
                if (builder) builders.Add(builder);
            }

            return builders;
        }

        /// <summary>
        /// Root is a builder no collection references, building it covers whole it's branch.
        /// Parents of <see cref="AssemblyBuilder"/> are not a part of build hierarchy,
        /// they only give references, so a parent still needs it's own root
        /// </summary>
        internal static List<BaseAssemblyBuilder> CollectRoots(IReadOnlyList<BaseAssemblyBuilder> builders)
        {
            var referenced = new HashSet<BaseAssemblyBuilder>();

            foreach (var builder in builders)
            {
                if (builder is not AssemblyBuilderCollection collection) continue;

                foreach (var child in collection._builders)
                {
                    // collection referencing itself doesn't stop it from being a root
                    if (child && child != collection) referenced.Add(child);
                }
            }

            var roots = new List<BaseAssemblyBuilder>();
            var reachable = new HashSet<BaseAssemblyBuilder>();

            foreach (var builder in builders)
            {
                if (referenced.Contains(builder)) continue;

                roots.Add(builder);
                MarkReachable(builder, reachable);
            }

            // collections can reference each other cyclically without any entry point from outside,
            // such branch has no root at all and stays unreachable, so it's own root is picked.
            // collection goes first, builders inside of it are covered by it and need no own root
            foreach (var builder in builders)
            {
                if (builder is not AssemblyBuilderCollection) continue;
                if (reachable.Contains(builder)) continue;

                roots.Add(builder);
                MarkReachable(builder, reachable);
            }

            // safety net, every builder must be built, even if it's unreachable in some new way
            foreach (var builder in builders)
            {
                if (reachable.Contains(builder)) continue;

                roots.Add(builder);
                MarkReachable(builder, reachable);
            }

            return roots;
        }

        private static void MarkReachable(BaseAssemblyBuilder builder, HashSet<BaseAssemblyBuilder> reachable)
        {
            // cyclic collections are stopped by reachable set, same as visited set of build
            if (!reachable.Add(builder)) return;

            if (builder is not AssemblyBuilderCollection collection) return;

            foreach (var child in collection._builders)
            {
                if (child) MarkReachable(child, reachable);
            }
        }
    }
}
