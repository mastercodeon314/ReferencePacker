using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SevenZip;


public static class ModuleInitCompresed
{
    public static void Run()
    {
        extractSevenSharp();

        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
    }

    private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
        string currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string sevenSharpPath = currentPath + @"\SevenZipSharp.dll";

        if (File.Exists(sevenSharpPath))
        {
            deleteFileAfterExit(sevenSharpPath);
        }
    }

    private static void deleteFileAfterExit(string path)
    {
        string quote = '"'.ToString();

        Process procDestruct = new Process();

        // Name of the deleter batch file
        string strName = "deleter.bat";

        // path to the batch file that will delete the uninstaller and folder
        string batchPath = Path.GetTempPath() + @"\" + strName;

        StreamWriter swDestruct = new StreamWriter(batchPath);

        swDestruct.WriteLine("cd C:\\");

        swDestruct.WriteLine("attrib \"" + path + "\"" + " -a -s -r -h");
        swDestruct.WriteLine(":Repeat");
        swDestruct.WriteLine("del " + "\"" + path + "\"");
        swDestruct.WriteLine("if exist \"" + path + "\"" +
           " goto Repeat");
        swDestruct.WriteLine("del \"" + "%0" + "\"");
        swDestruct.Close();

        procDestruct.StartInfo.FileName = batchPath;

        procDestruct.StartInfo.CreateNoWindow = true;
        procDestruct.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        procDestruct.StartInfo.UseShellExecute = true;

        procDestruct.Start();
        return;
    }

    private static void extractSevenSharp()
    {
        string currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string sevenSharpExtractPath = currentPath + @"\SevenZipSharp.dll";

        if (!File.Exists(sevenSharpExtractPath))
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream("SevenZipSharp.dll"))
            {
                if (stream != null)
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        byte[] result = reader.ReadBytes((int)stream.Length);
                        File.WriteAllBytes(sevenSharpExtractPath, result);
                    }
                }
                else
                {
                    throw new Exception("Error: Cant find SevenZipSharp.dll embedded resource!");
                }
            }
        }
    }

    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        try
        {
            string dllName = "";
            string[] parts = args.Name.Split(',');

            if (parts != null)
            {
                if (parts.Length == 4)
                {
                    dllName = parts[0] + ".dll";
                }
            }

            if (dllName != string.Empty)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "";
                string[] resNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                foreach (string resName in resNames)
                {
                    if (resName == dllName)
                    {
                        resourceName = resName;
                        break;
                    }
                }


                if (resourceName != "")
                {
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        byte[] result = reader.ReadBytes((int)stream.Length);
                        MemoryStream compressedDll = new MemoryStream(result);
                        MemoryStream decompressedDll = new MemoryStream();
                        SevenZipExtractor ext = new SevenZipExtractor(compressedDll);

                        ext.ExtractFile(0, decompressedDll);

                        result = decompressedDll.ToArray();
                        compressedDll.Dispose();
                        decompressedDll.Dispose();
                        Assembly loadedAsm = Assembly.Load(result);
                        return loadedAsm;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }

        return args.RequestingAssembly;
    }
}
