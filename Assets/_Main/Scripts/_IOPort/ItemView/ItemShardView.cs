namespace Luzart
{
    public class ItemShardView : ItemDefinitionView
    {
        protected override void SetText()
        {
            if (_txt == null)
            {
                return;
            }
            if (Data.Amount == -1)
            {
                _txt.gameObject.SetActive(false);
                return;
            }
            _txt.gameObject.SetActive(true);
            string rarityStr = ExtractRarity(Data.ResourcePool.Definition.DisplayName);
            _txt.text = $"{_preStr}{rarityStr} {Data.Amount}{_endStr}";
        }

        private static string ExtractRarity(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            string[] parts = input.Split(' ');

            foreach (var part in parts)
            {
                if (part == "Epic" || part == "Legend" || part == "Rare")
                {
                    return part;
                }
            }

            return null;
        }
    }
}
