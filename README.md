This is a comandline app that takes a file path to a .NET assembly, and the packs all referenced assemblies into the target as embedded resources.
An assembly resolver is also injected into the <Module> instalizer of the target.
  
Inject.cs was taken from: https://github.com/mkaring/ConfuserEx/blob/master/Confuser.Core/Helpers/InjectHelper.cs
