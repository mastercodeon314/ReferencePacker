using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Diagnostics;
using System.IO;
using System.Threading;
using dnlib.DotNet.Writer;

namespace ReferencePacker
{
    public class ReferencePacker
    {
        public ModuleDef Module { get; set; }
        private ModuleDef SelfModule { get; }

        private string asmPath = "";

        public ReferencePacker(string assemblyToPack)
        {
            if (File.Exists(assemblyToPack))
            {
                asmPath = assemblyToPack;

                this.Module = ModuleDefMD.Load(assemblyToPack);
                this.SelfModule = ModuleDefMD.Load(System.Reflection.Assembly.GetExecutingAssembly().Location);


            }
            else
            {
                throw new FileNotFoundException("File not found!", assemblyToPack);
            }
        }

        public void Pack()
        {
            checkIfPacked();

            Console.WriteLine("Step 1: Inject assembly resolver into <Module>");
            CreateResourceLoader();

            Console.WriteLine("Step 2: Embed referenced assemblies");
            CreateResources();
        }

        public void Save()
        {
            string origFile = Module.Location;
            string path = Path.GetDirectoryName(Module.Location);
            string fileName = Path.GetFileNameWithoutExtension(Module.Location);
            string ext = Path.GetExtension(Module.Location);

            string tempFile = path + @"\" + fileName + "_1" + ext;

            Console.WriteLine("Saving changes to: " + fileName + "_1" + ext);
            Module.Write(tempFile);

            Module.Dispose();

            Console.WriteLine("Sleeping for 1 second to ensure dnlib finished writing...");
            Thread.Sleep(1000);

            Console.WriteLine("Deleting original assembly: " + fileName + ext);
            File.Delete(origFile);

            Thread.Sleep(200);
            Console.WriteLine("Renaming: " + fileName + "_1" + ext + " to: " + fileName + ext);
            File.Move(tempFile, origFile);

            Thread.Sleep(200);
            Console.WriteLine("Reference Packer finished saving!");
        }

        private void checkIfPacked()
        {
            if (this.Module.GlobalType != null)
            {
                MethodDef CurrentDomain_AssemblyResolveDef = this.Module.GlobalType.FindMethod("CurrentDomain_AssemblyResolve");

                if (CurrentDomain_AssemblyResolveDef != null)
                {
                    throw new Exception("Error: " + Path.GetFileName(this.Module.Location) + " has already been packed!" + Environment.NewLine + "CurrentDomain_AssemblyResolve exists already");
                }
            }
        }

        private void CreateResourceLoader()
        {
            if (this.Module.GlobalType != null)
            {
                MethodDef cctor = this.Module.GlobalType.FindOrCreateStaticConstructor();

                if (cctor != null)
                {
                    TypeDef moduleInitDef = null;
                    foreach (TypeDef type in this.SelfModule.Types)
                    {
                        if (type.Name == "ModuleInit")
                        {
                            moduleInitDef = type;
                            break;
                        }
                    }

                    TypeDef injectedModuleInt = null;
                    if (moduleInitDef != null)
                    {
                        Console.WriteLine("Injecting assembly resolver into: " + Path.GetFileName(this.Module.Location));
                        // Injects ModuleInit from this module into the target module
                        injectedModuleInt = InjectHelper.Inject(moduleInitDef, this.Module);
                        this.Module.Types.Add(injectedModuleInt);
                    }

                    cctor.Body.Instructions.Clear();

                    if (injectedModuleInt != null)
                    {
                        Console.WriteLine("Moving assembly resolver into the GlobalType (a.k.a <Module>)");
                        // Move injectedModuleInt methods into <Module> GlobalType
                        while (injectedModuleInt.Methods.Count > 0)
                        {
                            injectedModuleInt.Methods[0].DeclaringType = this.Module.GlobalType;
                        }

                        // Remove the ModuleInt type that was injected
                        this.Module.Types.Remove(injectedModuleInt);

                        // Find the "Run" method and copy its instructions to <Module>.cctor
                        MethodDef runDef = this.Module.GlobalType.FindMethod("Run");

                        foreach (Instruction ins in runDef.Body.Instructions)
                        {
                            cctor.Body.Instructions.Add(ins);
                        }

                        //Remove the old "Run" method
                        this.Module.GlobalType.Methods.Remove(runDef);

                        Console.WriteLine("Assembly resolver injection finished!");
                        Console.WriteLine();
                    }
                    else
                    {
                        Debugger.Break();
                    }
                }
            }
        }

        private void CreateResources()
        {
            List<string> dllRefs = getReferencedAndOtherDlls();

            Console.WriteLine("Embedding " + dllRefs.Count.ToString() + " reference dlls into " + Path.GetFileName(this.Module.Location));
            Console.WriteLine();

            foreach (string dll in dllRefs)
            {
                string dllName = Path.GetFileName(dll);

                Console.WriteLine("Adding: " + dllName + " as embedded resource into target assembly");

                this.Module.Resources.Add(new EmbeddedResource(dllName, File.ReadAllBytes(dll)));
            }

            Console.WriteLine("Finished embedding reference assemblies!");
            Console.WriteLine();
        }

        private List<string> getReferencedAndOtherDlls()
        {
            List<string> result = getReferencedDlls(this.Module);

            List<string> otherDlls = getOtherDlls(Path.GetDirectoryName(this.Module.Location));

            foreach (string dll in result)
            {
                if (otherDlls.Contains(dll))
                {
                    otherDlls.Remove(dll);
                }
            }

            if (otherDlls.Count > 0)
            {
                result.AddRange(otherDlls.ToArray());
            }

            return result;
        }

        private List<string> getReferencedDlls(ModuleDef module)
        {
            List<string> result = new List<string>();
            string path = Path.GetDirectoryName(module.Location);

            foreach (AssemblyRef reff in module.GetAssemblyRefs())
            {
                string fullName = reff.FullName;
                AssemblyInfo inf = new AssemblyInfo(fullName);

                if (inf.Name != "mscorlib")
                {
                    string dllLoc = path + @"\" + inf.ResourceName;
                    if (File.Exists(dllLoc))
                    {
                        result.Add(dllLoc);

                        List<string> refRefs = getReferencedDlls(ModuleDefMD.Load(dllLoc));

                        foreach (string dll in refRefs)
                        {
                            if (!result.Contains(dll))
                            {
                                result.Add(dll);
                            }
                        }
                    }
                }
            }
            return result.Distinct().ToList();
        }

        private List<string> getOtherDlls(string path)
        {
            string[] files = Directory.GetFiles(path, "*.dll");

            return new List<string>(files);
        }
    }
}
