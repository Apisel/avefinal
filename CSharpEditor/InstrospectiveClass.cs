using System;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;

namespace CSharpEditor
{
    internal class InstrospectiveClass : MarshalByRefObject
    {

        private Assembly ass;

        public IEnumerable<String> getTypes(String path)
        {
            System.Reflection.Assembly o = System.Reflection.Assembly.Load(path);
            foreach (var typeName in o.GetTypes())
            {
                yield return "Type: " + typeName.Name;
            }
        }

        public void loadAssembly(String exeFilePath)
        {
            ass = Assembly.LoadFrom(exeFilePath);
        }

        private IEnumerable<String> getTypeEvents(Type type, BindingFlags bf)
        {
            foreach (var evt in type.GetEvents(bf))
            {
                yield return "Event: "+ evt.Name;
            }

        }

        private IEnumerable<String> getTypeFields(Type type, BindingFlags bf)
        {

            foreach (var f in type.GetFields(bf))
            {
                yield return "Field: "+f.Name;
            }

        }

        private IEnumerable<String> getTypeMethods(Type type, BindingFlags bf)
        {
            String aux;
            foreach (var methodName in type.GetMethods(bf))
            {
                aux = "Method: "+methodName.ReturnType + " " + methodName.Name + " (";
                foreach (var param in methodName.GetParameters())
                {
                    aux = aux + param.Name;
                    aux = aux + ", ";
                }

                yield return aux + ")";
            }

        }

        private IEnumerable<String> getTypeProperties(Type type, BindingFlags bf)
        {

            String aux;
            foreach (var prop in type.GetProperties(bf))
            {
                aux = "Property: "+prop.Name + " [";
                foreach (var param in prop.GetIndexParameters())
                {
                    aux = aux + param.Name;
                    aux = aux + ", ";
                }

                yield return aux + "]";
            }

        }

        private IEnumerable<String> getTypeNestedTypes(Type type, BindingFlags bf)
        {
            foreach (var nt in type.GetNestedTypes(bf))
            {
                yield return "Nested Type: "+ nt.Name;
            }
        }

        public String[] getMembers(Type type, bool isStatic)
        {
            BindingFlags bf = BindingFlags.Public;
            if (isStatic) bf = bf | BindingFlags.Static;
            else bf = bf | BindingFlags.Instance;

            List<String> membersList = new List<String>();

            membersList.AddRange(getTypeEvents(type, bf));
            membersList.AddRange(getTypeFields(type, bf));
            membersList.AddRange(getTypeMethods(type, bf));
            membersList.AddRange(getTypeProperties(type, bf));
            membersList.AddRange(getTypeNestedTypes(type, bf));

            return membersList.ToArray();

        }

        public bool checkType(String type)
        {
            Type assType = ass.GetType(type);
            if (assType == null)
                assType = Type.GetType(type);
            else
                return true;
            return assType != null;
        }

        public String[] getMembersByString(String type, bool isStatic)
        {
            Type assType = ass.GetType(type);
            return getMembers(assType, isStatic);
        }
    }
}
