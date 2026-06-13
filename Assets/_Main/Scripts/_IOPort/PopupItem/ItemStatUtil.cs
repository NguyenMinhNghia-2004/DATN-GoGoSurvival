using System.Globalization;

namespace Luzart
{
    /// <summary>
    /// Derives the ATK / HP an ItemConfig contributes at its current level. Each item grants a
    /// single stat (the other is 0), so this lets any view show which stat an item boosts.
    /// The stat is identified from the modifier definition/asset name ("...ATK..." / "...HPMax...").
    /// </summary>
    public static class ItemStatUtil
    {
        public static void GetAtkHp(ItemConfig item, out double atk, out double hp)
        {
            atk = 0; hp = 0;
            if (item == null) return;
            var pairs = item.GetCurrentModifierPairs();
            if (pairs == null) return;
            for (int i = 0; i < pairs.Count; i++)
            {
                var pair = pairs[i];
                if (pair == null || pair.Factor == null) continue;
                double val = pair.Factor.Value != null ? pair.Factor.Value.Value : 0;
                string key = StatKey(pair);
                if (key.Contains("ATK")) atk += val;
                else if (key.Contains("HP")) hp += val;
            }
        }

        public static string Fmt(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        private static string StatKey(IModifierPair pair)
        {
            string s = "";
            var d = pair.Factor != null ? pair.Factor.Definition as UnityEngine.Object : null;
            if (d != null) s += d.name + " ";
            if (pair.Modifier != null)
            {
                var md = pair.Modifier.Definition as UnityEngine.Object;
                if (md != null) s += md.name + " ";
                var mo = pair.Modifier as UnityEngine.Object;
                if (mo != null) s += mo.name + " ";
            }
            return s;
        }
    }
}
