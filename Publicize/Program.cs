using dnlib.DotNet;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;

namespace Publicize
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new OptionSet() {};
            var exeName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

            List<string> extra;
            try
            {
                extra = parser.Parse(args);
                if (extra.Count != 1)
                {
                    throw new OptionException("Must provide exactly one assembly path", "positional arg");
                }
            }
            catch (OptionException e)
            {
                Console.WriteLine($"{exeName}: {e.Message}");
                Console.WriteLine($"Try `{exeName} --help' for more information.");
                return;
            }

            RewriteAssembly(extra[0]);
        }

        private static void RewriteAssembly(string assemblyPath)
        {
            ModuleDef assembly = ModuleDefMD.Load(assemblyPath);

            var processed = new Dictionary<string, bool>();
            foreach (TypeDef type in assembly.Types)
            {
                RewriteAssemblyRecurse(type, processed);
            }

            assembly.Write($"{Path.ChangeExtension(assemblyPath, null)}_public.dll");
        }

        private static void RewriteAssemblyRecurse(TypeDef type, Dictionary<string, bool> processed)
        {
            if (processed.ContainsKey(type.FullName))
            {
                return;
            }
            processed.Add(type.FullName, true);

            type.Attributes &= ~TypeAttributes.VisibilityMask;

            if (type.IsNested)
            {
                type.Attributes |= TypeAttributes.NestedPublic;
            }
            else
            {
                type.Attributes |= TypeAttributes.Public;
            }

            foreach (MethodDef method in type.Methods)
            {
                method.Attributes &= ~MethodAttributes.MemberAccessMask;
                method.Attributes |= MethodAttributes.Public;
            }

            foreach (FieldDef field in type.Fields)
            {
                field.Attributes &= ~FieldAttributes.FieldAccessMask;
                field.Attributes |= FieldAttributes.Public;
            }

            foreach (TypeDef nestedType in type.NestedTypes)
            {
                RewriteAssemblyRecurse(nestedType, processed);
            }
        }
    }
}
