using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpEditor
{
    class A
    {
        public static void main() 
        {
            String s = "using System;";
            Console.WriteLine(s.Split()[1]);
        }
    }
}
