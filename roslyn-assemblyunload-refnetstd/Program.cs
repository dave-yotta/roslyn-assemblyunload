using roslyn_assemblyunload_lib;
using System;
using System.Threading.Tasks;

namespace roslyn_assemblyunload
{

    public static class Program
    {
        public static async Task Main()
        {
            Console.WriteLine("Ref NetStd");
            var testClass = new MrTest();
            await testClass.Go(new PlatformServices());
        }
    }
}
