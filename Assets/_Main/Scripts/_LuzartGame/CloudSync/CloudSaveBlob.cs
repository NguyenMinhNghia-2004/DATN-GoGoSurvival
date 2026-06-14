using System.Collections.Generic;

namespace Luzart
{
    /// <summary>
    /// The CENTRAL player-data blob — a backend-agnostic, JsonUtility-friendly snapshot of every
    /// <see cref="ISaveable"/> in the Domain (currencies, item unlocks, item levels, equipped items,
    /// …). One blob = one player's whole meta-progression. Produced/consumed by
    /// <see cref="PlayerDataSerializer"/> and shipped as a JSON string through any
    /// <see cref="ICloudSaveProvider"/>.
    ///
    /// <para>Deliberately uses only primitive-string fields so it round-trips cleanly through
    /// <c>UnityEngine.JsonUtility</c> (no Json.NET in the project) and through any backend.</para>
    /// </summary>
    [System.Serializable]
    public class CloudSaveBlob
    {
        public int version = 1;
        public List<CloudContent> contents = new List<CloudContent>();
    }

    /// <summary>One saveable content: its stable <see cref="IContent.Id"/> + its fields.</summary>
    [System.Serializable]
    public class CloudContent
    {
        public string id;
        public List<CloudField> fields = new List<CloudField>();
    }

    /// <summary>One persisted field. <see cref="type"/> mirrors <see cref="ValueSaveType"/>;
    /// <see cref="val"/> is the invariant-culture string form of the value.</summary>
    [System.Serializable]
    public class CloudField
    {
        public string key;
        public int type; // ValueSaveType: Int=0, Float=1, String=2, Bool=3, Double=4
        public string val;
    }
}
