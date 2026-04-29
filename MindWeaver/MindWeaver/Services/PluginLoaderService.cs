using System.Reflection;
using MindWeaver.Contracts;

namespace MindWeaver.Services
{
    /// <summary>
    /// Scans the application's dedicated Plugins directory for external .NET assemblies,
    /// discovers all concrete <see cref="IWidgetPlugin"/> implementations, and returns
    /// instantiated plugin objects ready for the host to render.
    /// </summary>
    public class PluginLoaderService
    {
        // ── Configuration ────────────────────────────────────────────────────────
        // Plugins live in a "Plugins" sub-folder under the app's sandboxed data dir.
        // On Android/iOS this is the app's private storage; on Windows/macOS it is
        // the user-scoped AppData equivalent exposed by MAUI's FileSystem API.
        private static string PluginDirectory =>
            Path.Combine(FileSystem.AppDataDirectory, "Plugins");

        /// <summary>
        /// Loads all valid <see cref="IWidgetPlugin"/> instances found in external
        /// .dll files placed in the Plugins directory.
        /// </summary>
        /// <returns>
        /// A lazily-evaluated sequence of activated plugin instances.
        /// Invalid or non-.NET DLLs are silently skipped.
        /// </returns>
        public IEnumerable<IWidgetPlugin> LoadPlugins()
        {
            // Ensure the directory exists so that users can drop plugins into it.
            if (!Directory.Exists(PluginDirectory))
            {
                Directory.CreateDirectory(PluginDirectory);
                yield break;
            }

            var dllFiles = Directory.GetFiles(PluginDirectory, "*.dll");

            foreach (var filePath in dllFiles)
            {
                IEnumerable<IWidgetPlugin>? plugins = null;

                try
                {
                    var assembly = Assembly.LoadFrom(filePath);

                    plugins = assembly.GetTypes()
                        .Where(t =>
                            typeof(IWidgetPlugin).IsAssignableFrom(t) &&
                            t.IsClass &&
                            !t.IsAbstract)
                        .Select(t => Activator.CreateInstance(t) as IWidgetPlugin)
                        .Where(instance => instance is not null)!;
                }
                catch (Exception ex) when (
                    ex is BadImageFormatException    // Not a valid .NET PE
                    or FileLoadException             // Assembly binding failed
                    or ReflectionTypeLoadException)  // Partial type load
                {
                    // Log-friendly skip — keeps the dashboard stable even if a
                    // rogue DLL is placed in the Plugins folder.
                    System.Diagnostics.Debug.WriteLine(
                        $"[PluginLoader] Skipped '{Path.GetFileName(filePath)}': {ex.Message}");
                    continue;
                }

                foreach (var plugin in plugins)
                {
                    yield return plugin;
                }
            }
        }
    }
}
