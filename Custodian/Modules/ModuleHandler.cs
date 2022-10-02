using Custodian.Shared.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Custodian.Modules
{
    public class ModuleHandler
    {
        public string ModulesPath { get; set; }

        private List<Shared.Modules.Module> loadedModules;
        public List<Shared.Modules.Module> Modules { get => loadedModules; }

        public ModuleHandler()
        {
            ModulesPath = ".\\modules";
            loadedModules = new List<Shared.Modules.Module>();
        }

        /// <summary>
        /// Reloads all of the modules.
        /// </summary>
        /// <returns>The amount of libraries reloaded.</returns>
        public async Task<int> ReloadAsync()
        {
            return await LoadAsync();
        }

        /// <summary>
        /// Loads all of the modules from ModuleHandler.ModulesPath.
        /// </summary>
        /// <returns>The amount of libraries loaded.</returns>
        public async Task<int> LoadAsync()
        {
            var libraries = await GetLibrariesAsync();

            if(libraries.Length < 1)
            {
                return 0;
            }

            var validAssemblies = await GetValidAssembliesAsync(libraries);

            if(validAssemblies.Count < 1)
            {
                return 0;
            }

            foreach(var assembly in validAssemblies)
            {
                foreach(var type in assembly.GetTypes())
                {
                    if (type.IsAssignableTo(typeof(Shared.Modules.Module)))
                    {
                        try
                        {
                            var module = Activator.CreateInstance(type) as Shared.Modules.Module;

                            if (module != null)
                            {
                                loadedModules.Add(module);
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
            }

            return loadedModules.Count;
        }

        private async Task<string[]> GetLibrariesAsync()
        {
            return await Task.Run(() => { return Directory.GetFiles(ModulesPath, "*.dll"); });
        }

        private async Task<List<Assembly>> GetValidAssembliesAsync(string[] libraries)
        {
            return await Task.Run(() =>
            {
                var validAssemblies = new List<Assembly>();

                foreach (var lib in libraries)
                {
                    try
                    {
                        var assembly = Assembly.LoadFile(Path.GetFullPath(lib));

                        if (assembly.GetTypes().Any(t => t.IsAssignableTo(typeof(Shared.Modules.Module))))
                        {
                            validAssemblies.Add(assembly);
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }

                return validAssemblies;
            });
        }

        public async Task InjectAsync(ServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetService<ILogger>();

            foreach (var module in this.Modules)
            {
                await logger.LogAsync(LogLevel.INFO, $"Resolving members for '{module.Name}'..");
                var members = module.GetType()
                    .GetMembers(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.IsDefined(typeof(Shared.Modules.ModuleImport)));

                if (members != null && members.Count() > 0)
                {
                    foreach (var member in members)
                    {
                        await logger.LogAsync(LogLevel.INFO, $"Found member '{member.Name}' with attribute ModuleImport.");

                        switch (member.MemberType)
                        {
                            case MemberTypes.Field:
                                var fieldInfo = ((FieldInfo)member);
                                var importObject = serviceProvider.GetService(fieldInfo.FieldType);
                                if (importObject != null)
                                {
                                    await logger.LogAsync(LogLevel.INFO, $"Injecting field with type '{fieldInfo.FieldType}'..");
                                    fieldInfo.SetValue(module, importObject);
                                }
                                else
                                {
                                    await logger.LogAsync(LogLevel.INFO, $"Import Object '{fieldInfo.FieldType}' was null.");
                                }
                                break;
                        }
                    }
                }
            }
        }
    }
}
