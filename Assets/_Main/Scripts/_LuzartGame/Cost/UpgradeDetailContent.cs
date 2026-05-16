using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
namespace Luzart
{
    public class UpgradeDetailContent
    {
        public static string GetUpgradeDetailModifierPair(IReadOnlyList<IModifierPair> modifierFactors)
        {
            StringBuilder sb = new StringBuilder();
            // Modifiers
            for (int i = 0; i < modifierFactors.Count; i++)
            {
                var modifier = modifierFactors[i];
                string label = modifier.Factor.Definition.DisplayName.Value;
                double value = modifier.Factor.Value.Value;
                sb.AppendLine(FormatSafe(label, value));
            }
            return sb.ToString();
            string FormatSafe(string template, object valueConvert)
            {
                if (string.IsNullOrEmpty(template))
                    return valueConvert?.ToString() ?? "";
                if (template.Contains("{0}"))
                    return string.Format(template, valueConvert);
                return $"{template} : {valueConvert}";
            }
        }
    }
}
