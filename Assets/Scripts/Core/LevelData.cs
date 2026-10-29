using System.Collections.Generic;
using UnityEngine;

namespace SoulsALaCarte.Core
{
    public enum CustomerClass
    {
        Tonius = 0,    // Chicas demonio impacientes ("Poca comida, mucha prisa")
        Palexius = 1,  // Demonios trolls gamberros ("Troll 4 life")
        Goregina = 2   // Demonios agresivos bordes ("Carne al punto del averno")
    }

    public enum BossType
    {
        None = 0,
        Kalaya = 1,    // Boss Tonius: influencer de Hellsgram
        Traitor = 2,   // Boss Palexius: Loki del caos, crea laberintos
        Johanus = 3    // Boss Goregina: Mano derecha de Satán, voraz y letal
    }

    [System.Serializable]
    public class LevelConfig
    {
        public int levelIndex;               // 1..10
        public string title;                 // e.g. "El Primer Servicio"
        public string subtitle;              // e.g. "Clase: Tonius"
        public string description;
        public float toniusChance;
        public float palexiusChance;
        public float goreginaChance;
        public float bossChance;
        public BossType bossType;
        public float durationSeconds;
        public int baseRewardCoins;
        public int star1Points;              // Fácilmente alcanzable con 1 pedido
        public int star2Points;
        public int star3Points;
        public string catchphrase;
        public string conditionsInfo;

        public LevelConfig(int index, string t, string sub, string desc, float ton, float pal, float gor, float bChance, BossType bType, float dur, int reward, int s1, int s2, int s3, string phrase, string cond)
        {
            levelIndex = index;
            title = t;
            subtitle = sub;
            description = desc;
            toniusChance = ton;
            palexiusChance = pal;
            goreginaChance = gor;
            bossChance = bChance;
            bossType = bType;
            durationSeconds = dur;
            baseRewardCoins = reward;
            star1Points = s1;
            star2Points = s2;
            star3Points = s3;
            catchphrase = phrase;
            conditionsInfo = cond;
        }

        public CustomerClass RollClass()
        {
            float total = toniusChance + palexiusChance + goreginaChance;
            if (total <= 0f) return CustomerClass.Tonius;

            float r = Random.value * total;
            if (r < toniusChance) return CustomerClass.Tonius;
            if (r < toniusChance + palexiusChance) return CustomerClass.Palexius;
            return CustomerClass.Goregina;
        }

        public bool ShouldSpawnBoss()
        {
            if (bossType == BossType.None) return false;
            return Random.value <= bossChance;
        }
    }

    public static class LevelDatabase
    {
        private static readonly Dictionary<int, LevelConfig> levels = new Dictionary<int, LevelConfig>();

        static LevelDatabase()
        {
            // 1. EL PRIMER SERVICIO (Tutorial Guiado - 1 solo demonio)
            levels[1] = new LevelConfig(
                1, "El Primer Servicio", "Tutorial: Tonius",
                "Un demonio solitario llega al Soul Express. Aprende a tomar la comanda, preparar ingredientes en el caldero FoodLicker y servirlo.",
                1.0f, 0.0f, 0.0f, 0.0f, BossType.None,
                90f, 500, 30, 60, 90,
                "«Poca comida, mucha prisa»", "Nivel tutorial: 1 cliente garantizado.");

            // 2. FIESTA EN EL TREN
            levels[2] = new LevelConfig(
                2, "Fiesta en el Tren", "Clase: Palexius",
                "Los traviesos Palexius invaden el vagón comedor. Son demonios fiesteros que piden recetas variadas y bebidas.",
                0.2f, 0.8f, 0.0f, 0.0f, BossType.None,
                85f, 750, 40, 80, 140,
                "«Troll 4 life»", "Paciencia alta, ambiente relajado.");

            // 3. EL BANQUETE ROJO
            levels[3] = new LevelConfig(
                3, "El Banquete Rojo", "Clase: Goregina",
                "Demonios corpulentos y carnívoros. Piden platos de carne y pescado. ¡Grandes propinas!",
                0.1f, 0.1f, 0.8f, 0.0f, BossType.None,
                90f, 1000, 50, 100, 180,
                "«Carne al punto del averno»", "Generosas propinas por servicio rápido.");

            // 4. LA RUTA HELADA
            levels[4] = new LevelConfig(
                4, "La Ruta Helada", "Clase: Tonius & Palexius",
                "El tren cruza los páramos gélidos del infierno. Los demonios buscan refrescarse con bebidas del DrinkLin.",
                0.5f, 0.5f, 0.0f, 0.0f, BossType.None,
                95f, 1200, 60, 120, 200,
                "«Sed del inframundo»", "Petición equilibrada de comidas y bebidas.");

            // 5. SOBREVIVIR AL CAOS
            levels[5] = new LevelConfig(
                5, "Sobrevivir al Caos", "Clase: Variada",
                "Todas las clases de demonios llegan al tren a la vez. ¡Posible visita de la influencer Kalaya!",
                0.4f, 0.3f, 0.3f, 0.25f, BossType.Kalaya,
                100f, 1500, 70, 140, 240,
                "«El caos se apodera del servicio»", "Visita opcional de Kalaya. Alta puntuación.");

            // 6. EL FESTÍN DE JOHANUS
            levels[6] = new LevelConfig(
                6, "El Festín de Johanus", "Clase: Goregina",
                "Johanus, la mano derecha de Satán, tiene un apetito gigante. Sírvele sus platos favoritos para recibir una fortuna.",
                0.15f, 0.15f, 0.7f, 0.40f, BossType.Johanus,
                105f, 2500, 80, 160, 280,
                "«El gigante esquelético desciende»", "Jefe Johanus: Bonificación masiva de monedas.");

            // 7. EL LABERINTO
            levels[7] = new LevelConfig(
                7, "El Laberinto", "Clase: Palexius",
                "Traitor, el bromista del infierno, desafía tu habilidad culinaria con sus trucos.",
                0.2f, 0.7f, 0.1f, 0.40f, BossType.Traitor,
                105f, 3000, 90, 180, 300,
                "«Trampas en cada pasillo»", "Jefe Traitor: Desafío divertido y asequible.");

            // 8. LA DAMA DE LOS VIDEOS
            levels[8] = new LevelConfig(
                8, "La Dama de los Videos", "Clase: Tonius",
                "Kalaya visita el tren para grabar un reportaje en Hellsgram. Prepara sus postres favoritos.",
                0.7f, 0.15f, 0.15f, 0.45f, BossType.Kalaya,
                110f, 3500, 100, 200, 320,
                "«Reseña viral en Hellsgram»", "Jefe Kalaya: Vídeo de recomendación positiva.");

            // 9. EL TREN ABANDONADO
            levels[9] = new LevelConfig(
                9, "El Tren Abandonado", "Clase: Horda Demoníaca",
                "Viajeros de todos los rincones del Averno llenan el comedor para una cena festiva.",
                0.35f, 0.35f, 0.3f, 0.35f, BossType.Traitor,
                115f, 4500, 110, 220, 360,
                "«Servicio a toda marcha»", "Ritmo animado y muchas propinas.");

            // 10. EL SOBORNO FINAL
            levels[10] = new LevelConfig(
                10, "El Soborno Final", "Clase: Gran Banquete",
                "El último servicio del Soul Express. Reúne el dinero restante para sobornar a Migafees y escapar al Paraíso.",
                0.34f, 0.33f, 0.33f, 0.5f, BossType.Johanus,
                120f, 6000, 120, 250, 400,
                "«¡Compra tu libertad!»", "Gran banquete final con recompensas de oro.");
        }

        public static LevelConfig Get(int level)
        {
            if (levels.TryGetValue(level, out var cfg)) return cfg;
            return levels[1];
        }

        public static int TotalLevels => levels.Count;
    }
}
