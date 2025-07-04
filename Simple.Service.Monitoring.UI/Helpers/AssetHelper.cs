using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Simple.Service.Monitoring.UI.Helpers
{
    public static class AssetHelper
    {
        // Cache manifest data to avoid reading from embedded resources on every request
        private static readonly ConcurrentDictionary<string, string> AssetPathCache = new();
        private static Dictionary<string, string> ManifestData;
        private static readonly object ManifestLock = new();
        private static DateTime LastManifestCheck = DateTime.MinValue;
        private static readonly TimeSpan ManifestCacheTime = TimeSpan.FromMinutes(10);

        // Flag to enable/disable caching (should be set during startup)
        public static bool EnableCaching { get; set; } = true;

        public static string GetAssetPath(string fileName, string fileType = "js", string fallbackPath = null)
        {
            // Create a cache key combining fileName and fileType
            string cacheKey = $"{fileName}_{fileType}";

            // Try to get from cache first (if caching is enabled)
            if (EnableCaching && AssetPathCache.TryGetValue(cacheKey, out var cachedPath))
            {
                return cachedPath;
            }

            try
            {
                // Check if we need to load/reload the manifest
                LoadManifestIfNeeded();

                // Try to get the path from manifest
                var key = $"{fileName}.{fileType}";
                if (ManifestData != null && ManifestData.TryGetValue(key, out var hashedPath))
                {
                    var result = $"/monitoring-static/{hashedPath}";

                    // Only cache if enabled
                    if (EnableCaching)
                    {
                        AssetPathCache[cacheKey] = result;
                    }
                    return result;
                }

                // Fallback path if not found
                var defaultPath = fallbackPath ?? $"/monitoring-static/{fileType}/{fileName}.{fileType}";

                // Only cache if enabled
                if (EnableCaching)
                {
                    AssetPathCache[cacheKey] = defaultPath;
                }
                return defaultPath;
            }
            catch
            {
                // Return fallback on error
                var defaultPath = fallbackPath ?? $"/monitoring-static/{fileType}/{fileName}.{fileType}";

                // Only cache if enabled
                if (EnableCaching)
                {
                    AssetPathCache[cacheKey] = defaultPath;
                }
                return defaultPath;
            }
        }

        // Convenience method specifically for JS files
        public static string GetJsPath(string fileName) =>
            GetAssetPath(fileName, "js", $"/monitoring-static/js/{fileName}.bundle.js");

        // Convenience method specifically for CSS files
        public static string GetCssPath(string fileName) =>
            GetAssetPath(fileName, "css", $"/monitoring-static/css/{fileName}.css");

        private static void LoadManifestIfNeeded()
        {
            var now = DateTime.Now;

            // Skip cache check if caching is disabled
            if (EnableCaching && ManifestData != null && (now - LastManifestCheck) < ManifestCacheTime)
            {
                return;
            }

            lock (ManifestLock)
            {
                // Double-check inside lock (skip if caching is disabled)
                if (EnableCaching && ManifestData != null && (now - LastManifestCheck) < ManifestCacheTime)
                {
                    return;
                }

                try
                {
                    // Get assembly for embedded resources
                    var assembly = typeof(Simple.Service.Monitoring.UI.Models.IndexModel).Assembly;
                    var embeddedProvider = new EmbeddedFileProvider(assembly, "Simple.Service.Monitoring.UI.wwwroot");

                    // Load the manifest file
                    var manifestFile = embeddedProvider.GetFileInfo("asset-manifest.json");

                    if (manifestFile.Exists)
                    {
                        using var stream = manifestFile.CreateReadStream();
                        using var reader = new StreamReader(stream);
                        var manifestJson = reader.ReadToEnd();
                        ManifestData = JsonSerializer.Deserialize<Dictionary<string, string>>(manifestJson);

                        // Clear cache when manifest reloads (if caching is enabled)
                        if (EnableCaching)
                        {
                            AssetPathCache.Clear();
                        }
                    }

                    LastManifestCheck = now;
                }
                catch
                {
                    // On error, just set an empty manifest and try again later
                    ManifestData = new Dictionary<string, string>();
                    LastManifestCheck = now;
                }
            }
        }
    }
}