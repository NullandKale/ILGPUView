using ILGPU.Algorithms;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ILGPUView.Utils
{
    // This file should probably be called AssemblyHacks
    // This stuff is messy and will probably break
    public static class AssemblyHelpers
    {
        private static List<MetadataReference> cachedMetadata;
        public static List<MetadataReference> getAsManyAsPossible()
        {
            if(cachedMetadata == null)
            {
                List<string> locations = getAllCurrentlyLoadedAssembiles().Concat(getAllDllsInSamples()).ToList();

                Dictionary<string, MetadataReference> dedupedReferences = new Dictionary<string, MetadataReference>();

                foreach (string s in locations)
                {
                    if (s == null || s.Length <= 0)
                    {
                        continue;
                    }

                    string filename = s.Substring(s.LastIndexOf("\\"));
                    if (!dedupedReferences.ContainsKey(filename))
                    {
                        if (TryGetMetadataReference(s, out MetadataReference meta))
                        {
                            dedupedReferences.Add(filename, meta);
                        }
                    }

                }

                cachedMetadata = dedupedReferences.Values.ToList();
            }

            return cachedMetadata;
        }

        private static bool TryGetMetadataReference(string s, out MetadataReference r)
        {
            try
            {
                r = MetadataReference.CreateFromFile(s);
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("Failed to load assembly: " + s + "\n" + e.ToString());
                r = null;
                return false;
            }
        }

        public static List<string> getAllCurrentlyLoadedAssembiles()
        {
            List<string> refs = new List<string>();

            Assembly[] assems = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assem in assems)
            {
                if (!assem.IsDynamic && File.Exists(assem.Location))
                {
                    refs.Add(assem.Location);
                    refs.AddRange(searchForMore(assem));
                }
            }
            return refs;
        }

        public static List<string> searchForMore(Assembly root)
        {
            List<string> refs = new List<string>();

            AssemblyName[] assems = root.GetReferencedAssemblies();
            foreach (AssemblyName a in assems)
            {
                if(a.CodeBase != null && a.CodeBase.Length > 0)
                {
                    refs.Add(a.CodeBase);
                }
            }

            return refs;
        }

        public static List<string> getAllDllsInSamples()
        {
            return Directory.GetFiles(".\\Samples\\", "*.dll", SearchOption.AllDirectories).ToList();
        }

        private static HashSet<string> typeNameCache;
        public static HashSet<string> getAllTypes()
        {
            if(typeNameCache == null)
            {
                HashSet<string> list = new HashSet<string>();

                foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type t in ass.GetExportedTypes())
                    {
                        if (!list.Contains(t.Name))
                        {
                            list.Add(t.Name);
                        }
                    }
                }

                typeNameCache = list;
            }

            return typeNameCache;
        }
    }
}
