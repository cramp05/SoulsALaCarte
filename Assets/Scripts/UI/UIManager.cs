using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SoulsALaCarte.Core;

namespace SoulsALaCarte.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Paneles Principales Separados")]
        public GameObject mainMenuPanel;
        public GameObject gameSelectPanel;
        public GameObject storyDialoguePanel;
        public GameObject levelSelectPanel;
        public GameObject shopPanel;
        public GameObject hudPanel;
        public GameObject pausePanel;
        public GameObject resultsPanel;
        public GameObject victoryPanel;
        public GameObject optionsPanel;
        public GameObject creditsPanel;

        [Header("HUD Elementos")]
        public Text hudClassTitleText;
        public Text hudClassPhraseText;
        public Text hudTimerText;
        public Text hudMoneyText;
        public Slider hudFreedomSlider;
        public Text hudFailuresText;
        public Text hudCarriedItemText;
        public Text hudTargetNameText;
        public Text hudActionPromptText;
        public Text hudPromptText;
        public Transform hudOrdersContainer;
        public GameObject blurSprayOverlay;
        public GameObject bossWarningBanner;
        public Text bossWarningText;
        public Text hudNotificationText;

        [Header("Banner Tutorial")]
        public GameObject hudTutorialBannerObj;
        public Text hudTutorialBannerText;

        [Header("Diálogo")]
        public Text dialogueSpeakerText;
        public Text dialogueContentText;
        public Button dialogueNextButton;
        public Button dialogueBribeButton;

        [Header("Resultados")]
        public Text resultsTitleText;
        public Text resultsEarnedText;
        public Text resultsTipsText;
        public Text resultsPenaltiesText;
        public Text resultsOrdersText;
        public Text resultsStarsText;

        [Header("Opciones")]
        public Slider optionsVolumeSlider;

        private float blurTimer = 0f;
        private float notificationTimer = 0f;
        private GameManager.GameState previousStateBeforeOptions = GameManager.GameState.MainMenu;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (optionsVolumeSlider != null)
            {
                optionsVolumeSlider.onValueChanged.AddListener((v) =>
                {
                    SoundManager.Instance?.SetVolume(v);
                });
            }
        }

        private void Update()
        {
            if (blurTimer > 0f)
            {
                blurTimer -= Time.deltaTime;
                if (blurSprayOverlay != null) blurSprayOverlay.SetActive(true);
                if (blurTimer <= 0f && blurSprayOverlay != null)
                {
                    blurSprayOverlay.SetActive(false);
                }
            }

            if (notificationTimer > 0f)
            {
                notificationTimer -= Time.deltaTime;
                if (notificationTimer <= 0f && hudNotificationText != null)
                {
                    hudNotificationText.gameObject.SetActive(false);
                }
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.InGameRound)
            {
                UpdateHUDStats();
            }
        }

        public void OnGameStateChanged(GameManager.GameState state)
        {
            HideAllPanels();

            switch (state)
            {
                case GameManager.GameState.MainMenu:
                    if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                    break;

                case GameManager.GameState.StoryIntro:
                    ShowStoryTutorial();
                    break;

                case GameManager.GameState.BetweenRounds:
                    // En descanso entre niveles, se ocultan paneles para caminar por el tren libremente
                    if (hudPanel != null) hudPanel.SetActive(true);
                    if (hudOrdersContainer != null) hudOrdersContainer.gameObject.SetActive(false);
                    if (hudTimerText != null) hudTimerText.text = "Entre Niveles: Paseo Libre";
                    SetInteractionInfo("Manos vacías", "Tren Soul Express", "[E] en Tablero para elegir nivel | [E] en Migafees para comprar mejoras");
                    break;

                case GameManager.GameState.LevelSelect:
                    if (levelSelectPanel != null)
                    {
                        levelSelectPanel.SetActive(true);
                        var ls = levelSelectPanel.GetComponent<LevelSelectUI>();
                        if (ls != null) ls.RefreshDisplay();
                    }
                    break;

                case GameManager.GameState.Shop:
                    if (shopPanel != null)
                    {
                        shopPanel.SetActive(true);
                        var sp = shopPanel.GetComponent<ShopUI>();
                        if (sp != null) sp.RefreshDisplay();
                    }
                    break;

                case GameManager.GameState.InGameRound:
                    if (hudPanel != null) hudPanel.SetActive(true);
                    if (hudOrdersContainer != null) hudOrdersContainer.gameObject.SetActive(true);
                    SetupHUDForCurrentLevel();
                    break;

                case GameManager.GameState.Paused:
                    if (pausePanel != null) pausePanel.SetActive(true);
                    break;

                case GameManager.GameState.RoundResults:
                    if (resultsPanel != null)
                    {
                        resultsPanel.SetActive(true);
                        SetupResultsPanel();
                    }
                    break;

                case GameManager.GameState.VictoryEscaped:
                    if (victoryPanel != null) victoryPanel.SetActive(true);
                    break;
            }
        }

        public void HideAllPanels()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (gameSelectPanel != null) gameSelectPanel.SetActive(false);
            if (storyDialoguePanel != null) storyDialoguePanel.SetActive(false);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
            if (shopPanel != null) shopPanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (resultsPanel != null) resultsPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
        }

        // --- Diálogos & Historia ---
        public void ShowStoryTutorial()
        {
            string speaker = "Lord Mr. Sir Dr. Migafees Do Santos III jr (Maquinista)";
            string story = "¡Bienvenido al Soul Express de Fenre, Francis!\n\nVendiste tu alma por dinero, te atragantaste con una gamba en un restaurante y ahora te toca pagar tu deuda cocinando en el Infierno.\n\nPrepara las recetas con las almas y los platos en los calderos FoodLicker, sirve las bebidas en el DrinkLin y mantén la vajilla limpia.\n\nSi consigues reunir 50.000 óbolos para sobornarme... comprarás tu libertad y podrás escapar al Paraíso.\n\n¡Empieza con el primer servicio!";

            ShowDialogue(speaker, story, false);
        }

        public void ShowDialogue(string speaker, string content, bool canBribe)
        {
            HideAllPanels();
            if (storyDialoguePanel != null) storyDialoguePanel.SetActive(true);

            if (dialogueSpeakerText != null) dialogueSpeakerText.text = speaker;
            if (dialogueContentText != null) dialogueContentText.text = content;

            if (dialogueNextButton != null)
            {
                dialogueNextButton.gameObject.SetActive(!canBribe);
                dialogueNextButton.onClick.RemoveAllListeners();
                dialogueNextButton.onClick.AddListener(() =>
                {
                    SoundManager.Instance?.PlayClick();
                    if (GameManager.Instance != null && GameManager.Instance.highestUnlockedLevel == 1)
                    {
                        GameManager.Instance.StartLevel(1);
                    }
                    else
                    {
                        GameManager.Instance?.SetGameState(GameManager.GameState.BetweenRounds);
                    }
                });
            }

            if (dialogueBribeButton != null)
            {
                dialogueBribeButton.gameObject.SetActive(canBribe);
                dialogueBribeButton.onClick.RemoveAllListeners();
                dialogueBribeButton.onClick.AddListener(() =>
                {
                    SoundManager.Instance?.PlayClick();
                    GameManager.Instance?.BribeMigafees();
                });
            }
        }

        // --- HUD ---
        private void SetupHUDForCurrentLevel()
        {
            var cfg = GameManager.Instance?.CurrentLevelConfig;
            if (cfg == null) return;

            if (hudClassTitleText != null) hudClassTitleText.text = $"Nivel {cfg.levelIndex}: {cfg.subtitle}";
            if (hudClassPhraseText != null) hudClassPhraseText.text = cfg.catchphrase;

            if (bossWarningBanner != null) bossWarningBanner.SetActive(false);
            if (blurSprayOverlay != null) blurSprayOverlay.SetActive(false);
            blurTimer = 0f;
        }

        public void SetTutorialHint(string hint)
        {
            if (hudTutorialBannerObj != null)
            {
                hudTutorialBannerObj.SetActive(true);
            }
            if (hudTutorialBannerText != null)
            {
                hudTutorialBannerText.text = hint;
            }
        }

        public void ClearTutorialHint()
        {
            if (hudTutorialBannerObj != null)
            {
                hudTutorialBannerObj.SetActive(false);
            }
        }

        public void AddHUDOrder(int ticket, string name, int tableNum) { }
        public void RemoveHUDOrder(int ticket) { }

        private void UpdateHUDStats()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (hudTimerText != null)
            {
                int seconds = Mathf.Max(0, Mathf.CeilToInt(gm.roundTimeRemaining));
                hudTimerText.text = $"Tiempo: {seconds / 60:D2}:{seconds % 60:D2}";
            }

            if (hudMoneyText != null)
            {
                int currentRoundProfit = gm.roundCoinsEarned + gm.roundTipsEarned - gm.roundPenaltiesSuffered;
                hudMoneyText.text = $"Ronda: +{currentRoundProfit:N0} ❖  |  Total: {gm.totalCoins:N0} ❖ / {gm.freedomTarget:N0} ❖";
            }

            if (hudFreedomSlider != null)
            {
                hudFreedomSlider.value = Mathf.Clamp01((float)gm.totalCoins / gm.freedomTarget);
            }

            if (hudFailuresText != null)
            {
                int fails = gm.ordersFailed;
                hudFailuresText.text = $"Fallos: {fails}/{GameManager.MAX_ALLOWED_FAILURES} (Con {GameManager.MAX_ALLOWED_FAILURES} pierdes)";
            }

            UpdateHUDOrders();
        }

        private void UpdateHUDOrders()
        {
            if (hudOrdersContainer == null) return;

            var diningMgr = FindAnyObjectByType<SoulsALaCarte.Dining.DiningCarManager>();
            if (diningMgr == null) return;

            var activeTables = new List<SoulsALaCarte.Dining.CustomerTable>();
            foreach (var t in diningMgr.tables)
            {
                if (t != null && t.currentCustomer != null && t.currentCustomer.state == SoulsALaCarte.Dining.CustomerState.WaitingForFood)
                {
                    activeTables.Add(t);
                }
            }

            int childCount = hudOrdersContainer.childCount;
            for (int i = 0; i < Mathf.Max(childCount, activeTables.Count); i++)
            {
                if (i < activeTables.Count)
                {
                    Transform card = (i < childCount) ? hudOrdersContainer.GetChild(i) : null;
                    if (card == null)
                    {
                        card = CreateOrderCardUI(hudOrdersContainer, i);
                    }
                    card.gameObject.SetActive(true);
                    UpdateOrderCardUI(card, activeTables[i]);
                }
                else
                {
                    if (i < childCount)
                    {
                        hudOrdersContainer.GetChild(i).gameObject.SetActive(false);
                    }
                }
            }
        }

        private Transform CreateOrderCardUI(Transform parent, int index)
        {
            Font font = SoulsALaCarte.Builder.SoulsGameBuilder.GetSafeFont();
            GameObject card = new GameObject($"OrderCard_{index}");
            card.transform.SetParent(parent, false);

            var cImg = card.AddComponent<Image>();
            cImg.color = new Color(0.14f, 0.10f, 0.18f, 0.92f);

            var cRect = card.GetComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(250f, 60f);

            var strip = new GameObject("Strip");
            strip.transform.SetParent(card.transform, false);
            var sImg = strip.AddComponent<Image>();
            sImg.color = Color.yellow;
            var sRect = strip.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 0f);
            sRect.anchorMax = new Vector2(0f, 1f);
            sRect.pivot = new Vector2(0f, 0.5f);
            sRect.sizeDelta = new Vector2(6f, 0f);
            sRect.anchoredPosition = Vector2.zero;

            var headObj = new GameObject("Header");
            headObj.transform.SetParent(card.transform, false);
            var hText = headObj.AddComponent<Text>();
            hText.font = font;
            hText.fontSize = 13;
            hText.fontStyle = FontStyle.Bold;
            hText.color = new Color(1f, 0.85f, 0.2f);
            hText.alignment = TextAnchor.MiddleLeft;
            var hRect = headObj.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 0.5f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.offsetMin = new Vector2(12f, 0f);
            hRect.offsetMax = new Vector2(-6f, -3f);

            var itemObj = new GameObject("ItemName");
            itemObj.transform.SetParent(card.transform, false);
            var iText = itemObj.AddComponent<Text>();
            iText.font = font;
            iText.fontSize = 14;
            iText.fontStyle = FontStyle.Bold;
            iText.color = Color.white;
            iText.alignment = TextAnchor.MiddleLeft;
            var iRect = itemObj.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0f, 0.22f);
            iRect.anchorMax = new Vector2(1f, 0.65f);
            iRect.offsetMin = new Vector2(12f, 0f);
            iRect.offsetMax = new Vector2(-6f, 0f);

            var pBarBg = new GameObject("PatienceBg");
            pBarBg.transform.SetParent(card.transform, false);
            var pbImg = pBarBg.AddComponent<Image>();
            pbImg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
            var pbRect = pBarBg.GetComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0f, 0f);
            pbRect.anchorMax = new Vector2(1f, 0f);
            pbRect.pivot = new Vector2(0.5f, 0f);
            pbRect.sizeDelta = new Vector2(-24f, 6f);
            pbRect.anchoredPosition = new Vector2(0f, 4f);

            var pBarFill = new GameObject("PatienceFill");
            pBarFill.transform.SetParent(pBarBg.transform, false);
            var pfImg = pBarFill.AddComponent<Image>();
            pfImg.color = Color.green;
            var pfRect = pBarFill.GetComponent<RectTransform>();
            pfRect.anchorMin = new Vector2(0f, 0f);
            pfRect.anchorMax = new Vector2(1f, 1f);
            pfRect.pivot = new Vector2(0f, 0.5f);
            pfRect.offsetMin = Vector2.zero;
            pfRect.offsetMax = Vector2.zero;

            return card.transform;
        }

        private void UpdateOrderCardUI(Transform card, SoulsALaCarte.Dining.CustomerTable table)
        {
            var cust = table.currentCustomer;
            if (cust == null) return;

            string orderTitle = cust.isOrderTaken ? cust.GetOrderDisplayName() : "[E] Tomar Comanda";
            Color orderCol = cust.isOrderTaken ? cust.GetOrderColor() : Color.yellow;

            var strip = card.Find("Strip")?.GetComponent<Image>();
            if (strip != null) strip.color = orderCol;

            var hText = card.Find("Header")?.GetComponent<Text>();
            if (hText != null)
            {
                string demonName = cust.isBoss ? $"¡JEFE {cust.bossType}!" : cust.demonClass.ToString();
                hText.text = $"#{cust.ticketNumber} Mesa {table.tableNumber} ({demonName})";
            }

            var iText = card.Find("ItemName")?.GetComponent<Text>();
            if (iText != null)
            {
                iText.text = orderTitle;
            }

            var pFill = card.Find("PatienceBg/PatienceFill")?.GetComponent<RectTransform>();
            var pfImg = pFill?.GetComponent<Image>();
            if (pFill != null && cust.totalPatience > 0f)
            {
                float ratio = Mathf.Clamp01(cust.currentPatience / cust.totalPatience);
                pFill.anchorMax = new Vector2(ratio, 1f);

                if (pfImg != null)
                {
                    if (ratio > 0.5f)
                        pfImg.color = Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f);
                    else
                        pfImg.color = Color.Lerp(Color.red, Color.yellow, ratio * 2f);
                }
            }
        }

        public void SetInteractionInfo(string carried, string targetName, string action)
        {
            if (hudCarriedItemText != null) hudCarriedItemText.text = $"En manos: {carried}";
            if (hudTargetNameText != null) hudTargetNameText.text = $"Frente a: {targetName}";
            if (hudActionPromptText != null) hudActionPromptText.text = action;

            if (hudPromptText != null)
            {
                hudPromptText.text = $"Llevas: {carried}\nFrente a: {targetName}\n{action}";
            }
        }

        public void TriggerBlurSpray(float duration)
        {
            blurTimer = duration;
            if (blurSprayOverlay != null) blurSprayOverlay.SetActive(true);
            ShowNotification("¡Spray a los ojos de Tonius! Visión nublada durante 5 segundos.");
        }

        public void ShowBossWarning(string bossName)
        {
            if (bossWarningBanner != null)
            {
                bossWarningBanner.SetActive(true);
                if (bossWarningText != null) bossWarningText.text = $"¡ALERTA DE JEFE!\n{bossName} ha entrado al Soul Express";
                SoundManager.Instance?.PlayBoss();
                StartCoroutine(HideBossWarningRoutine(4f));
            }
        }

        private IEnumerator HideBossWarningRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (bossWarningBanner != null) bossWarningBanner.SetActive(false);
        }

        public void ShowNotification(string msg)
        {
            if (hudNotificationText != null)
            {
                hudNotificationText.gameObject.SetActive(true);
                hudNotificationText.text = msg;
                notificationTimer = 3.0f;
            }
        }

        // --- Resultados ---
        private void SetupResultsPanel()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.roundFailed)
            {
                if (resultsTitleText != null) resultsTitleText.text = "¡RONDA FALLIDA! COMANDAS PERDIDAS";
                if (resultsEarnedText != null) resultsEarnedText.text = $"Ganancias base: +{gm.roundCoinsEarned:N0} ❖";
                if (resultsTipsText != null) resultsTipsText.text = $"Propinas: +{gm.roundTipsEarned:N0} ❖";
                if (resultsPenaltiesText != null) resultsPenaltiesText.text = $"Sanciones: -{gm.roundPenaltiesSuffered:N0} ❖";
                if (resultsOrdersText != null) resultsOrdersText.text = $"Pedidos entregados: {gm.ordersCompleted}  |  Fallidos: {gm.ordersFailed}";
                if (resultsStarsText != null) resultsStarsText.text = "Calificación: ☆ ☆ ☆ (¡Inténtalo de nuevo!)";
            }
            else
            {
                int stars = gm.levelStars.ContainsKey(gm.currentLevelIndex) ? gm.levelStars[gm.currentLevelIndex] : 0;

                if (resultsTitleText != null)
                    resultsTitleText.text = (stars > 0) ? $"¡Nivel {gm.currentLevelIndex} Completado con Éxito!" : $"Nivel {gm.currentLevelIndex} Finalizado";

                if (resultsEarnedText != null) resultsEarnedText.text = $"Ganancias base: +{gm.roundCoinsEarned:N0} ❖";
                if (resultsTipsText != null) resultsTipsText.text = $"Propinas: +{gm.roundTipsEarned:N0} ❖";
                if (resultsPenaltiesText != null) resultsPenaltiesText.text = $"Sanciones: -{gm.roundPenaltiesSuffered:N0} ❖";
                if (resultsOrdersText != null) resultsOrdersText.text = $"Pedidos entregados: {gm.ordersCompleted}  |  Fallidos: {gm.ordersFailed}";

                if (resultsStarsText != null)
                {
                    string starStr = "";
                    for (int i = 0; i < 3; i++)
                    {
                        starStr += (i < stars) ? " ★" : " ☆";
                    }
                    resultsStarsText.text = $"Calificación:{starStr}";
                }
            }
        }

        // --- Botones de Menú ---
        public void OnClickPlay()
        {
            SoundManager.Instance?.PlayClick();
            if (gameSelectPanel != null)
            {
                mainMenuPanel.SetActive(false);
                gameSelectPanel.SetActive(true);
            }
            else
            {
                GameManager.Instance?.ContinueGame();
            }
        }

        public void OnClickCoopInfo()
        {
            SoundManager.Instance?.PlayClick();
            ShowNotification("Modo Cooperativo Local: Se habilitará en la próxima actualización.");
        }

        public void OnClickNewGame()
        {
            SoundManager.Instance?.PlayClick();
            GameManager.Instance?.StartNewGame();
        }

        public void OnClickContinue()
        {
            SoundManager.Instance?.PlayClick();
            GameManager.Instance?.ContinueGame();
        }

        public void OnClickOptions()
        {
            SoundManager.Instance?.PlayClick();
            if (GameManager.Instance != null)
            {
                previousStateBeforeOptions = GameManager.Instance.CurrentState;
            }
            HideAllPanels();
            if (optionsPanel != null) optionsPanel.SetActive(true);
        }

        public void OnClickCloseOptions()
        {
            SoundManager.Instance?.PlayClick();
            HideAllPanels();
            if (previousStateBeforeOptions == GameManager.GameState.Paused && pausePanel != null)
            {
                pausePanel.SetActive(true);
            }
            else if (previousStateBeforeOptions == GameManager.GameState.InGameRound && hudPanel != null)
            {
                hudPanel.SetActive(true);
            }
            else if (previousStateBeforeOptions == GameManager.GameState.BetweenRounds && hudPanel != null)
            {
                hudPanel.SetActive(true);
            }
            else if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }
        }

        public void OnClickCredits()
        {
            SoundManager.Instance?.PlayClick();
            HideAllPanels();
            if (creditsPanel != null) creditsPanel.SetActive(true);
        }

        public void OnClickCloseCredits()
        {
            SoundManager.Instance?.PlayClick();
            HideAllPanels();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }

        public void OnClickResume()
        {
            SoundManager.Instance?.PlayClick();
            GameManager.Instance?.ResumeGame();
        }

        public void OnClickAbandonRound()
        {
            SoundManager.Instance?.PlayClick();
            GameManager.Instance?.SetGameState(GameManager.GameState.BetweenRounds);
        }

        public void OnClickReturnToMainMenu()
        {
            SoundManager.Instance?.PlayClick();
            GameManager.Instance?.SetGameState(GameManager.GameState.MainMenu);
        }

        public void OnClickResultsContinue()
        {
            SoundManager.Instance?.PlayClick();
            GameManager.Instance?.SetGameState(GameManager.GameState.BetweenRounds);
        }

        public void OnClickResultsRetry()
        {
            SoundManager.Instance?.PlayClick();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartLevel(GameManager.Instance.currentLevelIndex);
            }
        }

        public void OnClickExit()
        {
            SoundManager.Instance?.PlayClick();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
