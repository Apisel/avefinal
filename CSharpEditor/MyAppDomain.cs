using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;

namespace CSharpEditor
{
    class MyAppDomain : MarshalByRefObject
    {
        public IEnumerable<string> getTypes(String path)
        {
           System.Reflection.Assembly o = System.Reflection.Assembly.Load(path);
           foreach (var typeName in o.GetTypes())
           {
               yield return typeName.Name;
           }
        }

        public IEnumerable<String> getTypeEvents(Type type, bool isStatic)
        {

            BindingFlags bf = BindingFlags.Public;
            if (isStatic) bf = bf | BindingFlags.Static;
            else bf = bf | BindingFlags.Instance;

            foreach (var evt in type.GetEvents(bf))
            {
                yield return "Event:"+ evt.Name;
            }
            
        }

        public IEnumerable<String> getTypeFields(Type type, bool isStatic)
        {

            BindingFlags bf = BindingFlags.Public;
            if (isStatic) bf = bf | BindingFlags.Static;
            else bf = bf | BindingFlags.Instance;

            foreach (var f in type.GetFields(bf))
            {
                yield return "Field: "+ f.Name;
            }

        }

        public IEnumerable<string> getTypeMethods(Type type, bool isStatic)
        {

            BindingFlags bf = BindingFlags.Public;
            if (isStatic) bf = bf | BindingFlags.Static;
            else bf = bf | BindingFlags.Instance;

            String aux;
            foreach (var methodName in type.GetMethods(bf))
            {
                aux = "Method: "+ methodName.ReturnType + methodName.Name + " (";
                foreach(var param in methodName.GetParameters())
                {
                    aux = aux + param.Name;
                    aux = aux + ", ";
                }

                yield return aux + ")";
            }

        }

        public IEnumerable<String> getTypeProperties (Type type, bool isStatic)
        {

            BindingFlags bf = BindingFlags.Public;
            if (isStatic) bf = bf | BindingFlags.Static;
            else bf = bf | BindingFlags.Instance;

            String aux;
            foreach (var prop in type.GetProperties(bf))
            {
                aux= "Property: "+ prop.Name + " [";
                foreach (var param in prop.GetIndexParameters())
                {
                    aux = aux + param.Name;
                    aux = aux + ", ";
                }

                yield return aux + "]";
            }

        }

        public IEnumerable<String> getTypeNestedTypes(Type type, bool isSatic)
        {
            
            foreach (var nt in type.GetNestedTypes())
            {
                yield return "Nested Type: " +nt.Name;
            }

        }
        
    }
}
