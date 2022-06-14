using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReferencePacker
{
    public class AssemblyInfo
    {
        public string Name { get; set; }

        public string ResourceName
        {
            get
            {
                return this.Name + ".dll";
            }
        }

        public string Version { get; set; }
        public string Culture { get; set; }
        public string PublicKeyToken { get; set; }

        public AssemblyInfo(string asmData)
        {
            string[] parts = asmData.Split(',');

            if (parts != null)
            {
                if (parts.Length == 4)
                {
                    this.Name = parts[0];
                    this.Version = getRightOfEqual(parts[1]);
                    this.Culture = getRightOfEqual(parts[2]);
                    this.PublicKeyToken = getRightOfEqual(parts[3]);
                }
            }

            foreach (string part in parts)
            {
                string val = part.Trim();

            }
        }

        private string getRightOfEqual(string x)
        {
            if (x.Contains("="))
            {
                string[] pts = x.Split('=');

                if (pts.Length >= 2)
                {
                    return pts[1];
                }
                return "";
            }
            return "";
        }
    }
}
