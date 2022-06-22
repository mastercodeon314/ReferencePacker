using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Threading;
//using SevenZip;


public static class ModuleInit
{
    public static void Run()
    {
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

        //MemoryStream uncompressedStream = new MemoryStream(new byte[0]);
        //MemoryStream compressedStream = new MemoryStream(new byte[0]);

        //using (SevenZipExtractor extractor = new SevenZipExtractor(compressedStream))
        //{
        //    extractor.ExtractFile(0, uncompressedStream);
        //}

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
