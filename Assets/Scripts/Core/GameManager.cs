using System.Collections.Generic;
using UnityEngine;
using SoulsALaCarte.Dining;
using SoulsALaCarte.Cooking;
using SoulsALaCarte.UI;
using SoulsALaCarte.Environment;
using SoulsALaCarte.Characters;

namespace SoulsALaCarte.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            MainMenu,
            StoryIntro,
            BetweenRounds,
            LevelSelect,
            Shop,
            InGameRound,
            Paused,
            RoundResults,
            VictoryEscaped
        }

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        [Header("Economía y Libertad")]
        public int totalCoins = 600;
        public readonly int freedomTarget = 50000;

        [Header("Progresión de Niveles")]
        public int currentLevelIndex = 1;
        public int highestUnlockedLevel = 1;
        public Dictionary<int, int> levelStars = new Dictionary<int, int>();

        [Header("Mejoras de Tienda")]
        public int speedLevel = 0;       // Max 3 (+12% cada)
        public int cookSpeedLevel = 0;   // Max 3 (-1.0s cada)
        public int extraPlatesLevel = 0; // Max 2 (+3 cada)
        public int drinkSpeedLevel = 0;  // Max 3 (-0.6s cada)
        public bool giuseppeHired = false;
        public int extraTipsLevel = 0;   // Max 3 (+10 cada)
        public bool colorPlatesActive = false;

        [Header("Estadísticas de la Ronda Actual")]
        public float roundTimeRemaining = 0f;
        public int roundCoinsEarned = 0;
        public int roundTipsEarned = 0;
        public int roundPenaltiesSuffered = 0;
        public int ordersCompleted = 0;
        public int ordersFailed = 0;
        public bool roundFailed = false;
        public bool kalayaPenaltyActive = false;

        public const int MAX_ALLOWED_FAILURES = 5;

        public LevelConfig CurrentLevelConfig => LevelDatabase.Get(currentLevelIndex);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            LoadProgress();
        }

        private void Start()
        {
            SetGameState(GameState.MainMenu);
        }

        private void Update()
        {
            if (CurrentState == GameState.InGameRound)
            {
                roundTimeRemaining -= Time.deltaTime;
                if (roundTimeRemaining <= 0f)
                {
                    EndRound();
                }

                // En partida activa, SOLO se puede abrir el menú de pausa con ESC
                if (InputBridge.GetPauseDown())
                {
                    PauseGame();
                }
            }
            else if (CurrentState == GameState.Paused)
            {
                if (InputBridge.GetPauseDown())
                {
                    ResumeGame();
                }
            }
            else if (CurrentState == GameState.BetweenRounds)
            {
                if (InputBridge.GetPauseDown())
                {
                    PauseGame();
                }
            }
        }

        public void SetGameState(GameState state)
        {
            CurrentState = state;
            Time.timeScale = (state == GameState.Paused) ? 0f : 1f;

            // Actualizar puerta de almas según el estado
            if (SoulShelfDoorway.Instance != null)
            {
                bool isLevelActive = (state == GameState.InGameRound || state == GameState.Paused);
                SoulShelfDoorway.Instance.SetDoorwayState(isLevelActive);
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.OnGameStateChanged(state);
            }
        }

        public void StartNewGame()
        {
            ResetProgress();
            SetGameState(GameState.StoryIntro);
        }

        public void ContinueGame()
        {
            LoadProgress();
            SetGameState(GameState.BetweenRounds);
        }

        public void StartLevel(int levelIdx)
        {
            currentLevelIndex = levelIdx;
            var cfg = CurrentLevelConfig;

            roundTimeRemaining = cfg.durationSeconds;
            roundCoinsEarned = 0;
            roundTipsEarned = 0;
            roundPenaltiesSuffered = 0;
            ordersCompleted = 0;
            ordersFailed = 0;
            roundFailed = false;

            // Si el jugador estaba en la cabina del maquinista, situarlo en la cocina
            if (PlayerController.Instance != null)
            {
                if (PlayerController.Instance.transform.position.x < -13.0f)
                {
                    PlayerController.Instance.transform.position = new Vector3(-6.0f, 0f, 0f);
                }
            }

            // Reset cocina y comensales
            var cookers = FindObjectsByType<FoodLickerCooker>();
            foreach (var c in cookers) c.ResetCooker();

            var cleanStacks = FindObjectsByType<CleanPlateStack>();
            foreach (var cs in cleanStacks) cs.UpdateCapacity();

            var dining = FindAnyObjectByType<DiningCarManager>();
            if (dining != null) dining.StartRound(cfg);

            SetGameState(GameState.InGameRound);

            if (levelIdx == 1)
            {
                UIManager.Instance?.SetTutorialHint("¡NIVEL 1 (TUTORIAL)! Ve al comedor de la derecha y pulsa [E] para tomar la comanda del cliente.");
            }
            else
            {
                UIManager.Instance?.ClearTutorialHint();
            }
        }

        public void RegisterOrderDelivered(int reward, int tip)
        {
            if (kalayaPenaltyActive)
            {
                reward = Mathf.RoundToInt(reward * 0.85f);
                tip = Mathf.RoundToInt(tip * 0.85f);
            }

            roundCoinsEarned += reward;
            roundTipsEarned += tip;
            ordersCompleted++;

            UIManager.Instance?.ShowNotification($"¡Pedido entregado! +{reward + tip} óbolos");
        }

        public void RegisterOrderPranked()
        {
            ordersCompleted++;
            UIManager.Instance?.ShowNotification("¡Palexius escapó sin pagar! +0 óbolos (¡Troleado!)");
        }

        public void RegisterOrderFailed()
        {
            ordersFailed++;
            roundPenaltiesSuffered += 15;

            if (ordersFailed >= MAX_ALLOWED_FAILURES)
            {
                roundFailed = true;
                UIManager.Instance?.ShowNotification("¡5 COMANDAS PERDIDAS! ¡DERROTA DE RONDA!");
                EndRoundByFailure();
            }
            else
            {
                UIManager.Instance?.ShowNotification($"¡Comanda perdida! Fallos: {ordersFailed}/{MAX_ALLOWED_FAILURES}");
            }
        }

        public void EndRoundByFailure()
        {
            roundFailed = true;
            int roundTotal = roundCoinsEarned + roundTipsEarned - roundPenaltiesSuffered;
            if (roundTotal < 0) roundTotal = 0;
            totalCoins += roundTotal;
            kalayaPenaltyActive = false;

            SaveProgress();
            SetGameState(GameState.RoundResults);
            SoundManager.Instance?.PlayLose();
        }

        public void ApplyKalayaPenalty()
        {
            kalayaPenaltyActive = true;
        }

        public void DeductCoins(int amount)
        {
            roundPenaltiesSuffered += amount;
            totalCoins = Mathf.Max(0, totalCoins - amount);
        }

        public void EndRound()
        {
            int roundTotal = roundCoinsEarned + roundTipsEarned - roundPenaltiesSuffered;
            if (roundTotal < 0) roundTotal = 0;

            totalCoins += roundTotal;
            kalayaPenaltyActive = false;

            var cfg = CurrentLevelConfig;
            int stars = 0;
            int totalScore = roundCoinsEarned + roundTipsEarned;

            if (currentLevelIndex == 1 && ordersCompleted >= 1)
            {
                stars = 3;
            }
            else
            {
                if (totalScore >= cfg.star3Points) stars = 3;
                else if (totalScore >= cfg.star2Points) stars = 2;
                else if (ordersCompleted >= 1 || totalScore >= cfg.star1Points) stars = 1;
            }

            if (!levelStars.ContainsKey(currentLevelIndex) || levelStars[currentLevelIndex] < stars)
            {
                levelStars[currentLevelIndex] = stars;
            }

            if (stars >= 1 && currentLevelIndex == highestUnlockedLevel && highestUnlockedLevel < LevelDatabase.TotalLevels)
            {
                highestUnlockedLevel++;
            }

            SaveProgress();
            SetGameState(GameState.RoundResults);

            if (stars > 0)
                SoundManager.Instance?.PlayWin();
            else
                SoundManager.Instance?.PlayLose();
        }

        public void PauseGame()
        {
            SetGameState(GameState.Paused);
        }

        public void ResumeGame()
        {
            SetGameState(GameState.InGameRound);
        }

        public void OpenLevelSelector()
        {
            if (CurrentState == GameState.InGameRound)
            {
                UIManager.Instance?.ShowNotification("¡No puedes seleccionar niveles durante una ronda activa!");
                return;
            }
            SetGameState(GameState.LevelSelect);
        }

        public void OpenShop()
        {
            if (CurrentState == GameState.InGameRound)
            {
                UIManager.Instance?.ShowNotification("¡El maquinista no atiende durante el servicio activo!");
                return;
            }
            SetGameState(GameState.Shop);
        }

        public void BribeMigafees()
        {
            if (totalCoins >= freedomTarget)
            {
                totalCoins -= freedomTarget;
                SaveProgress();
                SetGameState(GameState.VictoryEscaped);
                SoundManager.Instance?.PlayWin();
            }
            else
            {
                SoundManager.Instance?.PlayBuzzer();
                UIManager.Instance?.ShowNotification($"¡Te faltan {freedomTarget - totalCoins:N0} óbolos para pagar el soborno!");
            }
        }

        // --- Sistema de Mejoras ---
        public bool BuyUpgrade(string upgradeId)
        {
            int cost = GetUpgradeCost(upgradeId);
            if (totalCoins < cost)
            {
                SoundManager.Instance?.PlayBuzzer();
                return false;
            }

            bool purchased = false;
            switch (upgradeId)
            {
                case "Speed":
                    if (speedLevel < 3) { speedLevel++; purchased = true; }
                    break;
                case "CookSpeed":
                    if (cookSpeedLevel < 3) { cookSpeedLevel++; purchased = true; }
                    break;
                case "Plates":
                    if (extraPlatesLevel < 2) { extraPlatesLevel++; purchased = true; }
                    break;
                case "Drinks":
                    if (drinkSpeedLevel < 3) { drinkSpeedLevel++; purchased = true; }
                    break;
                case "Giuseppe":
                    if (!giuseppeHired) { giuseppeHired = true; purchased = true; }
                    break;
                case "Tips":
                    if (extraTipsLevel < 3) { extraTipsLevel++; purchased = true; }
                    break;
                case "Color":
                    if (!colorPlatesActive) { colorPlatesActive = true; purchased = true; }
                    break;
            }

            if (purchased)
            {
                totalCoins -= cost;
                SoundManager.Instance?.PlayCoin();
                SaveProgress();
                return true;
            }

            return false;
        }

        public int GetUpgradeCost(string upgradeId)
        {
            switch (upgradeId)
            {
                case "Speed": return 35 + (speedLevel * 25);
                case "CookSpeed": return 35 + (cookSpeedLevel * 25);
                case "Plates": return 30 + (extraPlatesLevel * 20);
                case "Drinks": return 30 + (drinkSpeedLevel * 20);
                case "Giuseppe": return 80;
                case "Tips": return 35 + (extraTipsLevel * 25);
                case "Color": return 25;
                default: return 30;
            }
        }

        public float GetSpeedMultiplier() => 1f + (speedLevel * 0.12f);
        public float GetCookTimeReduction() => cookSpeedLevel * 1.0f;
        public int GetExtraPlates() => extraPlatesLevel * 3;
        public float GetDrinkSpeedReduction() => drinkSpeedLevel * 0.6f;
        public int GetExtraTips() => extraTipsLevel * 10;

        // --- Guardado y Carga ---
        private void SaveProgress()
        {
            PlayerPrefs.SetInt("TotalCoins", totalCoins);
            PlayerPrefs.SetInt("HighestLevel", highestUnlockedLevel);
            PlayerPrefs.SetInt("SpeedLvl", speedLevel);
            PlayerPrefs.SetInt("CookLvl", cookSpeedLevel);
            PlayerPrefs.SetInt("PlatesLvl", extraPlatesLevel);
            PlayerPrefs.SetInt("DrinkLvl", drinkSpeedLevel);
            PlayerPrefs.SetInt("Giuseppe", giuseppeHired ? 1 : 0);
            PlayerPrefs.SetInt("TipsLvl", extraTipsLevel);
            PlayerPrefs.SetInt("ColorActive", colorPlatesActive ? 1 : 0);

            foreach (var kvp in levelStars)
            {
                PlayerPrefs.SetInt("Stars_Level_" + kvp.Key, kvp.Value);
            }

            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            totalCoins = PlayerPrefs.GetInt("TotalCoins", 600);
            highestUnlockedLevel = PlayerPrefs.GetInt("HighestLevel", 1);
            speedLevel = PlayerPrefs.GetInt("SpeedLvl", 0);
            cookSpeedLevel = PlayerPrefs.GetInt("CookLvl", 0);
            extraPlatesLevel = PlayerPrefs.GetInt("PlatesLvl", 0);
            drinkSpeedLevel = PlayerPrefs.GetInt("DrinkLvl", 0);
            giuseppeHired = PlayerPrefs.GetInt("Giuseppe", 0) == 1;
            extraTipsLevel = PlayerPrefs.GetInt("TipsLvl", 0);
            colorPlatesActive = PlayerPrefs.GetInt("ColorActive", 0) == 1;

            levelStars.Clear();
            for (int i = 1; i <= 10; i++)
            {
                int stars = PlayerPrefs.GetInt("Stars_Level_" + i, 0);
                if (stars > 0) levelStars[i] = stars;
            }
        }

        public void ResetProgress()
        {
            PlayerPrefs.DeleteAll();
            totalCoins = 600;
            highestUnlockedLevel = 1;
            speedLevel = 0;
            cookSpeedLevel = 0;
            extraPlatesLevel = 0;
            drinkSpeedLevel = 0;
            giuseppeHired = false;
            extraTipsLevel = 0;
            colorPlatesActive = false;
            levelStars.Clear();
            SaveProgress();
        }
    }
}
