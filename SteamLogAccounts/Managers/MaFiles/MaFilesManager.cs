using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using ParadiseHelper.SteamLogAccounts.SteamAuth;
using Core;

namespace ParadiseHelper.Managers.MaFiles
{
    /// <summary>
    /// Manages Steam mobile authenticator files (.maFile) for 2FA and SteamID lookup.
    /// Uses a thread-safe cache that REFRESHES on each call to reflect disk changes.
    /// </summary>
    public static class MaFilesManager
    {
        // Lock object for thread-safe access to the cache.
        private static readonly object _cacheLock = new object();

        // The cache stores account_name -> MaFileModel. It is reloaded on every request.
        private static Dictionary<string, MaFileModel> _maFileCache;

        static MaFilesManager()
        {
            if (!Directory.Exists(FilePaths.Standard.MaFilesDirectory))
                Directory.CreateDirectory(FilePaths.Standard.MaFilesDirectory);
        }

        /// <summary>
        /// Ensures the internal cache of maFile data is loaded from disk.
        /// NOTE: This method now ALWAYS reloads from disk to reflect real-time changes.
        /// </summary>
        private static void EnsureCacheLoaded()
        {
            // Lock to prevent simultaneous read/write access from different threads.
            lock (_cacheLock)
            {
                var tempCache = new Dictionary<string, MaFileModel>(StringComparer.OrdinalIgnoreCase);

                foreach (var file in Directory.GetFiles(FilePaths.Standard.MaFilesDirectory, "*.maFile"))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var ma = JsonConvert.DeserializeObject<MaFileModel>(json);

                        if (!string.IsNullOrEmpty(ma?.account_name))
                        {
                            tempCache[ma.account_name] = ma;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Logging error on file load failure.
                        Console.WriteLine($"[MaFilesManager ERROR] Failed to load maFile: {file}. Error: {ex.Message}");
                    }
                }

                _maFileCache = tempCache;
            }
        }

        /// <summary>
        /// Finds the maFile data for the given login and generates the current 2FA code.
        /// </summary>
        public static string Get2FACodeByLogin(string login)
        {
            EnsureCacheLoaded(); // Reloads fresh data

            lock (_cacheLock)
            {
                if (_maFileCache.TryGetValue(login, out var ma))
                {
                    if (string.IsNullOrEmpty(ma.shared_secret))
                    {
                        Console.WriteLine($"[MaFilesManager WARNING] 'shared_secret' missing for account: {login}");
                        return null;
                    }

                    return SteamGuardCodeGenerator.GenerateCode(ma.shared_secret);
                }
            }

            Console.WriteLine($"[MaFilesManager WARNING] No maFile data found in cache for account: {login}");
            return null;
        }

        /// <summary>
        /// Retrieves the SteamID64 for a given login from the cache.
        /// </summary>
        public static ulong? GetSteamID64ByLogin(string login)
        {
            EnsureCacheLoaded(); // Reloads fresh data

            lock (_cacheLock)
            {
                if (_maFileCache.TryGetValue(login, out var ma))
                {
                    if (ma.Session?.SteamID > 0) 
                        return ma.Session?.SteamID;
                }
            }

            return null;
        }

        /// <summary>
        /// Loads all available shared secrets into a dictionary (login -> shared_secret) from the cache.
        /// </summary>
        public static Dictionary<string, string> LoadSecrets()
        {
            EnsureCacheLoaded(); // Reloads fresh data

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            lock (_cacheLock)
            {
                foreach (var kvp in _maFileCache)
                {
                    if (!string.IsNullOrEmpty(kvp.Value.shared_secret))
                    {
                        dict.Add(kvp.Key, kvp.Value.shared_secret);
                    }
                }
            }

            return dict;
        }
    }
}