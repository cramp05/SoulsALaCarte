using UnityEngine;
using UnityEngine.UI;
using SoulsALaCarte.Core;

namespace SoulsALaCarte.UI
{
    public class ShopUI : MonoBehaviour
    {
        public static ShopUI Instance { get; private set; }

        [Header("Cabecera de Economía")]
        public Text headerMoneyText;
        public Text headerTargetText;
        public Slider headerFreedomSlider;
        public Text headerPercentageText;

        [Header("Contenedor de Mejoras")]
        public Transform shopCardsParent;

        [Header("Ayudante Giuseppe")]
        public Text giuseppeStatusText;
        public Button hireGiuseppeButton;
        public Text hireGiuseppeButtonText;

        [Header("Soborno y Libertad")]
        public Button bribeFreedomButton;
        public Text bribeStatusText;

        [Header("Navegación")]
        public Button closeButton;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            RefreshDisplay();
        }

        public void OpenPanel()
        {
            gameObject.SetActive(true);
            RefreshDisplay();
            SoundManager.Instance?.PlayClick();
        }

        public void ClosePanel()
        {
            gameObject.SetActive(false);
            SoundManager.Instance?.PlayClick();
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.InGameRound)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.BetweenRounds);
            }
        }

        public void RefreshDisplay()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (headerMoneyText != null) headerMoneyText.text = $"{gm.totalCoins:N0} ❖";
            if (headerTargetText != null) headerTargetText.text = $"Soborno Exigido: {gm.freedomTarget:N0} ❖";
            float freedomRatio = Mathf.Clamp01((float)gm.totalCoins / gm.freedomTarget);
            if (headerFreedomSlider != null) headerFreedomSlider.value = freedomRatio;
            if (headerPercentageText != null) headerPercentageText.text = $"{(freedomRatio * 100f):F0}%";

            RefreshShopCards();
            RefreshGiuseppe();
            RefreshBribe();
        }

        private void RefreshShopCards()
        {
            var gm = GameManager.Instance;
            if (gm == null || shopCardsParent == null) return;

            UpdateShopCard(shopCardsParent.Find("Card_Speed"), "Speed", "Velocidad de Movimiento", "Francis se desplaza más rápido por los vagones.", gm.speedLevel, 3, gm.GetUpgradeCost("Speed"));
            UpdateShopCard(shopCardsParent.Find("Card_CookSpeed"), "CookSpeed", "Tiempo de Cocción", "Los calderos FoodLicker cocinan más rápido.", gm.cookSpeedLevel, 3, gm.GetUpgradeCost("CookSpeed"));
            UpdateShopCard(shopCardsParent.Find("Card_Plates"), "Plates", "Más Platos Limpios", "Aumenta los platos iniciales disponibles en la cocina.", gm.extraPlatesLevel, 2, gm.GetUpgradeCost("Plates"));
            UpdateShopCard(shopCardsParent.Find("Card_Drinks"), "Drinks", "Mejora de Bebidas", "Los surtidores del DrinkLin llenan vasos en menos tiempo.", gm.drinkSpeedLevel, 3, gm.GetUpgradeCost("Drinks"));
            UpdateShopCard(shopCardsParent.Find("Card_Tips"), "Tips", "Ganancia Adicional", "Los demonios dejan propinas más cuantiosas.", gm.extraTipsLevel, 3, gm.GetUpgradeCost("Tips"));
            UpdateShopCard(shopCardsParent.Find("Card_Color"), "Color", "Placa de Color", "Iluminación mística para distinguir almas rápidamente.", gm.colorPlatesActive ? 1 : 0, 1, gm.GetUpgradeCost("Color"));
        }

        private void UpdateShopCard(Transform card, string upgradeId, string title, string desc, int currentLvl, int maxLvl, int cost)
        {
            if (card == null) return;
            var gm = GameManager.Instance;

            var texts = card.GetComponentsInChildren<Text>(true);
            var btn = card.GetComponentInChildren<Button>(true);

            bool isMaxed = currentLvl >= maxLvl;
            bool canAfford = gm != null && gm.totalCoins >= cost && !isMaxed;

            if (texts.Length >= 2)
            {
                texts[0].text = $"{title}\nNivel {currentLvl}/{maxLvl}";
                texts[1].text = isMaxed ? "¡MEJORA AL MÁXIMO!" : $"{desc}\nPrecio: {cost:N0} ❖";
            }

            if (btn != null)
            {
                btn.interactable = canAfford;
                var btnText = btn.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = isMaxed ? "Comprado" : "Comprar";
                }

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    if (gm.BuyUpgrade(upgradeId))
                    {
                        RefreshDisplay();
                    }
                });
            }
        }

        private void RefreshGiuseppe()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (giuseppeStatusText != null)
            {
                giuseppeStatusText.text = gm.giuseppeHired
                    ? "¡GIUSEPPE ESTÁ TRABAJANDO EN TU COCINA!\nRecoge los platos sucios de las mesas del comedor y los friega en el lavadero automáticamente."
                    : "Giuseppe es un experto cocinero italiano atrapado en la bolsa mágica de Migafees. Si lo contratas, recogerá los platos sucios de las mesas y los lavará por ti.";
            }

            if (hireGiuseppeButton != null)
            {
                int giuseppeCost = gm.GetUpgradeCost("Giuseppe");
                bool canHire = !gm.giuseppeHired && gm.totalCoins >= giuseppeCost;
                hireGiuseppeButton.interactable = canHire;

                if (hireGiuseppeButtonText != null)
                {
                    hireGiuseppeButtonText.text = gm.giuseppeHired ? "¡Contratado!" : $"Contratar a Giuseppe ({giuseppeCost:N0} ❖)";
                }

                hireGiuseppeButton.onClick.RemoveAllListeners();
                hireGiuseppeButton.onClick.AddListener(() =>
                {
                    if (gm.BuyUpgrade("Giuseppe"))
                    {
                        RefreshDisplay();
                    }
                });
            }
        }

        private void RefreshBribe()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            bool canBribe = gm.totalCoins >= gm.freedomTarget;

            if (bribeStatusText != null)
            {
                bribeStatusText.text = canBribe
                    ? "¡Tienes suficientes óbolos para pagar el soborno a Migafees y escapar del Infierno!"
                    : $"Te faltan {(gm.freedomTarget - gm.totalCoins):N0} óbolos para sobornar al maquinista.";
            }

            if (bribeFreedomButton != null)
            {
                bribeFreedomButton.interactable = canBribe;
                bribeFreedomButton.onClick.RemoveAllListeners();
                bribeFreedomButton.onClick.AddListener(() =>
                {
                    gameObject.SetActive(false);
                    gm.BribeMigafees();
                });
            }
        }
    }
}
