using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SoulsALaCarte.Core;

namespace SoulsALaCarte.UI
{
    public class LevelSelectUI : MonoBehaviour
    {
        public static LevelSelectUI Instance { get; private set; }

        [Header("Cabecera")]
        public Text headerMoneyText;
        public Text headerLevelCountText;

        [Header("Selector de Niveles")]
        public Transform levelCardsParent;

        [Header("Inspector de Nivel")]
        public Text selectedLevelTitleText;
        public Text selectedLevelSubText;
        public Text selectedLevelDescText;
        public Text selectedLevelRewardsText;
        public Text selectedLevelConditionsText;
        public Button startLevelButton;
        public Button closeButton;

        private int inspectorSelectedLevel = 1;

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
            if (headerLevelCountText != null) headerLevelCountText.text = $"Nivel Desbloqueado: {gm.highestUnlockedLevel} / 10";

            var cfg = LevelDatabase.Get(inspectorSelectedLevel);
            if (cfg != null)
            {
                if (selectedLevelTitleText != null) selectedLevelTitleText.text = $"NIVEL {cfg.levelIndex}: {cfg.title.ToUpper()}";
                if (selectedLevelSubText != null) selectedLevelSubText.text = cfg.subtitle;
                if (selectedLevelDescText != null) selectedLevelDescText.text = cfg.description;
                if (selectedLevelRewardsText != null) selectedLevelRewardsText.text = $"Recompensa base: +{cfg.baseRewardCoins:N0} óbolos\nEstrellas: 1★ ({cfg.star1Points}) | 2★ ({cfg.star2Points}) | 3★ ({cfg.star3Points})";
                if (selectedLevelConditionsText != null) selectedLevelConditionsText.text = $"Condiciones: {cfg.conditionsInfo}";

                bool isUnlocked = inspectorSelectedLevel <= gm.highestUnlockedLevel;
                if (startLevelButton != null)
                {
                    startLevelButton.interactable = isUnlocked;
                    startLevelButton.onClick.RemoveAllListeners();
                    startLevelButton.onClick.AddListener(() =>
                    {
                        SoundManager.Instance?.PlayClick();
                        gameObject.SetActive(false);
                        gm.StartLevel(inspectorSelectedLevel);
                    });
                }
            }

            if (levelCardsParent != null)
            {
                for (int i = 0; i < levelCardsParent.childCount; i++)
                {
                    int lvlIdx = i + 1;
                    var card = levelCardsParent.GetChild(i);
                    bool unlocked = lvlIdx <= gm.highestUnlockedLevel;

                    var btn = card.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.interactable = unlocked;
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() =>
                        {
                            SelectLevel(lvlIdx);
                        });
                    }

                    var texts = card.GetComponentsInChildren<Text>(true);
                    int stars = gm.levelStars.ContainsKey(lvlIdx) ? gm.levelStars[lvlIdx] : 0;
                    string starStr = (stars > 0) ? new string('★', stars) : (unlocked ? "☆☆☆" : "🔒 BLOQUEADO");

                    if (texts.Length > 0) texts[0].text = $"Nivel {lvlIdx}\n{(unlocked ? LevelDatabase.Get(lvlIdx).title : "Bloqueado")}\n{starStr}";
                }
            }
        }

        public void SelectLevel(int levelIdx)
        {
            inspectorSelectedLevel = levelIdx;
            SoundManager.Instance?.PlayClick();
            RefreshDisplay();
        }
    }
}
