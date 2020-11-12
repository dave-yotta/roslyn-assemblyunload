using System;
using System.IO;
using System.Reflection;

namespace roslyn_assemblyunload_lib
{

    public interface IAssemblyLoader : IDisposable
    {
        Assembly LoadFromStream(Stream stream);
    }
}
