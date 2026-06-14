using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Central (de)serializer for player meta-progression. Aggregates EVERY <see cref="ISaveable"/>
    /// registered in the Domain into a single <see cref="CloudSaveBlob"/> JSON, and applies a blob
    /// back onto those same saveables by matching <see cref="IContent.Id"/>.
    ///
    /// <para>This is the "tổ chức data tập trung" layer: one call gives you the whole player state as
    /// a portable JSON string, independent of which backend stores it. Mirrors the matching logic of
    /// the existing local SaveService but is backend-agnostic and self-contained.</para>
    ///
    /// <para>Apply must run AFTER all saveable content (incl. ItemConfig sub-assets) is registered in
    /// the Domain, and after <c>ItemConfigsOwned</c>'s boot-time random seeding — it then overrides
    /// that random state with the synced state. <see cref="CloudSyncService"/> handles this ordering.</para>
    /// </summary>
    public static class PlayerDataSerializer
    {
        /// <summary>Snapshot every Domain ISaveable into a JSON blob string.</summary>
        public static string SerializeDomain(IDomain domain)
        {
            var blob = new CloudSaveBlob();
            if (domain == null) return JsonUtility.ToJson(blob);

            var saveables = domain.GetAll<ISaveable>();
            for (int i = 0; i < saveables.Count; i++)
            {
                var saveable = saveables[i];
                if (saveable is not IContent content) continue;

                IEnumerable<SaveItem> items;
                try { items = saveable.Save(); }
                catch (System.Exception e) { Debug.LogWarning($"[PlayerDataSerializer] Save failed for {content.Id}: {e.Message}"); continue; }
                if (items == null) continue;

                var cc = new CloudContent { id = content.Id };
                foreach (var it in items) cc.fields.Add(ToField(it));
                if (cc.fields.Count > 0) blob.contents.Add(cc);
            }
            return JsonUtility.ToJson(blob);
        }

        /// <summary>Apply a JSON blob onto the Domain's ISaveables. Returns the number of contents applied.</summary>
        public static int ApplyToDomain(IDomain domain, string json)
        {
            if (domain == null || string.IsNullOrEmpty(json)) return 0;

            CloudSaveBlob blob;
            try { blob = JsonUtility.FromJson<CloudSaveBlob>(json); }
            catch (System.Exception e) { Debug.LogWarning($"[PlayerDataSerializer] Bad JSON: {e.Message}"); return 0; }
            if (blob?.contents == null) return 0;

            var saveables = domain.GetAll<ISaveable>();
            int applied = 0;
            foreach (var cc in blob.contents)
            {
                if (cc == null || cc.fields == null) continue;
                var match = saveables.FirstOrDefault(s => s is IContent c && c.Id == cc.id);
                if (match == null) continue;

                var items = cc.fields.Select(FromField).ToList();
                try { match.Load(items); applied++; }
                catch (System.Exception e) { Debug.LogWarning($"[PlayerDataSerializer] Load failed for {cc.id}: {e.Message}"); }
            }
            return applied;
        }

        private static CloudField ToField(SaveItem it)
        {
            var f = new CloudField { key = it.key, type = (int)it.valueType };
            switch (it.valueType)
            {
                case ValueSaveType.Bool: f.val = it.boolValue ? "1" : "0"; break;
                case ValueSaveType.Int: f.val = it.intValue.ToString(CultureInfo.InvariantCulture); break;
                case ValueSaveType.Float: f.val = it.floatValue.ToString("R", CultureInfo.InvariantCulture); break;
                case ValueSaveType.Double: f.val = it.doubleValue.ToString("R", CultureInfo.InvariantCulture); break;
                default: f.val = it.stringValue ?? string.Empty; break;
            }
            return f;
        }

        private static SaveItem FromField(CloudField f)
        {
            var ci = CultureInfo.InvariantCulture;
            switch ((ValueSaveType)f.type)
            {
                case ValueSaveType.Bool:
                    return new SaveItem(f.key, f.val == "1" || f.val == "true" || f.val == "True");
                case ValueSaveType.Int:
                    int.TryParse(f.val, NumberStyles.Integer, ci, out var iv);
                    return new SaveItem(f.key, iv);
                case ValueSaveType.Float:
                    float.TryParse(f.val, NumberStyles.Float, ci, out var fv);
                    return new SaveItem(f.key, fv);
                case ValueSaveType.Double:
                    double.TryParse(f.val, NumberStyles.Float, ci, out var dv);
                    return new SaveItem(f.key, dv);
                default:
                    return new SaveItem(f.key, f.val ?? string.Empty);
            }
        }
    }
}
