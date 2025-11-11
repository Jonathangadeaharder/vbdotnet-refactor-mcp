using System.Reflection;
using MCP.Contracts;
using Microsoft.Extensions.Logging;

namespace MCP.RefactoringWorker;

/// <summary>
/// Discovers and loads refactoring tool plugins from a designated directory.
///
/// Implements the plugin discovery and isolation strategy from Section 7.2:
/// - Scans the plugins directory for assemblies
/// - Loads each plugin into its own AssemblyLoadContext for dependency isolation
/// - Discovers types implementing IRefactoringProvider
/// - Registers them by name for runtime lookup
/// </summary>
public class PluginLoader
{
    private readonly ILogger<PluginLoader> _logger;
    private readonly Dictionary<string, IRefactoringProvider> _providers = new();
    private readonly List<PluginLoadContext> _loadContexts = new();

    public PluginLoader(ILogger<PluginLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads all plugins from the specified directory.
    /// </summary>
    /// <param name="pluginDirectory">Directory containing plugin DLLs</param>
    public void LoadPlugins(string pluginDirectory)
    {
        if (!Directory.Exists(pluginDirectory))
        {
            _logger.LogWarning("Plugin directory does not exist: {PluginDirectory}", pluginDirectory);
            return;
        }

        _logger.LogInformation("Loading plugins from: {PluginDirectory}", pluginDirectory);

        // Find all DLL files in the plugin directory
        var pluginFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);

        foreach (var pluginPath in pluginFiles)
        {
            try
            {
                LoadPlugin(pluginPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin: {PluginPath}", pluginPath);
            }
        }

        _logger.LogInformation(
            "Plugin loading complete. Loaded {Count} provider(s): {Names}",
            _providers.Count,
            string.Join(", ", _providers.Keys));
    }

    private void LoadPlugin(string pluginPath)
    {
        _logger.LogDebug("Loading plugin assembly: {PluginPath}", pluginPath);

        // Create an isolated load context for this plugin
        var loadContext = new PluginLoadContext(pluginPath);
        _loadContexts.Add(loadContext);

        // Load the plugin assembly into the isolated context
        var assembly = loadContext.LoadFromAssemblyPath(pluginPath);

        // Find all types that implement IRefactoringProvider
        var providerTypes = assembly.GetTypes()
            .Where(t => typeof(IRefactoringProvider).IsAssignableFrom(t)
                       && !t.IsInterface
                       && !t.IsAbstract);

        foreach (var providerType in providerTypes)
        {
            try
            {
                // Instantiate the provider
                var provider = (IRefactoringProvider?)Activator.CreateInstance(providerType);
                if (provider == null)
                {
                    _logger.LogWarning(
                        "Failed to instantiate provider type: {TypeName}",
                        providerType.FullName);
                    continue;
                }

                // Register by name
                if (_providers.ContainsKey(provider.Name))
                {
                    _logger.LogWarning(
                        "Duplicate provider name '{Name}' in {Assembly}. Skipping.",
                        provider.Name,
                        assembly.GetName().Name);
                    continue;
                }

                _providers[provider.Name] = provider;

                _logger.LogInformation(
                    "Registered plugin: {Name} from {Assembly} - {Description}",
                    provider.Name,
                    assembly.GetName().Name,
                    provider.Description);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to instantiate provider type: {TypeName}",
                    providerType.FullName);
            }
        }
    }

    /// <summary>
    /// Gets a provider by name.
    /// </summary>
    public IRefactoringProvider? GetProvider(string name)
    {
        return _providers.TryGetValue(name, out var provider) ? provider : null;
    }

    /// <summary>
    /// Gets all registered provider names.
    /// </summary>
    public IEnumerable<string> GetProviderNames() => _providers.Keys;

    /// <summary>
    /// Unloads all plugin contexts (for graceful shutdown).
    /// </summary>
    public void Unload()
    {
        foreach (var context in _loadContexts)
        {
            context.Unload();
        }
        _loadContexts.Clear();
        _providers.Clear();
    }
}
