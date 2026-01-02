using System.Collections.Generic;
using RainbowMage.OverlayPlugin.MemoryProcessors;

namespace RainbowMage.OverlayPlugin.EventSources;

// TODO: replace these all with just JObjects instead of explicit classes.
public interface JSEvent {
    string EventName();
}

// This class defines all the event |details| structures that go to each event type.
public class JSEvents {
    public class Point3F {
        public Point3F(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public float x;
        public float y;
        public float z;
    }

    public class ForceReloadEvent : JSEvent {
        public string EventName() => "onForceReload";
    }

    public class GameExistsEvent : JSEvent {
        public GameExistsEvent(bool exists) {
            this.exists = exists;
        }

        public string EventName() => "onGameExistsEvent";

        public bool exists;
    }

    public class GameActiveChangedEvent : JSEvent {
        public GameActiveChangedEvent(bool active) {
            this.active = active;
        }

        public string EventName() => "onGameActiveChangedEvent";

        public bool active;
    }

    public class LogEvent : JSEvent {
        public LogEvent(List<string> logs) {
            this.logs = logs;
        }

        public string EventName() => "onLogEvent";

        public List<string> logs;
    }

    public class ImportLogEvent : JSEvent {
        public ImportLogEvent(List<string> logs) {
            this.logs = logs;
        }

        public string EventName() => "onImportLogEvent";

        public List<string> logs;
    }

    public class InCombatChangedEvent : JSEvent {
        public InCombatChangedEvent(bool in_act_combat, bool in_game_combat) {
            inACTCombat = in_act_combat;
            inGameCombat = in_game_combat;
        }

        public string EventName() => "onInCombatChangedEvent";

        public bool inACTCombat;
        public bool inGameCombat;
    }

    public class ZoneChangedEvent : JSEvent {
        public ZoneChangedEvent(string name) {
            zoneName = name;
        }

        public string EventName() => "onZoneChangedEvent";

        public string zoneName;
    }

    public class PlayerDiedEvent : JSEvent {
        public string EventName() => "onPlayerDied";
    }

    public class PartyWipeEvent : JSEvent {
        public string EventName() => "onPartyWipe";
    }

    public class PlayerChangedEvent : JSEvent {
        public PlayerChangedEvent(FFXIVProcess.EntityData e) {
            id = e.id;
            level = e.level;
            name = e.name;
            job = e.job.ToString();
            currentHP = e.hp;
            maxHP = e.max_hp;
            currentMP = e.mp;
            maxMP = e.max_mp;
            maxTP = 1000;
            currentGP = e.gp;
            maxGP = e.max_gp;
            currentCP = e.cp;
            maxCP = e.max_cp;
            pos = new Point3F(e.pos_x, e.pos_y, e.pos_z);
            rotation = e.rotation;
            jobDetail = null;
            debugJob = e.debug_job;
            currentShield = e.shield_value;
        }

        public string EventName() => "onPlayerChangedEvent";

        public uint id;
        public int level;
        public string name;
        public string job;

        public int currentHP;
        public int maxHP;
        public int currentMP;
        public int maxMP;
        public int currentTP;
        public int maxTP;
        public int currentGP;
        public int maxGP;
        public int currentCP;
        public int maxCP;
        public string debugJob;
        public int currentShield;

        public Point3F pos;
        public float rotation;

        // One of the FooJobDetails structures, depending on the value of |job|.
        public object jobDetail;
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

    public class SendSaveData : JSEvent {
        public SendSaveData(string data) {
            this.data = data;
        }

        public string EventName() => "onSendSaveData";

        public string data;
    }

    public class DataFilesRead : JSEvent {
        public DataFilesRead(Dictionary<string, string> files) {
            this.files = files;
        }

        public string EventName() => "onDataFilesRead";

        public Dictionary<string, string> files;
    }

    public class OnInitializeOverlay : JSEvent {
        public OnInitializeOverlay(string location, Dictionary<string, string> files, string language) {
            userLocation = location;
            localUserFiles = files;
            this.language = language;
        }

        public string EventName() => "onInitializeOverlay";

        public string userLocation;
        public Dictionary<string, string> localUserFiles;
        public string language;
    }
}