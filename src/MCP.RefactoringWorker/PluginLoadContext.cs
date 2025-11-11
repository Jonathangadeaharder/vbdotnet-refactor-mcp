using System.Reflection;
using System.Runtime.Loader;

namespace MCP.RefactoringWorker;

/// <summary>
/// Custom AssemblyLoadContext for isolating plugin dependencies.
///
/// As specified in Section 7.2 of the architectural blueprint, this prevents
/// "DLL Hell" by ensuring each plugin loads its own dependencies from its
/// own directory, independent of the host application and other plugins.
///
/// For example:
/// - PluginA built against Roslyn 4.6 loads from PluginA's directory
/// - PluginB built against Roslyn 4.7 loads from PluginB's directory
/// - No conflicts occur
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        // AssemblyDependencyResolver reads the .deps.json file of the plugin
        // and resolves dependencies based on the plugin's own dependency graph
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to resolve from the plugin's dependency graph
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // If not found in plugin dependencies, fall back to default context
        // This allows shared contracts (MCP.Contracts) to be loaded from the host
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}
