using roslyn_assemblyunload_lib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace roslyn_assemblyunload
{

    internal class PlatformServices : IPlatformServices
    {
        public IAssemblyLoader CreateAssemblyLoadContext()
        {
            return new TestAssemblyLoader();
        }

        public class TestAssemblyLoader : AssemblyLoadContext, IAssemblyLoader
        {
            public TestAssemblyLoader() : base(true)
            {
                LoadedAssemblies = new Dictionary<string, Assembly>();

                Resolving += LoadContext_Resolving;
            }

            private Assembly LoadContext_Resolving(AssemblyLoadContext arg1, AssemblyName arg2)
            {
                return LoadedAssemblies[arg2.FullName];
            }

            private Dictionary<string, Assembly> LoadedAssemblies { get; }

            Assembly IAssemblyLoader.LoadFromStream(Stream stream)
            {
                var assembly = LoadFromStream(stream);
                return LoadedAssemblies[assembly.FullName] = assembly;
            }

            protected sealed override Assembly Load(AssemblyName assemblyName)
            {
                return null;
            }

            public void Dispose()
            {
             //   LoadedAssemblies.Clear();
                Unload();
            }
        }
    }
}
