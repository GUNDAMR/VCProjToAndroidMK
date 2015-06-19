using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCToNDK
{
    class Program
    {
        static void Main(string[] args)
        {
            VCXProj proj = new VCXProj();
            proj.Load(args[0]);
            proj.Generate();
        }
    }
}
