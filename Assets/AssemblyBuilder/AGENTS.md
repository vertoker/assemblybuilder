# AssemblyBuilder

Unity Editor-only package (`com.vertoker.assemblybuilder`) that builds a parallel hierarchy of
`.asmdef` files. You describe dependencies once, in `AssemblyBuilder` assets, and the package
writes the `references` field of every `.asmdef` for you.

It is an overhead layer over `.asmdef`, not a replacement: the project still compiles from plain
`.asmdef` files, they are just generated from a hierarchy instead of being linked by hand.

## Install

- UPM git url: `https://github.com/vertoker/assemblybuilder.git?path=/Assets/AssemblyBuilder`
- `manifest.json`: `"com.vertoker.assemblybuilder": "https://github.com/vertoker/assemblybuilder.git?path=/Assets/AssemblyBuilder"`
- Minimum Unity: `2021.3`. No package dependencies.

## Assets

Two `ScriptableObject` assets, both derived from `BaseAssemblyBuilder`:

**`AssemblyBuilder`** — one node of the hierarchy. Fields:

| Field | Meaning |
| --- | --- |
| `definitions` | `.asmdef` files this node owns and rewrites. Usually exactly one |
| `public parents` | nodes this one inherits from, and which are passed down to its own children |
| `private parents` | nodes this one inherits from, but which are **not** passed down |
| `inherit mode` | `DeepInherit` (default) / `Inherit` / `NoInherit`, see below |
| `readonly` | node is never written to, but still gives references to others |

**`AssemblyBuilderCollection`** — a plain list of builders (and of other collections). It is a
grouping asset: building it builds everything inside, once. `Builders Count` in the inspector
shows how many unique builders it covers.

## Usage

1. Create or select an `.asmdef` file.
2. Create an `AssemblyBuilder` asset: `Create/Scripting/AssemblyBuilder/AssemblyBuilder`.
3. Put the `.asmdef` into its `definitions`.
4. Fill `public parents` / `private parents` with other `AssemblyBuilder` assets.
5. Optionally set `inherit mode`.
6. Optionally add the builder into an `AssemblyBuilderCollection`.
7. Press `Build`.

Steps 2-3 have a shortcut: select `.asmdef` files and use
`Create/Scripting/AssemblyBuilder/AssemblyBuilder from AssemblyDefinition` (`Shift+Ctrl+F11`).
It creates an `AssemblyBuilder` next to every selected file, with the `.asmdef` already added.

Step 7 has menu entries that need no selection in the inspector:

- `Tools/AssemblyBuilder/Build All` (`Shift+Ctrl+B`) — finds every builder in the project,
  detects the roots of the hierarchy and builds them. Every builder is built exactly once,
  no matter how many collections contain it.
- `Tools/AssemblyBuilder/Build Selected` — builds only what is selected in the project window,
  the same as the `Build` button of the inspector.

Build goes up: building a builder also builds every parent above it, through the whole depth,
so a branch is never left half updated. A root is a builder that no collection contains;
`Build All` starts from roots, and every builder is built exactly once, no matter through how
many paths it is reached.

## Inheritance

`inherit mode` controls how deep a builder looks for references:

- `DeepInherit` (default) — every parent through the whole hierarchy.
- `Inherit` — only the nearest parents, without parents of parents.
- `NoInherit` — nothing, the `references` field is cleared.

The mode of a builder defines only its own `references` field. Modes of its parents do not
affect it, so a mode can be changed in any asset safely.

Public and private parents differ only one level down:

- `public parents` are collected on every level of the recursion;
- `private parents` are collected only for the builder itself, children of that builder
  never see them.

This is how encapsulation is expressed: a dependency that an assembly needs, but that its own
dependents must not reach, goes into `private parents`.

An `AssemblyBuilderCollection` can also be put into `public parents` / `private parents`.
There it is transparent: it stands for the builders inside it, not for a layer of inheritance,
so it gives exactly the same result as adding those builders one by one. Nested collections are
unwrapped the same way.

## Guarantees

- Only the `references` field of an `.asmdef` is changed. Everything else — `name`,
  `rootNamespace`, platforms, `defineConstraints`, `versionDefines`, `allowUnsafeCode` and the
  rest — is read and written back unchanged.
- References are written as `GUID:<guid>`, so renaming an assembly does not break them.
- Build is idempotent: building twice in a row gives byte-identical files.
- No `.asmdef` file is ever created or deleted, only rewritten.
- A builder never references its own `definitions`.
- Cyclic parents are reported as an error in the console and inheritance stops on that branch;
  the editor never hangs. The cycle itself still has to be fixed by hand — `.asmdef` files
  cannot reference each other in a circle.
- Cyclic or self-containing collections do not hang the build either.
- `readonly` builders are never written, but are still a valid source of references, and build
  still goes up through their parents.
- A builder reached from several collections, parents or roots is built exactly once.

## Scripting API

Everything lives in the `AssemblyBuilder` namespace, editor-only assembly `AssemblyBuilder`.

```csharp
public abstract class BaseAssemblyBuilder : ScriptableObject
{
    public abstract void Build();          // build this asset and refresh AssetDatabase
}

public class AssemblyBuilder : BaseAssemblyBuilder
{
    public IReadOnlyList<BaseAssemblyBuilder> PublicParents { get; }
    public IReadOnlyList<BaseAssemblyBuilder> PrivateParents { get; }
    public IReadOnlyList<AssemblyDefinitionAsset> Definitions { get; }
}

public class AssemblyBuilderCollection : BaseAssemblyBuilder
{
    public int CountBuilders();            // unique builders inside, including nested collections
}
```

Serialized fields are `internal`, so a hierarchy is authored through the inspector, not through
code. `Build()` writes files directly and calls `AssetDatabase.Refresh()`, which triggers a
recompilation — do not call it in a loop over many assets, use a collection or `Build All`.

## What not to do

- Do not edit the `references` field of a managed `.asmdef` by hand: the next build overwrites it.
  Express the dependency as a parent instead.
- Do not use this package at runtime. It is editor-only and compiles only for `Editor`.
- Do not point a builder at `.asmdef` files you do not own (UPM packages and other external
  code). Mark such a builder `readonly` and use it as a parent only.
- Do not put several `.asmdef` files into one builder unless they really must share the exact
  same references. One builder per `.asmdef` is the recommended shape.
- Do not use `private parents` by default. Start with `public parents` and make a parent private
  only when dependents must not inherit it.
- Do not expect a build to update the children of the built builder. Build goes up to parents,
  never down to dependents; use `Build All` to rebuild the whole project.
- Do not expect anything but `references` to be managed. Platforms, defines and version defines
  are still set up by hand in each `.asmdef`.
