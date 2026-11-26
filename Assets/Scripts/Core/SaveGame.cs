using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DrownedBell.Core
{
    public enum Chapter { Belfry = 1, Sluice = 2, Archive = 3 }

    /// <summary>
    /// One campaign save: which chapter, what is opened or found, and the running totals. Positions are not saved:
    /// a continued run starts at the last lamp or lift with full health, exactly like a respawn.
    /// Stored as "key=value" lines with a version, so an old or damaged file is ignored instead of crashing.
    /// </summary>
    public sealed class SaveData
    {
        public const int Version = 1;

        public Chapter Chapter = Chapter.Belfry;
        public float Time;
        public int Deaths;
        public bool Lung;
        /// <summary>The chapter's lamp is lit, and its gate (Belfry grate or Sluice valve gate) is open.</summary>
        public bool Lamp, Gate;
        public bool Gauge, Bypass, Lift, Key, WardenDown;
        /// <summary>Archive: second keeper record, drum key, Archivist beaten, sea-gate order taken. Optional in the file (added after v0.3).</summary>
        public bool Ledger, DrumKey, ArchivistDown, Order;

        public string Serialize()
        {
            var sb = new StringBuilder();
            void Put(string key, object value) => sb.Append(key).Append('=').Append(Convert.ToString(value, CultureInfo.InvariantCulture)).Append('\n');
            Put("version", Version);
            Put("chapter", Chapter.ToString());
            Put("time", Time.ToString("R", CultureInfo.InvariantCulture));
            Put("deaths", Deaths);
            Put("lung", Lung);
            Put("lamp", Lamp);
            Put("gate", Gate);
            Put("gauge", Gauge);
            Put("bypass", Bypass);
            Put("lift", Lift);
            Put("key", Key);
            Put("warden", WardenDown);
            Put("ledger", Ledger);
            Put("drumkey", DrumKey);
            Put("archivist", ArchivistDown);
            Put("order", Order);
            Put("end", "ok");
            return sb.ToString();
        }

        /// <summary>Returns null for anything that is not a complete save of this version.</summary>
        public static SaveData Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var map = new Dictionary<string, string>();
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                int eq = line.IndexOf('=');
                if (eq > 0) map[line.Substring(0, eq)] = line.Substring(eq + 1);
            }
            // "end" is written last, so a file cut off mid-write is rejected.
            if (!map.TryGetValue("version", out string v) || v != Version.ToString(CultureInfo.InvariantCulture)) return null;
            if (!map.TryGetValue("end", out string end) || end != "ok") return null;
            var s = new SaveData();
            try
            {
                if (!Enum.TryParse(map["chapter"], out s.Chapter) || !Enum.IsDefined(typeof(Chapter), s.Chapter)) return null;
                s.Time = float.Parse(map["time"], CultureInfo.InvariantCulture);
                s.Deaths = int.Parse(map["deaths"], CultureInfo.InvariantCulture);
                s.Lung = bool.Parse(map["lung"]);
                s.Lamp = bool.Parse(map["lamp"]);
                s.Gate = bool.Parse(map["gate"]);
                s.Gauge = bool.Parse(map["gauge"]);
                s.Bypass = bool.Parse(map["bypass"]);
                s.Lift = bool.Parse(map["lift"]);
                s.Key = bool.Parse(map["key"]);
                s.WardenDown = bool.Parse(map["warden"]);
                bool Optional(string key) => map.TryGetValue(key, out string v2) && bool.Parse(v2);
                s.Ledger = Optional("ledger");
                s.DrumKey = Optional("drumkey");
                s.ArchivistDown = Optional("archivist");
                s.Order = Optional("order");
            }
            catch (Exception e) when (e is KeyNotFoundException || e is FormatException || e is OverflowException)
            {
                return null;
            }
            if (s.Time < 0f || float.IsNaN(s.Time) || float.IsInfinity(s.Time) || s.Deaths < 0) return null;
            return s;
        }

        public string Summary()
        {
            int t = (int)Time;
            return (Chapter == Chapter.Archive ? "Sounding Archive" : Chapter == Chapter.Sluice ? "Sluice Works" : "Drowned Belfry") + "   " + (t / 60) + ":" + (t % 60).ToString("00", CultureInfo.InvariantCulture);
        }
    }
}
