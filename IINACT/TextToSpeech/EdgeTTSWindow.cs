using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Components;
using Dalamud.Interface;
using System.Globalization;

namespace IINACT.TextToSpeech;

public class EdgeTTSWindow : Window
{
    private readonly EdgeTTSManager _manager;
    private bool _shouldMigrateFiles;
    private readonly Plugin _plugin;
    private string _newRuleFrom = "";
    private string _newRuleTo = "";

    public EdgeTTSWindow(EdgeTTSManager manager, Plugin plugin) : base("EdgeTTS设置")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        _manager = manager;
        _plugin = plugin;
    }

    public void Show()
    {
        IsOpen = true;
    }

    public override void Draw()
    {
        var config = _manager.GetConfig();
        var voiceEntries = _manager.GetAvailableVoiceEntries();
        var devices = _manager.GetAvailableDevices();

        ImGui.Columns(2, "EdgeTTSSettingsColumns", true);

        ImGui.Separator();

        using (var child = ImRaii.Child("VoiceListChild", new Vector2(ImGui.GetContentRegionAvail().X, -1), true))
        {
            if (child)
            {
                // 语言优先级：中文、日语、韩语、英语，其它按显示名排序
                static int GetLanguagePriority(string languageCode) => languageCode switch
                {
                    "zh" => 0,
                    "ja" => 1,
                    "ko" => 2,
                    "en" => 3,
                    _ => 100
                };

                static string GetPreferredLanguageFolderName(string languageCode, string fallbackDisplayName)
                {
                    var uiLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                    if (uiLang == "zh")
                    {
                        return languageCode switch
                        {
                            "zh" => "中文",
                            "ja" => "日语",
                            "ko" => "韩语",
                            "en" => "英语",
                            _ => fallbackDisplayName
                        };
                    }

                    return fallbackDisplayName;
                }

                static int GetGenderPriority(string gender) => gender switch
                {
                    "Female" => 0,
                    "Male" => 1,
                    _ => 2
                };

                var groupedByLanguage = voiceEntries
                    .GroupBy(v => v.LanguageCode)
                    .OrderBy(g => GetLanguagePriority(g.Key))
                    .ThenBy(g => GetPreferredLanguageFolderName(g.Key, g.First().LanguageDisplayName))
                    .ToArray();

                foreach (var langGroup in groupedByLanguage)
                {
                    var langName = GetPreferredLanguageFolderName(langGroup.Key, langGroup.First().LanguageDisplayName);
                    using var langNode = ImRaii.TreeNode($"{langName}##lang_{langGroup.Key}");
                    if (!langNode)
                        continue;

                    var groupedByGender = langGroup
                        .GroupBy(v => v.Gender)
                        .OrderBy(g => GetGenderPriority(g.Key))
                        .ThenBy(g => g.First().GenderDisplayName)
                        .ToArray();

                    foreach (var genderGroup in groupedByGender)
                    {
                        var genderName = genderGroup.First().GenderDisplayName;
                        using var genderNode = ImRaii.TreeNode($"{genderName}##gender_{langGroup.Key}_{genderGroup.Key}");
                        if (!genderNode)
                            continue;

                        foreach (var voice in genderGroup.OrderBy(v => v.FriendlyName).ThenBy(v => v.Value))
                        {
                            var isSelected = voice.Value == config.Voice;
                            var label = $"{voice.FriendlyName} ({voice.LocaleDisplayName})";

                            if (ImGui.Selectable(label, isSelected))
                            {
                                _manager.UpdateConfig(c => c.Voice = voice.Value);
                            }

                            if (isSelected)
                            {
                                ImGui.SetItemDefaultFocus();
                            }
                        }
                    }
                }
            }
        }

        ImGui.NextColumn();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("测试文本");
        ImGui.SameLine();
        if (ImGui.Button("朗读"))
        {
            Task.Run(() => _manager.Speak(config.TestText));
        }
        
        var testText = config.TestText;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##TestTextInput", ref testText, 1000)) 
        {
            _manager.UpdateConfig(c => c.TestText = testText);
        }
        ImGui.Spacing();
        
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X);
        ImGui.TextWrapped(testText);
        ImGui.PopTextWrapPos();
        ImGui.Spacing();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("语速:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
        var speed = config.Speed;
        if (ImGui.SliderInt("##SpeedSlider", ref speed, 1, 200))
        {
            _manager.UpdateConfig(c => c.Speed = speed);
        }

        ImGui.AlignTextToFramePadding();
        ImGui.Text("语调:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
        var pitch = config.Pitch;
        if (ImGui.SliderInt("##PitchSlider", ref pitch, 1, 200))
        {
            _manager.UpdateConfig(c => c.Pitch = pitch);
        }

        ImGui.AlignTextToFramePadding();
        ImGui.Text("音量:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
        var volume = config.Volume;
        if (ImGui.SliderInt("##VolumeSlider", ref volume, 1, 100))
        {
            _manager.UpdateConfig(c => c.Volume = volume);
        }

        ImGui.AlignTextToFramePadding();
        ImGui.Text("输出设备:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        var selectedDeviceIndex = devices.FindIndex(d => d.ID == config.DeviceId);
        if (ImGui.BeginCombo("##OutputDeviceCombo", selectedDeviceIndex >= 0 ? devices[selectedDeviceIndex].Name : "默认设备"))
        {
            if (ImGui.Selectable("默认设备", selectedDeviceIndex == -1))
            {
                _manager.UpdateConfig(c => c.DeviceId = 0);
            }
            for (var i = 0; i < devices.Count; i++)
            {
                if (ImGui.Selectable(devices[i].Name, i == selectedDeviceIndex))
                {
                    _manager.UpdateConfig(c => c.DeviceId = devices[i].ID);
                }
            }
            ImGui.EndCombo();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("文本替换规则");
        if (ImGui.BeginTable("TextReplacementTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn(" 写作");
            ImGui.TableSetupColumn(" 读作");
            ImGui.TableSetupColumn(" 操作");
            ImGui.TableHeadersRow();

            var toRemove = -1;
            for (var i = 0; i < config.TextReplacements.Count; i++)
            {
                var replacement = config.TextReplacements.ElementAt(i);
                var from = replacement.Key;
                var to = replacement.Value;

                ImGui.PushID(i);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText("##From" + i, ref from, 100))
                {
                    var newDict = new Dictionary<string, string>(config.TextReplacements);
                    newDict.Remove(replacement.Key);
                    newDict[from] = to;
                    _manager.UpdateConfig(c => c.TextReplacements = newDict);
                }

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText("##To" + i, ref to, 100))
                {
                    var newDict = new Dictionary<string, string>(config.TextReplacements);
                    newDict[replacement.Key] = to;
                    _manager.UpdateConfig(c => c.TextReplacements = newDict);
                }

                ImGui.TableNextColumn();
                if (ImGui.Button("删除##" + i))
                {
                    toRemove = i;
                }
                ImGui.PopID();
            }

            ImGui.PushID("new_rule");
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##NewFrom", ref _newRuleFrom, 100);

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##NewTo", ref _newRuleTo, 100);

            ImGui.TableNextColumn();
            if (string.IsNullOrEmpty(_newRuleFrom))
            {
                ImGui.BeginDisabled();
                ImGui.Button("添加");
                ImGui.EndDisabled();
            }
            else
            {
                if (ImGui.Button("添加"))
                {
                    var newDict = new Dictionary<string, string>(config.TextReplacements);
                    newDict[_newRuleFrom] = _newRuleTo;
                    _manager.UpdateConfig(c => c.TextReplacements = newDict);
                    _newRuleFrom = "";
                    _newRuleTo = "";
                }
            }
            ImGui.PopID();

            ImGui.EndTable();

            if (toRemove >= 0)
            {
                var newDict = new Dictionary<string, string>(config.TextReplacements);
                newDict.Remove(config.TextReplacements.ElementAt(toRemove).Key);
                _manager.UpdateConfig(c => c.TextReplacements = newDict);
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("缓存文件夹:");
        ImGui.SameLine();
        var cachePath = _manager.CurrentCachePath;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 30);
        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
        ImGui.InputText("##CachePath", ref cachePath, 1000, ImGuiInputTextFlags.ReadOnly);
        ImGui.PopStyleColor();
        ImGui.SameLine();
        if (ImGuiComponents.DisabledButton(FontAwesomeIcon.Folder))
        {
            _plugin.FileDialogManager.OpenFolderDialog("选择缓存文件夹", (success, path) =>
            {
                if (!success) return;
                if (_shouldMigrateFiles)
                {
                    _manager.MigrateCacheFiles(path);
                }
                _manager.UpdateConfig(c => c.CustomCachePath = path);
            }, cachePath);
        }

        ImGui.AlignTextToFramePadding();
        ImGui.Checkbox("迁移现有缓存文件", ref _shouldMigrateFiles);
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("选择新的缓存文件夹时，迁移现有缓存文件。");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("打开缓存文件夹"))
        {
            _manager.OpenCacheFolder();
        }
        ImGui.SameLine();
        if (ImGui.Button("清理缓存"))
        {
            _manager.CleanupCache();
        }

        ImGui.Columns(1);
    }
} 
