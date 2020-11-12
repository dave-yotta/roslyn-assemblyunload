using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace roslyn_assemblyunload_lib
{
    public class MrTest
    {
        public async Task Go(IPlatformServices services)
        {
            int i = 0;

            var rvals = new List<object>();

            while (true)
            {
                if (i % 100 == 0)
                {
                    Console.WriteLine($"{i}: {ListFileHandles()} file descriptors");
                }

                using (var loader = services.CreateAssemblyLoadContext())
                {
                    rvals.Add(await MsRun(loader, i++));
                }
            }
        }
        static List<Assembly> refs = new List<Assembly>
            {
                typeof(object).Assembly,
                typeof(ExpandoObject).Assembly,
                Assembly.Load(new AssemblyName("Microsoft.CSharp")),
                Assembly.Load(new AssemblyName("netstandard")),
                Assembly.Load(new AssemblyName("mscorlib")),
                Assembly.Load(new AssemblyName("System.Runtime")),
                typeof(HostBase).Assembly,
                typeof(Enumerable).Assembly
            };

        private async Task<object> MsRun(IAssemblyLoader loader, int i)
        {
            var name = "test" + i;

            var code = "public class test : roslyn_assemblyunload_lib.HostBase { public int Thing() { return (int)(12.0 * new System.Random().NextDouble()); } }";

            var metadataReferences = refs.Select(x => MetadataReference.CreateFromFile(x.Location))
                                         .Cast<MetadataReference>()
                                         .ToArray();

            var libCompilation = CSharpCompilation.Create(name + "Host", new[] { SyntaxFactory.ParseSyntaxTree(code) },
                metadataReferences, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var libReference = libCompilation.ToMetadataReference();

            var scriptReferences = metadataReferences.Append(libReference).ToList();

            var libStream = EmitToStream(libCompilation);

            var libAssembly = loader.LoadFromStream(libStream);

            var hostType = libAssembly.GetType("test");

            var hostObject = Activator.CreateInstance(hostType);

            var scriptCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var scriptCode = "Thing();";

            var scriptCompilation = CSharpCompilation.CreateScriptCompilation(name + "Script",
                SyntaxFactory.ParseSyntaxTree(scriptCode, new CSharpParseOptions(kind: SourceCodeKind.Script)),
                scriptReferences, scriptCompilationOptions, null, null, hostType);

            var scriptStream = EmitToStream(scriptCompilation);

            var scriptAssembly = loader.LoadFromStream(scriptStream);

            var scriptEntryPoint = scriptCompilation.GetEntryPoint(CancellationToken.None);
            var scriptEntryPointType = scriptAssembly.GetType(scriptEntryPoint.ContainingType.MetadataName);
            var scriptEntryPointMethod = scriptEntryPointType.GetMethod(scriptEntryPoint.Name);

            var scriptDelegate = (Func<object[], Task<object>>)scriptEntryPointMethod.CreateDelegate(typeof(Func<object[], Task<object>>));

            return await scriptDelegate(new object[] { hostObject, null });
        }

        private static Stream EmitToStream(CSharpCompilation compilation)
        {
            var stream = new MemoryStream();

            // emit result into a stream
            var emitResult = compilation.Emit(stream);

            if (!emitResult.Success)
            {
                // if not successful, throw an exception
                var errors =
                    emitResult.Diagnostics
                                .Where(d => d.Severity == DiagnosticSeverity.Error)
                                .Select(d => new
                                {
                                    failedCode = d.Location?.SourceTree?.GetText()?.GetSubText(d.Location.SourceSpan).ToString(),
                                    location = d.Location?.SourceSpan,
                                    error = d.GetMessage(),
                                    errorId = d.Descriptor.Id
                                }).ToList();


                throw new Exception(JObject.FromObject(new { errors }).ToString());
            }

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }


        private static int ListFileHandles()
        {
            var pid = Process.GetCurrentProcess().Id;
            var proc = Process.Start(new ProcessStartInfo { FileName = "lsof", Arguments = "-p " + pid, UseShellExecute = false, RedirectStandardOutput = true });
            var nFileDescriptors = proc.StandardOutput.ReadToEnd().Count(x => x == '\n');
            proc.WaitForExit();
            return nFileDescriptors;
        }
    }
}
