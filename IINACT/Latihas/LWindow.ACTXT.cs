using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;

namespace IINACT.Latihas;

public partial class LWindow {
	public partial class ActxtEditor() : Window($"{WindowPrefix}ActxtViewer") {
		private const string fmt = @"hh\:mm\:ss\.fff";
		private static readonly FileDialogManager FileDialogManager = new();
		private static string FilePath = "";
		private static List<string> ImportLogs = [];

		public static string ReadLockedTextFile(string filePath) {
			using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}

		private ActxtCombat[]? actxtCombats = [];

		private class ActxtCombat(string territory, int id) {
			public readonly string territory = territory;
			public string time = null!, period = null!;
			public readonly string id = id.ToString();
			public readonly List<string> logs = [];
		}

		private ActxtCombat? currentCombat;
		private string currentCombatStart;
		private int ReplayTotal, ReplayIndex;

		public override void Draw() {
			if (actxtCombats == null) ImGui.Text("处理中");
			else {
				FileDialogManager.Draw();
				if (ImGui.Button("选择文件"))
					FileDialogManager.OpenFileDialog("Selector", ".actxt", (succ, str) => {
						if (!succ) return;
						actxtCombats = null;
						Task.Run(() => {
							List<ActxtCombat> combats = [];
							FilePath = str.First();
							ImportLogs = ReadLockedTextFile(FilePath).Split(Environment.NewLine).ToList();
							ImportLogs.Sort((a, b) => string.Compare(
								a,
								0,
								b,
								0,
								14,
								StringComparison.Ordinal
							));
							var Territory = 0u;
							foreach (var log in ImportLogs) {
								var territoryRegex = TerritoryRegex_.Match(log);
								if (territoryRegex.Success)
									Territory = Convert.ToUInt32(territoryRegex.Groups["id"].Value, 16);
								var start = StartCombatRegex_.Match(log);
								if (start.Success) {
									currentCombat = new(Territory == 0 ? "None" : Plugin.MapInfo[Territory], combats.Count);
									currentCombatStart = start.Groups["time"].Value;
								}
								currentCombat?.logs.Add(log);
								var end = EndCombatRegex_.Match(log);
								if (end.Success) {
									if (currentCombat != null) {
										currentCombat.time = $"{currentCombatStart}-{end.Groups["time"].Value}";
										var duration =
											TimeSpan.ParseExact(end.Groups["time"].Value.Trim('[', ']'), fmt, null) -
											TimeSpan.ParseExact(currentCombatStart.Trim('[', ']'), fmt, null);
										if (duration < TimeSpan.Zero) duration += TimeSpan.FromDays(1);
										var sec = (int)Math.Round(duration.TotalSeconds);
										currentCombat.period = $"{sec / 60}:{sec % 60:D2}";
										combats.Add(currentCombat);
									}
									currentCombat = null;
								}
							}
							actxtCombats = combats.ToArray();
						});
					}, 1, Plugin.Instance.Configuration.LogFilePath);
				ImGui.SameLine();
				ImGui.Text(FilePath);
				ImGui.Text($"共{ImportLogs.Count}条, 点击复制战斗日志行");
				if (ReplayCts != null) {
					ImGui.Text($"正在重放({ReplayIndex}/{ReplayTotal})");
					ImGui.SameLine();
					if (ImGui.Button("停止所有重放")) {
						ReplayCts.Cancel();
						ReplayCts.Dispose();
						ReplayCts = null;
					}
				}
				NewTable(["Id", "时间", "时长", "地区", "长度", "操作"], actxtCombats, [
					i => ImGui.Text(i.id),
					i => ImGui.Text(i.time),
					i => ImGui.Text(i.period),
					i => ImGui.Text(i.territory),
					i => ImGui.Text(i.logs.Count.ToString()),
					i => {
						if (ImGui.Button($"复制##Copy{i.id}"))
							ImGui.SetClipboardText(string.Join(Environment.NewLine, i.logs));
						if (ReplayCts == null) {
							ImGui.SameLine();
							if (ImGui.Button($"重放##Replay{i.id}")) {
								ReplayCts = new();
								var token = ReplayCts.Token;
								Task.Run(async () => {
									var orderedEntries = i.logs
										.Select(line => new {
											Timestamp = ParseTimestamp(line),
											RawLine = line
										})
										.OrderBy(item => item.Timestamp)
										.ToList();
									if (orderedEntries.Count == 0) return;
									var baseTime = orderedEntries[0].Timestamp;
									var startTime = DateTime.Now;
									ReplayTotal = orderedEntries.Count;
									for (var index = 0; index < orderedEntries.Count; index++) {
										var entry = orderedEntries[index];
										var targetDelayMs = (entry.Timestamp - baseTime).TotalMilliseconds;
										var elapsedMs = (DateTime.Now - startTime).TotalMilliseconds;
										var remainMs = targetDelayMs - elapsedMs;
										if (remainMs > 0)
											await Task.Delay((int)Math.Ceiling(remainMs), token).ConfigureAwait(false);
										ReplayIndex = index;
										ActGlobals.oFormActMain.FakeOnLogLineRead(entry.RawLine);
									}
									ReplayCts.Dispose();
									ReplayCts = null;
								}, token);
							}
						}
					}
				]);
			}
		}

		private static DateTime ParseTimestamp(string logLine) => DateTime.ParseExact(logLine.Substring(1, 12), "HH:mm:ss.fff", null);

		private CancellationTokenSource? ReplayCts;

		[GeneratedRegex(@"^.{14} Territory 01:(?<id>[^:]+):")]
		private static partial Regex TerritoryRegex();

		private static readonly Regex TerritoryRegex_ = TerritoryRegex();

		[GeneratedRegex(@"^(?<time>.{14}) 260 104:.:0:.:1")]
		private static partial Regex EndCombatRegex();

		private static readonly Regex EndCombatRegex_ = EndCombatRegex();

		[GeneratedRegex(@"^(?<time>.{14}) 260 104:.:1:.:1")]
		private static partial Regex StartCombatRegex();

		private static readonly Regex StartCombatRegex_ = StartCombatRegex();
	}
}