using Discord.WebSocket;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Custodian.Commands
{
    public class SlashCommandCompile : ISlashCommand
    {
        public string Command { get => "compile"; }

        public string Description { get => "Compiles C# and prints the output."; }

        public List<CommandOption> Options
        {
            get => new List<CommandOption>()
            {
                new CommandOption()
                {
                    Name = "code",
                    Description = "The code to be compiled.",
                    Type = Discord.ApplicationCommandOptionType.String,
                    IsRequired = true,
                    IsAutoComplete = true
                }
            };
        }

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync();

                if (command.Data != null && command.Data.Options != null && command.Data.Options.Count > 0)
                {
                    var cmdOption = command.Data.Options.First(c => c.Name == "code");
                    if (cmdOption != null)
                    {
                        var code = cmdOption.Value as string;
                        var fullyTyped = @"
                        using System;
                        namespace CompileUnit
                        {
                            public class Invoker
                            {
                                public object Run()
                                {
                                    " + code + @"
                                }
                            }
                        }";
                        var cTree = CSharpSyntaxTree.ParseText(fullyTyped);

                        var mscorlib = MetadataReference.CreateFromFile(typeof(System.Object).GetTypeInfo().Assembly.Location);
                        var processlib = MetadataReference.CreateFromFile(typeof(System.Diagnostics.Process).GetTypeInfo().Assembly.Location);
                        var componentlib = MetadataReference.CreateFromFile(typeof(System.ComponentModel.Component).GetTypeInfo().Assembly.Location);
                        var runtimelib = MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll"));
                        var cCompile = CSharpCompilation.Create(
                            Path.GetRandomFileName(), 
                            syntaxTrees: new[] { cTree }, 
                            references: new[] { mscorlib, processlib, componentlib, runtimelib },
                            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                        using(var ms = new MemoryStream())
                        {
                            EmitResult cResult = cCompile.Emit(ms);
                            StringBuilder message = new StringBuilder();

                            if(!cResult.Success)
                            {
                                StringBuilder sr = new StringBuilder();
                                var failures = cResult.Diagnostics.Where(diag => diag.IsWarningAsError || diag.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
                            
                                foreach(var failure in failures)
                                {
                                    sr.AppendLine(failure.Id + ": " + failure.GetMessage());
                                }

                                message.AppendLine("There was an issue with your compile: ");
                                message.AppendLine(sr.ToString());
                            }
                            else
                            {
                                ms.Position = 0;
                                var loadContext = new AssemblyLoadContext("CS", true);
                                Assembly assembly = loadContext.LoadFromStream(ms);
                                //Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                                var type = assembly.GetType("CompileUnit.Invoker");
                                var instance = assembly.CreateInstance("CompileUnit.Invoker");
                                var meth = type.GetMember("Run").First() as MethodInfo;
                                object? result = meth.Invoke(instance, Array.Empty<object>());
                                message.AppendLine("Compiled successfully with output: ");
                                message.AppendLine($"```");
                                message.AppendLine(result.ToString());
                                message.AppendLine("```");
                                loadContext.Unload();
                            }

                            message.AppendLine("Original Code: ");
                            message.AppendLine($"```cs");
                            message.AppendLine(code);
                            message.AppendLine("```");

                            await command.ModifyOriginalResponseAsync(p =>
                            {
                                p.Content = message.ToString();
                            });
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("There was an issue trying to run the compile command: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Stacktrace: ");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
