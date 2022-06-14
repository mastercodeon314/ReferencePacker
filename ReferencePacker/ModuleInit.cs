using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

public static class ModuleInit
{
    public static void Run()
    {
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
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
            MessageBox.Show(ex.ToString());
        }

        Debugger.Break();
        return args.RequestingAssembly;
    }
}
