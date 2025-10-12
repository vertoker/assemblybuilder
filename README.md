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
- Via UPM (git url) - `https://github.com/vertoker/assemblybuilder.git`
- Via `manifest.json` - add new line `"com.vertoker.assemblybuilder": "https://github.com/vertoker/assemblybuilder.git"`
- Via `git submodule` (if you want to contribute here) - `git submodule add https://github.com/vertoker/assemblybuilder.git Assets/Plugins/AssemblyBuilder`
- Via `.zip` archive - just download package manually, extract and move to your project into `Assets/Plugins`

**How to work with it?**
1. Create new / Select existed `.asmdef` file
2. Create new `AssemblyBuilder` asset (`Scripting/AssemblyBuilder/...`)
3. Add selected `.asmdef` file into new `AssemblyBuilder`
4. Add `parents` into this `AssemblyBuilder`
5. (optional) Add into overall `AssemblyBuilderCollection` asset
6. Click button `Build`

**What you should know**
- Use `readonly` option for builder, which `.asmdef` files you don't want to change. 
It's usually files from `upm` and other _external_ packages in project itself
- Use `public parents` and `private parents` to create incapsulation 
in your inheritance tree. Even `.asmdef` files must have dependencies, 
which other `.asmdef` files should doesn't know about
- Use `public parents` by default
- `AssemblyBuilder` allows to add several `.asmdef` files, but recommendation:
use unique `AssemblyBuilder` for every `.asmdef` file
- `AssemblyBuilder` also can be used without `.asmdef` file, in inheritance
recursion, it uses as group of other `AssemblyBuilder`'s, 
just add it into `public parents`
- Use several `AssemblyBuilderCollection` assets, to collect all `AssemblyBuilder` assets
in your project. This allows to setup all dependencies with single `Build` click
- At the current moment, it changes only `references` field in `.asmdef` files, therefore
everything else must be setup manually (but it can be changed)

This package is in development, I open to any MR that you sent to project
