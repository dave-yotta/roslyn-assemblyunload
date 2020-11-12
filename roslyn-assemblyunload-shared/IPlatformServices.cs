namespace roslyn_assemblyunload_lib
{
    public interface IPlatformServices
    {
        IAssemblyLoader CreateAssemblyLoadContext();
    }
}
