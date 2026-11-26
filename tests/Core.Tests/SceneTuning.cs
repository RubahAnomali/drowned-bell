using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using DrownedBell.Core;

internal static class SceneTuning
{
    public static void Check(Action<bool, string> check)
    {
        var defaults = new Dictionary<string, object>
        {
            ["movement"] = new MovementTuning(), ["combat"] = new CombatTuning(),
            ["enemies"] = new EnemyTuning(), ["boss"] = new BossTuning(),
        };
        var expected = defaults.SelectMany(group => group.Value.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(field => (Key: group.Key + "." + field.Name, Value: field.GetValue(group.Value))))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        var differences = new List<string>();
        var scene = File.ReadLines(Path.Combine(AppContext.BaseDirectory, "Main.unity"))
            .SkipWhile(line => line.Trim() != "m_EditorClassIdentifier: Assembly-CSharp::DrownedBell.Game.GameController")
            .Skip(1).TakeWhile(line => !line.StartsWith("---"));
        string groupName = "";
        foreach (string line in scene)
        {
            if (!line.StartsWith("    "))
            {
                groupName = line.Trim().TrimEnd(':');
                continue;
            }
            if (!defaults.ContainsKey(groupName)) continue;
            string[] parts = line.Trim().Split(':', 2);
            string key = groupName + "." + parts[0];
            if (!expected.Remove(key, out object value)) differences.Add("unexpected " + key);
            else if (parts.Length != 2 || !Equals(Convert.ChangeType(parts[1].Trim(), value.GetType(), CultureInfo.InvariantCulture), value))
                differences.Add(key + " saved " + parts.Last().Trim() + ", expected " + value);
        }
        differences.AddRange(expected.Keys.Select(key => "missing " + key));
        check(differences.Count == 0, "Main.unity tuning matches Core defaults " + string.Join(", ", differences));
    }
}
