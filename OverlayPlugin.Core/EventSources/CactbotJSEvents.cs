using System.Collections.Generic;
using RainbowMage.OverlayPlugin.MemoryProcessors;

namespace RainbowMage.OverlayPlugin.EventSources;

// TODO: replace these all with just JObjects instead of explicit classes.
public interface JSEvent {
	string EventName();
}

// This class defines all the event |details| structures that go to each event type.
public class JSEvents {
	public class Point3F(float x, float y, float z) {
		public float x = x;
		public float y = y;
		public float z = z;
	}

	public class ForceReloadEvent : JSEvent {
		public string EventName() => "onForceReload";
	}

	public class GameExistsEvent(bool exists) : JSEvent {
		public string EventName() => "onGameExistsEvent";

		public bool exists = exists;
	}

	public class GameActiveChangedEvent(bool active) : JSEvent {
		public string EventName() => "onGameActiveChangedEvent";

		public bool active = active;
	}

	public class LogEvent(List<string> logs) : JSEvent {
		public string EventName() => "onLogEvent";

		public List<string> logs = logs;
	}

	public class ImportLogEvent(List<string> logs) : JSEvent {
		public string EventName() => "onImportLogEvent";

		public List<string> logs = logs;
	}

	public class InCombatChangedEvent(bool inActCombat, bool inGameCombat) : JSEvent {
		public string EventName() => "onInCombatChangedEvent";

		public bool inACTCombat = inActCombat;
		public bool inGameCombat = inGameCombat;
	}

	public class ZoneChangedEvent(string name) : JSEvent {
		public string EventName() => "onZoneChangedEvent";

		public string zoneName = name;
	}

	public class PlayerDiedEvent : JSEvent {
		public string EventName() => "onPlayerDied";
	}

	public class PartyWipeEvent : JSEvent {
		public string EventName() => "onPartyWipe";
	}

	public class PlayerChangedEvent(FFXIVProcess.EntityData e) : JSEvent {
		public string EventName() => "onPlayerChangedEvent";

		public uint id = e.id;
		public int level = e.level;
		public string name = e.name;
		public string job = e.job.ToString();

		public int currentHP = e.hp;
		public int maxHP = e.max_hp;
		public int currentMP = e.mp;
		public int maxMP = e.max_mp;
		public int currentTP;
		public int maxTP = 1000;
		public int currentGP = e.gp;
		public int maxGP = e.max_gp;
		public int currentCP = e.cp;
		public int maxCP = e.max_cp;
		public string debugJob = e.debug_job;
		public int currentShield = e.shield_value;

		public Point3F pos = new(e.pos_x, e.pos_y, e.pos_z);
		public float rotation = e.rotation;

		// One of the FooJobDetails structures, depending on the value of |job|.
		public object jobDetail = null;
	}

	public abstract class EntityChangedEvent {
		public EntityChangedEvent(FFXIVProcess.EntityData e) {
			if (e != null) {
				id = e.id;
				level = e.level;
				name = e.name;
				job = e.job.ToString();
				currentHP = e.hp;
				maxHP = e.max_hp;
				currentMP = e.mp;
				maxMP = e.max_mp;
				pos = new Point3F(e.pos_x, e.pos_y, e.pos_z);
				distance = e.distance;
			}
		}

		public uint id;
		public int level;
		public string name;
		public string job;

		public int currentHP;
		public int maxHP;
		public int currentMP;
		public int maxMP;
		public int currentTP = 0;
		public int maxTP = 0;

		public Point3F pos;
		public int distance;
	}

	public class SendSaveData(string data) : JSEvent {
		public string EventName() => "onSendSaveData";

		public string data = data;
	}

	public class DataFilesRead(Dictionary<string, string> files) : JSEvent {
		public string EventName() => "onDataFilesRead";

		public Dictionary<string, string> files = files;
	}

	public class OnInitializeOverlay(string location, Dictionary<string, string> files, string language) : JSEvent {
		public string EventName() => "onInitializeOverlay";

		public string userLocation = location;
		public Dictionary<string, string> localUserFiles = files;
		public string language = language;
	}
}