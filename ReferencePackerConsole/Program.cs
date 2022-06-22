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
using System.Windows.Forms;
using ReferencePacker;
namespace ReferencePackerConsole
{
    class Program
    {
        static string banner = @"
        █▄▄▄▄ ▄███▄   ▄████  ▄███▄   █▄▄▄▄ ▄███▄      ▄   ▄█▄    ▄███▄       █ ▄▄  ██   ▄█▄    █  █▀ ▄███▄   █▄▄▄▄ 
        █  ▄▀ █▀   ▀  █▀   ▀ █▀   ▀  █  ▄▀ █▀   ▀      █  █▀ ▀▄  █▀   ▀      █   █ █ █  █▀ ▀▄  █▄█   █▀   ▀  █  ▄▀ 
        █▀▀▌  ██▄▄    █▀▀    ██▄▄    █▀▀▌  ██▄▄    ██   █ █   ▀  ██▄▄        █▀▀▀  █▄▄█ █   ▀  █▀▄   ██▄▄    █▀▀▌  
        █  █  █▄   ▄▀ █      █▄   ▄▀ █  █  █▄   ▄▀ █ █  █ █▄  ▄▀ █▄   ▄▀     █     █  █ █▄  ▄▀ █  █  █▄   ▄▀ █  █  
          █   ▀███▀    █     ▀███▀     █   ▀███▀   █  █ █ ▀███▀  ▀███▀        █       █ ▀███▀    █   ▀███▀     █   
         ▀              ▀             ▀            █   ██                      ▀     █          ▀             ▀    
                                                                                    ▀                              
                                                 Mastercodeon 2022 ";
        static void PrintBanner()
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(banner);
            Console.WriteLine();
            Console.ForegroundColor = oldColor;
        }

        static void runPacker(string path, bool  useCompression)
        {
            Packer packer = new Packer(path);
            packer.EnableCompression = useCompression;
            try
            {
                packer.Pack();
                packer.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();
        }

        static void PrintNoFileError()
        {
            Console.WriteLine("Error: no file given as argument");
            Console.ReadLine();
        }

        static void Main(string[] args)
        {
            PrintBanner();

            if (args != null)
            {
                if (args.Length == 1)
                {
                    runPacker(args[0], false);
                }
                else if (args.Length == 2)
                {
                    string useCompression = args[1];

                    if (useCompression == "--compress" ||  useCompression == "-compress" || useCompression == "-c" || useCompression == "--c")
                    {
                        runPacker(args[0], true);
                    }
                }
                else
                {
                    PrintNoFileError();
                }
            }
            else
            {
                PrintNoFileError();
            }
        }
    }
}
