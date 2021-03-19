using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILGPUView.Utils
{
    public class ReferenceResolver : MetadataReferenceResolver
    {
        public ReferenceResolver()
        {

        }

        public override bool Equals(object other)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return 100;
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            Console.WriteLine("Need to find reference : " + reference + " @ " + baseFilePath + " " + properties.ToString());
            return new ImmutableArray<PortableExecutableReference>();
        }
    }
}
