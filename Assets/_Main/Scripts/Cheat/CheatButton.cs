using MyGame.Scripts.CheatManagement;
using UnityEngine;

public class CheatButton : MonoBehaviour
{
    private int count;
    [SerializeField] private CheatPanel cheatPanel;

    public void OnClickCheat()
    {
        count++;
        if (count >= 5)
        {
            count = 0;
            if (cheatPanel != null)
                cheatPanel.OnOffCheat();
        }
    }
}