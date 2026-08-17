## Assembly Builder

**What is it?** Unity Package, which allows to create parallel hierarchy of `.asmdef` 
compilation files. Through this package, you can setup huge amount of `.asmdef` files.
This package is UnityEditor only

**Why?** With HUGE amount of code, it's necessary to decompose all of this code to 
separate systems. And one of the best way to do this - with `.asmdef` files. In this way, you can 
architect your project with more flexibility and incapsulation of logic. But default interface for `.asmdef`
files is not great for that. This package solve this problem

I have several reasons, why you should use it, instead of default `.asmdef`
- This is overhead for `.asmdef`, you still works with `.asmdef`, but with improvements
- It automates manual linking of many `.asmdef` files. On default `.asmdef`, with every
new project `.asmdef`, you must think to link it manually. With this solution - you 
just need to create `AssemblyBuilder` and add links to other `AssemblyBuilder`

**How to install it?**
- Via UPM (git url) - `https://github.com/vertoker/assemblybuilder.git?path=/Assets/AssemblyBuilder`
- Via `manifest.json` - add new line `"com.vertoker.assemblybuilder": "https://github.com/vertoker/assemblybuilder.git?path=/Assets/AssemblyBuilder"`
- Via `git clone` (if you want to contribute here) - `git clone https://github.com/vertoker/assemblybuilder.git`
- Via `.zip` archive - just download package manually, extract and move to your project into `Assets/Plugins` concrete folder `Assets/AssemblyBuilder`

**How to work with it?**
1. Create new / Select existed `.asmdef` file
2. Create new `AssemblyBuilder` asset (`Scripting/AssemblyBuilder/...`)
3. Add selected `.asmdef` file into new `AssemblyBuilder`
4. Add `parents` into this `AssemblyBuilder`
5. (optional) Setup `inherit mode` of this `AssemblyBuilder`
6. (optional) Add into overall `AssemblyBuilderCollection` asset
7. Click button `Build`

Steps 2-3 can be done in one action: select `.asmdef` files and use
`Create/Scripting/AssemblyBuilder/AssemblyBuilder from AssemblyDefinition` (`Shift+Ctrl+F11`).
It creates `AssemblyBuilder` near every selected file, with `.asmdef` file already added

Step 7 can also be done from main menu, without selecting anything in project:
- `Tools/AssemblyBuilder/Build All` finds every builder in project,
detects roots of their hierarchy and builds them. Every builder is built exactly once,
so it doesn't matter, how many collections contain it
- `Tools/AssemblyBuilder/Build Selected` builds only builders selected in project window,
same as `Build` button of inspector

Root is a builder, which no `AssemblyBuilderCollection` contains, building it covers
whole it's branch. `parents` are not a part of build hierarchy, they only give references,
so a parent is still built as it's own root

**What you should know**
- Use `readonly` option for builder, which `.asmdef` files you don't want to change. 
It's usually files from `upm` and other _external_ packages in project itself
- Use `public parents` and `private parents` to create incapsulation 
in your inheritance tree. Even `.asmdef` files must have dependencies, 
which other `.asmdef` files should doesn't know about
- Use `public parents` by default
- Use `inherit mode` to control depth of inheritance:
`DeepInherit` (default) collects every parent through whole hierarchy,
`Inherit` collects only nearest parents, without parents of parents,
`NoInherit` collects nothing and clears `references` field
- `inherit mode` of builder defines only it's own `references` field. `inherit mode`
of it's parents doesn't affect this builder, so you can safely change it in any asset
- Cyclic `parents` are not allowed, `.asmdef` files can't reference each other in a circle.
Builder detects it, writes error into console and stops inheritance on this branch,
but hierarchy still must be fixed manually
- `AssemblyBuilder` allows to add several `.asmdef` files, but recommendation:
use unique `AssemblyBuilder` for every `.asmdef` file
- `AssemblyBuilder` also can be used without `.asmdef` file, in inheritance
recursion, it uses as group of other `AssemblyBuilder`'s, 
just add it into `public parents`
- `AssemblyBuilderCollection` can be added into `public parents` and `private parents` too.
There it works as transparent group: it stands for builders inside it, not for a layer
of inheritance, so it gives the same result as adding these builders one by one.
Nested collections are unwrapped the same way
- Use several `AssemblyBuilderCollection` assets, to collect all `AssemblyBuilder` assets
in your project. This allows to setup all dependencies with single `Build` click.
Collections can contain other collections, and one `AssemblyBuilder` can be added
into several of them, it's built only once anyway. `Builders Count` field
shows amount of unique builders inside collection
- At the current moment, it changes only `references` field in `.asmdef` files, therefore
everything else must be setup manually (but it can be changed)

This package is in development, I open to any MR that you sent to project
