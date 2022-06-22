using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MessageLib
{
    public class Dummy
    {
        public static void SayHi()
        {
            MessageBox.Show("Hello from MessageLib.dll!", "MessageLib");
        }

        public static void Foo()
        {
            return;
        }
    }
}
