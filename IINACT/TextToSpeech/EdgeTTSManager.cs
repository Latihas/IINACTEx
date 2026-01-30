using Dalamud.Plugin.Services;
using EdgeTTS;
using EdgeTTS.Models;
using System.Diagnostics;
using System.Globalization;

namespace IINACT.TextToSpeech;

public record Voice(string Value, string DisplayName);

public sealed record VoiceEntry(
    string Value,
    string FriendlyName,
    string Locale,
    string LocaleDisplayName,
    string LanguageCode,
    string LanguageDisplayName,
    string Gender,
    string GenderDisplayName
);

public class EdgeTTSManager
{
    private readonly string _configPath;
    private string _cachePath = string.Empty;
    private readonly EdgeTTSConfig _config;
    private EdgeTTSEngine _engine = null!;
    private readonly object _lock = new();
    private readonly IPluginLog _log;

    public string CurrentCachePath => _cachePath;

    private void ExtractVoicesJson()
    {
        try
        {
            var assembly = typeof(EdgeTTSManager).Assembly;
            using var stream = assembly.GetManifestResourceStream("IINACT.Resources.voices.json");
            if (stream == null)
            {
                _log.Error("EdgeTTSManager: voices.json embedded resource not found");
                return;
            }

            var voicesPath = Path.Combine(_cachePath, "voices.json");
            using var reader = new StreamReader(stream);
            var jsonContent = reader.ReadToEnd();

            File.WriteAllText(voicesPath, jsonContent);
            _log.Debug($"EdgeTTSManager: Extracted voices.json to {voicesPath}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "EdgeTTSManager: Failed to extract voices.json from embedded resource");
        }
    }

    public EdgeTTSManager(IPluginLog log, string configPath)
    {
        _log = log;
        _configPath = Path.Combine(Path.GetDirectoryName(configPath)!, "IINACT", "Notification", "TextToSpeech.json");
        _config = EdgeTTSConfig.Load(_configPath);
        
        UpdateCachePath(_config.CustomCachePath);
    }

    private void UpdateCachePath(string? customPath)
    {
        _cachePath = string.IsNullOrEmpty(customPath)
            ? Path.Combine(Path.GetDirectoryName(_configPath)!, "Cache")
            : customPath;

        if (!Directory.Exists(_cachePath))
            Directory.CreateDirectory(_cachePath);

        // Extract voices.json from embedded resource to cache directory
        ExtractVoicesJson();

        _engine = new EdgeTTSEngine
        {
            CacheFolder = _cachePath,
            VoiceFolder = _cachePath, // Use cache directory as voice folder
            LogHandler = message => _log.Debug($"EdgeTTS: {message}")
        };
    }

    public void UpdateConfig(Action<EdgeTTSConfig> updateAction)
    {
        lock (_lock)
        {
            var oldCachePath = _config.CustomCachePath;
            updateAction(_config);
            _config.Save(_configPath);

            if (oldCachePath != _config.CustomCachePath)
            {
                UpdateCachePath(_config.CustomCachePath);
            }
        }
    }

    public EdgeTTSConfig GetConfig() => _config;

    public Voice[] GetAvailableVoices() =>
        _engine.Voices
               .SelectMany(localeGroup => localeGroup.Value.SelectMany(genderGroup => genderGroup.Value))
               .Select(voiceInfo => new Voice
               (
                   voiceInfo.ShortName,
                   $"{voiceInfo.FriendlyName} ({voiceInfo.LocaleInfo.DisplayName} - {voiceInfo.GenderName})"
               ))
               .OrderBy(v => v.DisplayName)
               .ToArray();

    public VoiceEntry[] GetAvailableVoiceEntries()
    {
        static string GetLanguageDisplayName(string languageCode)
        {
            try
            {
                return CultureInfo.GetCultureInfo(languageCode).DisplayName;
            }
            catch
            {
                return languageCode;
            }
        }

        static string GetGenderDisplayName(VoiceInfo voiceInfo)
        {
            var uiLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (uiLang == "zh")
            {
                return voiceInfo.Gender switch
                {
                    "Male" => "男",
                    "Female" => "女",
                    _ => "其他"
                };
            }

            return voiceInfo.GenderName;
        }

        return _engine.Voices
            .SelectMany(localeGroup => localeGroup.Value.SelectMany(genderGroup => genderGroup.Value))
            .Select(voiceInfo =>
            {
                var localeInfo = voiceInfo.LocaleInfo;
                var languageCode = localeInfo.TwoLetterISOLanguageName;

                return new VoiceEntry(
                    voiceInfo.ShortName,
                    voiceInfo.FriendlyName,
                    voiceInfo.Locale,
                    localeInfo.DisplayName,
                    languageCode,
                    GetLanguageDisplayName(languageCode),
                    voiceInfo.Gender,
                    GetGenderDisplayName(voiceInfo)
                );
            })
            .ToArray();
    }

    public List<AudioDevice> GetAvailableDevices()
    {
        var devices = _engine.AudioDevices;
        return devices
            .OrderBy(pair => pair.Key)
            .Select(pair => pair.Value)
            .ToList();
    }

    public async Task Speak(string text)
    {
        var settings = _config.ToEdgeTTSSettings();
        await _engine.SpeakAsync(text, settings);
    }

    public void CleanupCache()
    {
        try
        {
            foreach (var file in Directory.GetFiles(_cachePath, "*.mp3"))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // 忽略单个文件删除失败的情况
                }
            }
        }
        catch
        {
            // 忽略缓存清理失败的情况
        }
    }

    public void OpenCacheFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _cachePath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"无法打开缓存文件夹: {_cachePath}");
        }
    }

    public void MigrateCacheFiles(string newPath)
    {
        try
        {
            if (!Directory.Exists(newPath))
                Directory.CreateDirectory(newPath);

            foreach (var file in Directory.GetFiles(_cachePath, "*.mp3"))
            {
                try
                {
                    var fileName = Path.GetFileName(file);
                    var destPath = Path.Combine(newPath, fileName);
                    File.Move(file, destPath, true);
                }
                catch
                {
                    // 忽略单个文件迁移失败的情况
                }
            }
        }
        catch
        {
            // 忽略整体迁移失败的情况
        }
    }
} 
