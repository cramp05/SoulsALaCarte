using UnityEngine;
using SoulsALaCarte.Core;

namespace SoulsALaCarte.Cooking
{
    public class ManagementBoard : MonoBehaviour
    {
        public void OpenBoard()
        {
            SoundManager.Instance?.PlayClick();
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentState == GameManager.GameState.InGameRound)
                {
                    UI.UIManager.Instance?.ShowNotification("¡Termina el servicio actual antes de seleccionar otro nivel!");
                    return;
                }
                GameManager.Instance.OpenLevelSelector();
            }
        }
    }
}
