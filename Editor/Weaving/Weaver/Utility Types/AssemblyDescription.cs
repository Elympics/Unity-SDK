using System;
using System.Reflection;

namespace Elympics.Weaver
{
    public class AssemblyDescription
    {
        public string Name { get; }
        public string Location { get; }

        public AssemblyDescription(string assemblyPath)
        {
            Name = AssemblyName.GetAssemblyName(assemblyPath).Name;
            Location = FileUtility.Normalize(new Uri(assemblyPath).LocalPath);
        }
    }
}
