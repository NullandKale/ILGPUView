using ILGPU;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ILGPUView.Utils
{
    // This file should probably be called AssemblyHacks
    // This stuff is messy and will probably break
    public static class AssemblyHelpers
    {
        public static List<MetadataReference> getAsManyAsPossible()
        {
            List<string> locations = getAllCurrentlyLoadedAssembiles().Concat(getAllDllsInSamples()).ToList();

            Dictionary<string, MetadataReference> dedupedReferences = new Dictionary<string, MetadataReference>();

            foreach(string s in locations)
            {
                string filename = s.Substring(s.LastIndexOf("\\"));
                if (!dedupedReferences.ContainsKey(filename))
                {
                    if (TryGetMetadataReference(s, out MetadataReference meta))
                    {
                        dedupedReferences.Add(filename, meta);
                    }
                }

            }

            return dedupedReferences.Values.ToList();
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
                }
            }

            return refs;
        }

        public static List<string> getAllDllsInSamples()
        {
            return Directory.GetFiles(".\\Samples\\", "*.dll", SearchOption.AllDirectories).ToList();
        }
    }
}
