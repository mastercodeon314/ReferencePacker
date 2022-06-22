using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[AttributeUsage(AttributeTargets.Assembly)]
public class ReferencePackerAttribute : Attribute
{
    string someText;
    public ReferencePackerAttribute() : this(string.Empty) { }
    public ReferencePackerAttribute(string txt) { someText = txt; }
}
