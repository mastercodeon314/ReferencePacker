using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
//using SevenZip;

/*
 * 
 * Post build event: $(SolutionDir)\bin\Debug\ReferencePackerConsole.exe "$(TargetPath)" "-compress"
 * $(SolutionDir)\bin\AssemblyPadder\Debug\AssemblyPadder.exe "$(TargetPath)"
 * 
 */

namespace ExampleApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //SevenZipCompressor comp = new SevenZip.SevenZipCompressor();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
