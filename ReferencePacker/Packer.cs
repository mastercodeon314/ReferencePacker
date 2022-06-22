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
using SevenZip;
namespace ReferencePacker
{
    public class Packer
    {
        public ModuleDef Module { get; set; }
        private ModuleDef SelfModule { get; }

        private string asmPath = "";

        public bool EnableCompression { get; set; } = false;

        private bool sevenSharpReferenced = false;

        private string sevenSharpRefPath = "";


        ConsoleColor oldColor;
        ConsoleColor compressingColor = ConsoleColor.Green;
        ConsoleColor noCompressingColor = ConsoleColor.DarkRed;

        public Packer(string assemblyToPack)
        {
            oldColor = Console.ForegroundColor;

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

            TypeDef referencePackerAttribute = null;
            foreach (TypeDef type in this.SelfModule.Types)
            {
                if (type.Name == "ReferencePackerAttribute")
                {
                    referencePackerAttribute = type;
                    break;
                }
            }

            TypeDef injectedReferencePackerAttribute = null;
            if (referencePackerAttribute != null)
            {
                //Console.WriteLine("Injecting assembly resolver into: " + Path.GetFileName(this.Module.Location));
                // Injects ModuleInit from this module into the target module
                injectedReferencePackerAttribute = InjectHelper.Inject(referencePackerAttribute, this.Module);
                this.Module.Types.Add(injectedReferencePackerAttribute);
            }

            if (!this.EnableCompression)
            {
                Console.Write("Compression: ");
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("DISABLED");
                Console.ForegroundColor = oldColor;
                Console.WriteLine();

                Console.WriteLine("Step 1: Inject assembly resolver into <Module>");
                CreateResourceLoader();

                Console.WriteLine("Step 2: Embed referenced assemblies");
                CreateResources();
            }
            else
            {
                Console.Write("Compression: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ENABLED");
                Console.ForegroundColor = oldColor;
                Console.WriteLine();

                Console.WriteLine("Step 1: Embed referenced assemblies");
                CreateResources();

                Console.WriteLine("Step 2: Inject assembly resolver into <Module>");
                CreateResourceLoader();
            }
        }

        public void Save()
        {
            string origFile = Module.Location;
            string path = Path.GetDirectoryName(Module.Location);
            string fileName = Path.GetFileNameWithoutExtension(Module.Location);
            string ext = Path.GetExtension(Module.Location);

            TypeRef att = Module.CorLibTypes.GetTypeRef("System", "Attribute");

            TypeDef attrRef = Module.Find("ReferencePackerAttribute", false);
            var ctorRef = new MemberRefUser(Module, ".ctor", MethodSig.CreateInstance(Module.CorLibTypes.Void, Module.CorLibTypes.String), attrRef);

            var attr = new CustomAttribute(ctorRef);
            attr.ConstructorArguments.Add(new CAArgument(Module.CorLibTypes.String, "Packed by Reference Packer"));
            Module.Assembly.CustomAttributes.Add(attr);

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
                        if (this.EnableCompression)
                        {
                            // Find the module init class with the decompression code
                            if (type.Name == "ModuleInitCompresed")
                            {
                                moduleInitDef = type;
                                break;
                            }
                        }
                        else
                        {
                            // Find the module init class with no decompression code
                            if (type.Name == "ModuleInit")
                            {
                                moduleInitDef = type;
                                break;
                            }
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

        private List<string> CreateResources()
        {
            List<string> dllRefs = getReferencedAndOtherDlls();

            Console.WriteLine("Embedding " + dllRefs.Count.ToString() + " reference dlls into " + Path.GetFileName(this.Module.Location));
            Console.WriteLine();

            foreach (string dll in dllRefs)
            {
                if (this.EnableCompression)
                {
                    if (this.sevenSharpReferenced)
                    {
                        if (this.sevenSharpRefPath != string.Empty)
                        {
                            if (dll == this.sevenSharpRefPath)
                            {
                                // Dont compress this reference because its SevenSharp Dll.
                                embeddReference(dll, false);
                            }
                            else
                            {
                                // Compress reference normally
                                embeddReference(dll, true);
                            }
                        }
                    }
                    else
                    {
                        // Compress reference normally
                        embeddReference(dll, true);
                    }
                }
                else
                {
                    embeddReference(dll, false);
                }

                File.Delete(dll);
            }

            if (this.EnableCompression && !this.sevenSharpReferenced && this.sevenSharpRefPath == String.Empty)
            {
                embeddSevenZipSharp();
            }

            Console.WriteLine("Finished embedding reference assemblies!");
            Console.WriteLine();

            return dllRefs;
        }

        private void consoleWriteRefName(string dllName, bool compress)
        {
            if (compress)
            {
                Console.ForegroundColor = compressingColor;
                Console.Write(dllName);
                Console.ForegroundColor = oldColor;
            }
            else
            {
                Console.ForegroundColor = noCompressingColor;
                Console.Write(dllName);
                Console.ForegroundColor = oldColor;
            }
        }

        private void embeddSevenZipSharp()
        {
            string dllName = Path.GetFileName("SevenZipSharp.dll");

            if (this.EnableCompression)
            {
                Console.Write("                Adding: ");
                consoleWriteRefName(dllName, false);
                Console.WriteLine(" as embedded resource into target assembly");
            }
            else
            {
                Console.Write("Adding: ");
                consoleWriteRefName(dllName, false);
                Console.WriteLine(" as embedded resource into target assembly");
            }

            this.Module.Resources.Add(new EmbeddedResource(dllName, Properties.Resources.SevenZipSharp));
        }

        private void embeddReference(string dll, bool compress)
        {
            if (compress)
            {
                string dllName = Path.GetFileName(dll);

                Console.Write("Compressing and Adding: ");

                consoleWriteRefName(dllName, compress);

                Console.WriteLine(" as embedded resource into target assembly");

                this.Module.Resources.Add(new EmbeddedResource(dllName, compressFile(dll)));
            }
            else
            {
                string dllName = Path.GetFileName(dll);

                if (this.EnableCompression)
                {
                    Console.Write("                Adding: ");
                    consoleWriteRefName(dllName, compress);
                    Console.WriteLine(" as embedded resource into target assembly");
                }
                else
                {
                    Console.Write("Adding: ");
                    consoleWriteRefName(dllName, compress);
                    Console.WriteLine(" as embedded resource into target assembly");
                }

                this.Module.Resources.Add(new EmbeddedResource(dllName, File.ReadAllBytes(dll)));
            }
        }

        private byte[] compressFile(string filePath)
        {
            MemoryStream memStr = new MemoryStream(File.ReadAllBytes(filePath));
            MemoryStream resStr = new MemoryStream();
            memStr.Position = 0;

            SevenZipCompressor comp = new SevenZipCompressor();
            comp.FastCompression = true;
            comp.PreserveDirectoryRoot = false;
            comp.DirectoryStructure = false;
            comp.CompressionMethod = CompressionMethod.Lzma2;
            comp.CompressionLevel = CompressionLevel.Ultra;
            comp.CompressionMode = CompressionMode.Create;

            comp.CompressStream(memStr, resStr);

            byte[] res = resStr.ToArray();
            memStr.Dispose();
            resStr.Dispose();
            return res;
        }

        private List<string> getReferencedAndOtherDlls()
        {
            List<string> result = getReferencedDlls(this.Module);

            List<string> otherDlls = getOtherDlls(Path.GetDirectoryName(this.Module.Location));

            foreach (string dll in result)
            {
                if (this.EnableCompression)
                {
                    checkIfSevenSharp(ModuleDefMD.Load(dll));
                }

                if (otherDlls.Contains(dll))
                {
                    otherDlls.Remove(dll);
                }
            }

            if (this.EnableCompression)
            {
                foreach (string dll in otherDlls)
                {
                    checkIfSevenSharp(ModuleDefMD.Load(dll));
                }
            }

            if (otherDlls.Count > 0)
            {
                result.AddRange(otherDlls.ToArray());
            }

            return result;
        }

        private bool checkIfSevenSharp(ModuleDef mod)
        {
            if (this.EnableCompression)
            {
                if (mod.Assembly.CustomAttributes.Count > 0)
                {
                    int attributesMatched = 0;
                    foreach (CustomAttribute ca in mod.Assembly.CustomAttributes)
                    {
                        switch (ca.AttributeType.Name)
                        {
                            case "AssemblyTitleAttribute":
                            {
                                if (ca.ConstructorArguments.Count == 1)
                                {
                                    string val = ca.ConstructorArguments[0].Value.ToString();

                                    if (val == "SevenZipSharp")
                                    {
                                        attributesMatched++;
                                    }
                                }
                                break;
                            }
                            case "AssemblyDescriptionAttribute":
                            {
                                if (ca.ConstructorArguments.Count == 1)
                                {
                                    string val = ca.ConstructorArguments[0].Value.ToString();
                                    if (val == "7-zip native library wrapper")
                                    {
                                        attributesMatched++;
                                    }
                                }
                                break;
                            }
                            case "AssemblyCompanyAttribute":
                            {
                                if (ca.ConstructorArguments.Count == 1)
                                {
                                    string val = ca.ConstructorArguments[0].Value.ToString();
                                    if (val == "Markovtsev Vadim")
                                    {
                                        attributesMatched++;
                                    }
                                }
                                break;
                            }
                            case "AssemblyProductAttribute":
                            {
                                if (ca.ConstructorArguments.Count == 1)
                                {
                                    string val = ca.ConstructorArguments[0].Value.ToString();
                                    if (val == "SevenZipSharp")
                                    {
                                        attributesMatched++;
                                    }
                                }
                                break;
                            }
                            case "AssemblyCopyrightAttribute":
                            {
                                if (ca.ConstructorArguments.Count == 1)
                                {
                                    string val = ca.ConstructorArguments[0].Value.ToString();
                                    if (val == "Copyright (C) Markovtsev Vadim 2009, 2010, licenced under LGPLv3")
                                    {
                                        attributesMatched++;
                                    }
                                }
                                break;
                            }
                        }
                    }

                    if (attributesMatched == 5)
                    {
                        sevenSharpReferenced = true;
                        sevenSharpRefPath = mod.Location;
                        return true;
                    }
                }
            }

            return false;
        }

        private List<string> getReferencedDlls(ModuleDef module)
        {
            List<string> result = new List<string>();
            string path = Path.GetDirectoryName(module.Location);

            ModuleRef[] moduleRefs = module.GetModuleRefs().ToArray();

            AssemblyRef[] refs = module.GetAssemblyRefs().ToArray();
            foreach (AssemblyRef reff in refs)
            {
                string fullName = reff.FullName;
                AssemblyInfo inf = new AssemblyInfo(fullName);

                if (inf.Name != "mscorlib")
                {
                    string dllLoc = path + @"\" + inf.ResourceName;

                    if (File.Exists(dllLoc))
                    {
                        ModuleDef mod = ModuleDefMD.Load(dllLoc);

                        result.Add(dllLoc);

                        List<string> refRefs = getReferencedDlls(mod);

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
