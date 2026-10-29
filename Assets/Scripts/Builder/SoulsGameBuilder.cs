using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SoulsALaCarte.Cooking;
using SoulsALaCarte.Dining;
using SoulsALaCarte.Characters;
using SoulsALaCarte.Environment;
using SoulsALaCarte.UI;
using SoulsALaCarte.Core;

namespace SoulsALaCarte.Builder
{
    public class SoulsGameBuilder : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoBootstrapOnPlay()
        {
            if (FindAnyObjectByType<GameManager>() == null)
            {
                Debug.Log("[Souls A La Carte] Inicializando escena completa automáticamente...");
                BuildCompleteScene();
            }
        }

        public static void BuildCompleteScene()
        {
            var existingWorld = GameObject.Find("SoulsALaCarte_World");
            if (existingWorld != null) SafeDestroy(existingWorld);

            var existingCanvas = GameObject.Find("SoulsALaCarte_Canvas");
            if (existingCanvas != null) SafeDestroy(existingCanvas);

            var existingManagers = GameObject.Find("--- MANAGERS ---");
            if (existingManagers != null) SafeDestroy(existingManagers);

            // 1. Managers
            GameObject managersObj = new GameObject("--- MANAGERS ---");
            managersObj.AddComponent<GameManager>();
            managersObj.AddComponent<SoundManager>();

            // 2. Root del mundo 3D
            GameObject worldRoot = new GameObject("SoulsALaCarte_World");

            // 3. Crear Materiales Compartidos con acabado mejorado
            var mats = CreateMaterials();

            // 4. Crear Iluminación Atmosférica
            CreateLighting(worldRoot.transform);

            // 5. Tren Soul Express con 4 VAGONES detallados
            var trainRoot = new GameObject("Train_SoulExpress");
            trainRoot.transform.SetParent(worldRoot.transform, false);
            trainRoot.AddComponent<TrainAtmosphere>();

            // Vagón 1: Cabina del Maquinista (X: -22 a -14)
            BuildConductorWagon(trainRoot.transform, mats);

            // Vagón 2: Cocina (X: -14 a +2)
            BuildKitchenWagon(trainRoot.transform, mats);

            // Vagón 3: Comedor (X: +2 a +18)
            var diningMgr = BuildDiningWagon(trainRoot.transform, mats);

            // Vagón 4: Sala de Invocación con Pentagrama (X: +18 a +28)
            BuildInvocationWagon(trainRoot.transform, mats, diningMgr);

            // 6. Personajes
            var playerObj = BuildPlayer(worldRoot.transform, mats);
            var giuseppeObj = BuildGiuseppe(worldRoot.transform, mats);
            var conductorObj = BuildConductor(worldRoot.transform, mats);

            // 7. Cámara Isométrica estilo Overcooked
            SetupCamera(playerObj.transform);

            // 8. Crear Canvas UI Completo con Paneles Separados de Niveles y Tienda
            BuildUI(worldRoot.transform);

            // 9. Actualizar estado inicial de la puerta de almas
            if (SoulShelfDoorway.Instance != null)
            {
                SoulShelfDoorway.Instance.SetDoorwayState(false);
            }

            Debug.Log("[Souls A La Carte] ¡Mundo 3D completo de 4 vagones, interactuables y UI separada construidos con éxito!");
        }

        public static Font GetSafeFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null)
            {
                try { f = Resources.FindObjectsOfTypeAll<Font>()[0]; } catch { }
            }
            return f;
        }

        public class GameMaterials
        {
            public Material woodFloor;
            public Material darkWoodPanel;
            public Material stoneFloor;
            public Material darkIron;
            public Material redVelvet;
            public Material brassGold;
            public Material copperTrim;
            public Material soulRed;
            public Material soulYellow;
            public Material soulBlue;
            public Material soulWhite;
            public Material purpleRunes;
            public Material windowGlow;
            public Material emberFire;
        }

        private static GameMaterials CreateMaterials()
        {
            var m = new GameMaterials();
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");

            m.woodFloor = new Material(litShader);
            m.woodFloor.name = "Mat_WoodFloor";
            m.woodFloor.color = new Color(0.28f, 0.17f, 0.11f);

            m.darkWoodPanel = new Material(litShader);
            m.darkWoodPanel.name = "Mat_DarkWoodPanel";
            m.darkWoodPanel.color = new Color(0.18f, 0.11f, 0.08f);

            m.stoneFloor = new Material(litShader);
            m.stoneFloor.name = "Mat_StoneFloor";
            m.stoneFloor.color = new Color(0.14f, 0.13f, 0.16f);

            m.darkIron = new Material(litShader);
            m.darkIron.name = "Mat_DarkIron";
            m.darkIron.color = new Color(0.11f, 0.11f, 0.14f);

            m.redVelvet = new Material(litShader);
            m.redVelvet.name = "Mat_RedVelvet";
            m.redVelvet.color = new Color(0.70f, 0.08f, 0.12f);

            m.brassGold = new Material(litShader);
            m.brassGold.name = "Mat_BrassGold";
            m.brassGold.color = new Color(0.86f, 0.68f, 0.22f);

            m.copperTrim = new Material(litShader);
            m.copperTrim.name = "Mat_CopperTrim";
            m.copperTrim.color = new Color(0.72f, 0.42f, 0.25f);

            m.soulRed = new Material(litShader);
            m.soulRed.color = new Color(0.98f, 0.15f, 0.15f);
            m.soulRed.EnableKeyword("_EMISSION");
            m.soulRed.SetColor("_EmissionColor", new Color(0.98f, 0.15f, 0.15f) * 0.8f);

            m.soulYellow = new Material(litShader);
            m.soulYellow.color = new Color(0.98f, 0.88f, 0.12f);
            m.soulYellow.EnableKeyword("_EMISSION");
            m.soulYellow.SetColor("_EmissionColor", new Color(0.98f, 0.88f, 0.12f) * 0.8f);

            m.soulBlue = new Material(litShader);
            m.soulBlue.color = new Color(0.15f, 0.65f, 0.98f);
            m.soulBlue.EnableKeyword("_EMISSION");
            m.soulBlue.SetColor("_EmissionColor", new Color(0.15f, 0.65f, 0.98f) * 0.8f);

            m.soulWhite = new Material(litShader);
            m.soulWhite.color = new Color(0.95f, 0.95f, 0.98f);
            m.soulWhite.EnableKeyword("_EMISSION");
            m.soulWhite.SetColor("_EmissionColor", new Color(0.95f, 0.95f, 0.98f) * 0.8f);

            m.purpleRunes = new Material(unlitShader);
            m.purpleRunes.color = new Color(0.88f, 0.15f, 0.98f);

            m.windowGlow = new Material(unlitShader);
            m.windowGlow.color = new Color(1.0f, 0.38f, 0.08f);

            m.emberFire = new Material(unlitShader);
            m.emberFire.color = new Color(1.0f, 0.45f, 0.05f);

            return m;
        }

        private static void CreateLighting(Transform parent)
        {
            GameObject lightObj = new GameObject("Directional_InfernalGlow");
            lightObj.transform.SetParent(parent, false);
            lightObj.transform.rotation = Quaternion.Euler(52f, -28f, 0f);

            var dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1.0f, 0.88f, 0.80f);
            dirLight.intensity = 1.25f;
            dirLight.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.22f, 0.22f);
        }

        // ==========================================
        // VAGÓN 1: CABINA DEL MAQUINISTA (X: -22 a -14)
        // ==========================================
        private static void BuildConductorWagon(Transform parent, GameMaterials mats)
        {
            GameObject cabin = new GameObject("Wagon_1_ConductorCabin");
            cabin.transform.SetParent(parent, false);
            cabin.transform.localPosition = new Vector3(-18f, 0f, 0f);

            // Suelo (8m largo x 7m ancho) con placas metálicas y remaches
            CreateBox("Floor", cabin.transform, new Vector3(0f, -0.1f, 0f), new Vector3(8f, 0.2f, 7f), mats.stoneFloor);
            CreateBox("FloorTrim", cabin.transform, new Vector3(0f, 0.01f, 0f), new Vector3(7.8f, 0.02f, 0.15f), mats.brassGold);

            // Morro frontal aerodinámico (Oeste) con vigas de refuerzo
            CreateBox("Wall_West_Front", cabin.transform, new Vector3(-4f, 1.8f, 0f), new Vector3(0.4f, 3.6f, 7f), mats.darkIron);
            CreateBox("FrontWindshield_N", cabin.transform, new Vector3(-3.8f, 2.0f, 1.6f), new Vector3(0.05f, 1.6f, 2.4f), mats.windowGlow);
            CreateBox("FrontWindshield_S", cabin.transform, new Vector3(-3.8f, 2.0f, -1.6f), new Vector3(0.05f, 1.6f, 2.4f), mats.windowGlow);
            CreateBox("WindshieldFrame", cabin.transform, new Vector3(-3.78f, 2.0f, 0f), new Vector3(0.08f, 1.7f, 0.2f), mats.brassGold);

            // Pared Norte con ventanas de la cabina
            CreateBox("Wall_North", cabin.transform, new Vector3(0f, 1.8f, 3.5f), new Vector3(8f, 3.6f, 0.3f), mats.darkIron);
            CreateBox("Window_Cab", cabin.transform, new Vector3(0f, 2.2f, 3.38f), new Vector3(2.5f, 1.2f, 0.05f), mats.windowGlow);

            // Pared Sur (Cutaway bajo a 0.45m con pasamanos de latón brillante)
            CreateBox("Wall_South_Cutaway", cabin.transform, new Vector3(0f, 0.22f, -3.5f), new Vector3(8f, 0.45f, 0.3f), mats.darkIron);
            CreateBox("Wall_South_Trim", cabin.transform, new Vector3(0f, 0.46f, -3.5f), new Vector3(8f, 0.08f, 0.35f), mats.brassGold);

            // Arcos de techo decorativos
            CreateRoofArch(cabin.transform, -2.5f, mats);
            CreateRoofArch(cabin.transform, 1.5f, mats);

            // Cuadro de mandos / Pupitre de conducción de la locomotora
            CreateBox("HelmConsoleBase", cabin.transform, new Vector3(-3.0f, 0.5f, 0f), new Vector3(1.4f, 1.0f, 4.2f), mats.darkIron);
            CreateBox("HelmConsoleTop", cabin.transform, new Vector3(-2.95f, 1.05f, 0f), new Vector3(1.3f, 0.1f, 4.3f), mats.brassGold);

            // Manómetros y diales de vapor
            for (int i = -1; i <= 1; i++)
            {
                var gauge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                gauge.name = $"Gauge_{i}";
                gauge.transform.SetParent(cabin.transform, false);
                gauge.transform.localPosition = new Vector3(-2.8f, 1.35f, i * 1.0f);
                gauge.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                gauge.transform.localScale = new Vector3(0.35f, 0.08f, 0.35f);
                SafeDestroy(gauge.GetComponent<Collider>());
                gauge.GetComponent<Renderer>().material = mats.brassGold;
            }

            // Palancas de aceleración y freno
            CreateBox("Throttle_Lever", cabin.transform, new Vector3(-2.6f, 1.35f, 0.4f), new Vector3(0.08f, 0.5f, 0.08f), mats.soulRed);
            CreateBox("Brake_Lever", cabin.transform, new Vector3(-2.6f, 1.35f, -0.4f), new Vector3(0.08f, 0.5f, 0.08f), mats.copperTrim);

            // Caldera de fuego inferior en la cabina
            CreateBox("FireboxDoor", cabin.transform, new Vector3(-3.1f, 0.4f, 0f), new Vector3(0.1f, 0.6f, 1.2f), mats.emberFire);

            // Farol
            CreateLantern(cabin.transform, new Vector3(-1f, 2.6f, 2.6f));
        }

        // ==========================================
        // VAGÓN 2: COCINA (X: -14 a +2)
        // ==========================================
        private static void BuildKitchenWagon(Transform parent, GameMaterials mats)
        {
            GameObject kitchen = new GameObject("Wagon_2_Kitchen");
            kitchen.transform.SetParent(parent, false);
            kitchen.transform.localPosition = new Vector3(-6f, 0f, 0f);

            // Suelo (16m largo x 7m ancho) de madera oscura con remaches
            CreateBox("Floor", kitchen.transform, new Vector3(0f, -0.1f, 0f), new Vector3(16f, 0.2f, 7f), mats.woodFloor);
            CreateBox("FloorRunnerBorderN", kitchen.transform, new Vector3(0f, 0.01f, 1.8f), new Vector3(15.8f, 0.01f, 0.1f), mats.brassGold);
            CreateBox("FloorRunnerBorderS", kitchen.transform, new Vector3(0f, 0.01f, -1.8f), new Vector3(15.8f, 0.01f, 0.1f), mats.brassGold);

            // Pared Norte: Alta con ventanas góticas
            CreateBox("Wall_North_Base", kitchen.transform, new Vector3(0f, 0.9f, 3.5f), new Vector3(16f, 1.8f, 0.3f), mats.darkIron);
            CreateBox("Wall_North_Top", kitchen.transform, new Vector3(0f, 2.7f, 3.5f), new Vector3(16f, 1.8f, 0.3f), mats.darkWoodPanel);
            CreateBox("Wall_North_Trim", kitchen.transform, new Vector3(0f, 1.8f, 3.38f), new Vector3(16f, 0.1f, 0.08f), mats.brassGold);

            // Pared Sur: Cutaway bajo (0.45m)
            CreateBox("Wall_South_Cutaway", kitchen.transform, new Vector3(0f, 0.22f, -3.5f), new Vector3(16f, 0.45f, 0.3f), mats.darkIron);
            CreateBox("Wall_South_Trim", kitchen.transform, new Vector3(0f, 0.46f, -3.5f), new Vector3(16f, 0.08f, 0.35f), mats.brassGold);

            // Arcos de techo
            CreateRoofArch(kitchen.transform, -5.5f, mats);
            CreateRoofArch(kitchen.transform, -0.5f, mats);
            CreateRoofArch(kitchen.transform, 4.5f, mats);

            // Ventanas brillantes en pared norte
            for (int i = -3; i <= 3; i += 2)
            {
                CreateBox($"Window_{i}", kitchen.transform, new Vector3(i * 2.2f, 2.3f, 3.38f), new Vector3(1.5f, 1.1f, 0.05f), mats.windowGlow);
                CreateBox($"WinFrame_{i}", kitchen.transform, new Vector3(i * 2.2f, 2.3f, 3.35f), new Vector3(1.55f, 1.15f, 0.02f), mats.darkIron);
            }

            CreateLantern(kitchen.transform, new Vector3(-3.5f, 2.6f, 2.8f));
            CreateLantern(kitchen.transform, new Vector3(3.5f, 2.6f, 2.8f));

            // ========================================================
            // PUERTA OESTE: ESTANTERÍA DE ALMAS RETRÁCTIL / PASO AL MAQUINISTA
            // ========================================================
            var doorwayHelper = kitchen.AddComponent<SoulShelfDoorway>();

            // Pared oeste fija superior e inferior lateral
            CreateBox("Wall_West_Top", kitchen.transform, new Vector3(-8f, 3.0f, 0f), new Vector3(0.3f, 1.2f, 7f), mats.darkIron);
            CreateBox("Wall_West_N_Post", kitchen.transform, new Vector3(-8f, 1.5f, 3.0f), new Vector3(0.3f, 3.0f, 1.0f), mats.darkIron);
            CreateBox("Wall_West_S_Post", kitchen.transform, new Vector3(-8f, 1.5f, -3.0f), new Vector3(0.3f, 3.0f, 1.0f), mats.darkIron);

            // Root de la Estantería de Almas (se desactiva entre niveles para abrir el paso)
            GameObject soulShelfRoot = new GameObject("SoulShelf_DoorwayRoot");
            soulShelfRoot.transform.SetParent(kitchen.transform, false);
            soulShelfRoot.transform.localPosition = new Vector3(-7.7f, 0f, 0f);
            doorwayHelper.soulShelfRoot = soulShelfRoot;

            // Mueble de estantería de almas
            CreateBox("ShelfFrame", soulShelfRoot.transform, new Vector3(0f, 1.6f, 0.4f), new Vector3(0.5f, 3.1f, 4.4f), mats.darkWoodPanel);
            CreateBox("ShelfTrim", soulShelfRoot.transform, new Vector3(0.26f, 1.6f, 0.4f), new Vector3(0.04f, 3.1f, 4.4f), mats.brassGold);

            // Las 4 Almas colocadas de arriba a abajo exactamente como en el diagrama oficial:
            // 1. Alma Blanca (Dulce/Postres): Arriba (Z = +2.0f)
            // 2. Alma Azul (Pescado/Marisco): Z = +0.9f
            // 3. Alma Amarilla (Pasta): Z = -0.2f
            // 4. Alma Roja (Carne): Abajo (Z = -1.3f)
            CreateSoulCrate(soulShelfRoot.transform, new Vector3(0.35f, 0f, 2.0f), SoulType.White, mats.soulWhite);
            CreateSoulCrate(soulShelfRoot.transform, new Vector3(0.35f, 0f, 0.9f), SoulType.Blue, mats.soulBlue);
            CreateSoulCrate(soulShelfRoot.transform, new Vector3(0.35f, 0f, -0.2f), SoulType.Yellow, mats.soulYellow);
            CreateSoulCrate(soulShelfRoot.transform, new Vector3(0.35f, 0f, -1.3f), SoulType.Red, mats.soulRed);

            // Tubo / Chimenea vertical decorativa al lado de las almas
            var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = "SoulChimneyPipe";
            pipe.transform.SetParent(kitchen.transform, false);
            pipe.transform.localPosition = new Vector3(-7.6f, 2.0f, 3.1f);
            pipe.transform.localScale = new Vector3(0.35f, 1.8f, 0.35f);
            SafeDestroy(pipe.GetComponent<Collider>());
            pipe.GetComponent<Renderer>().material = mats.copperTrim;

            // Bloqueador de paso físico durante el nivel
            GameObject blockerObj = new GameObject("DoorwayBlockerCollider");
            blockerObj.transform.SetParent(kitchen.transform, false);
            blockerObj.transform.localPosition = new Vector3(-8.0f, 1.5f, 0.4f);
            var bCol = blockerObj.AddComponent<BoxCollider>();
            bCol.size = new Vector3(0.8f, 3.0f, 4.8f);
            doorwayHelper.doorwayBlocker = blockerObj;

            // Cartel "MAQUINISTA CERRADO EN NIVEL ACTIVO"
            var signObj = new GameObject("Sign_ConductorLocked");
            signObj.transform.SetParent(kitchen.transform, false);
            signObj.transform.localPosition = new Vector3(-7.4f, 2.8f, 0.4f);
            signObj.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            CreateBox("SignBoard", signObj.transform, Vector3.zero, new Vector3(3.2f, 0.65f, 0.05f), mats.brassGold);
            var signText = new GameObject("Text").AddComponent<TextMesh>();
            signText.transform.SetParent(signObj.transform, false);
            signText.transform.localPosition = new Vector3(0f, 0f, 0.04f);
            signText.characterSize = 0.055f;
            signText.fontSize = 26;
            signText.alignment = TextAlignment.Center;
            signText.anchor = TextAnchor.MiddleCenter;
            signText.color = Color.black;
            signText.text = "MAQUINISTA CERRADO\nEN NIVEL ACTIVO";
            doorwayHelper.lockedSign = signObj;

            // ========================================================
            // PARED NORTE: TABLERO DE NIVELES PEGADO A LA PARED + DRINKLIN + VASOS + BASURA
            // ========================================================
            // El tablero de niveles se monta plano contra la pared norte (Z = +3.32) con collider fino para no estorbar el paso a las almas
            CreateWallMountedLevelBoard(kitchen.transform, new Vector3(-4.8f, 1.7f, 3.32f), mats);

            // Máquina DrinkLin con 3 surtidores de bebidas (Hellbull, Damn, Elixir)
            CreateDrinkLinStation(kitchen.transform, new Vector3(-1.2f, 0f, 2.6f), mats);

            // Dispensador de vasos desechables
            CreateCupDispenserProp(kitchen.transform, new Vector3(1.2f, 0f, 2.6f), mats);

            // Cubo de basura infernal
            CreateTrashBinProp(kitchen.transform, new Vector3(2.5f, 0f, 2.6f), mats);

            // ========================================================
            // CENTRO: MESA DE PREPARACIÓN ("Mesa para apoyar" - Isla Central)
            // ========================================================
            CreatePrepCounterIsland(kitchen.transform, new Vector3(-1.2f, 0f, 0.3f), mats);

            // ========================================================
            // PARED SUR: 3 FOODLICKERS + PLATOS LIMPIOS + FREGADERO
            // ========================================================
            CreateCleanPlateRack(kitchen.transform, new Vector3(-5.2f, 0f, -2.4f), mats);
            CreateSinkStation(kitchen.transform, new Vector3(-3.8f, 0f, -2.4f), mats);
            CreateFoodLickerStation(kitchen.transform, new Vector3(-1.8f, 0f, -2.4f), mats, 1);
            CreateFoodLickerStation(kitchen.transform, new Vector3(-0.3f, 0f, -2.4f), mats, 2);
            CreateFoodLickerStation(kitchen.transform, new Vector3(1.2f, 0f, -2.4f), mats, 3);
        }

        // ==========================================
        // VAGÓN 3: COMEDOR (X: +2 a +18)
        // ==========================================
        private static DiningCarManager BuildDiningWagon(Transform parent, GameMaterials mats)
        {
            GameObject dining = new GameObject("Wagon_3_DiningCar");
            dining.transform.SetParent(parent, false);
            dining.transform.localPosition = new Vector3(10f, 0f, 0f);

            var diningMgr = dining.AddComponent<DiningCarManager>();

            // Suelo (16m largo x 7m ancho) con elegante alfombra roja y franjas de oro
            CreateBox("Floor", dining.transform, new Vector3(0f, -0.1f, 0f), new Vector3(16f, 0.2f, 7f), mats.woodFloor);
            CreateBox("RedCarpet", dining.transform, new Vector3(0f, 0.02f, 0f), new Vector3(16f, 0.02f, 2.4f), mats.redVelvet);
            CreateBox("CarpetBorderN", dining.transform, new Vector3(0f, 0.025f, 1.22f), new Vector3(16f, 0.015f, 0.06f), mats.brassGold);
            CreateBox("CarpetBorderS", dining.transform, new Vector3(0f, 0.025f, -1.22f), new Vector3(16f, 0.015f, 0.06f), mats.brassGold);

            // Pared Norte con boiserie oscura y ventanas
            CreateBox("Wall_North_Base", dining.transform, new Vector3(0f, 0.9f, 3.5f), new Vector3(16f, 1.8f, 0.3f), mats.darkWoodPanel);
            CreateBox("Wall_North_Top", dining.transform, new Vector3(0f, 2.7f, 3.5f), new Vector3(16f, 1.8f, 0.3f), mats.darkWoodPanel);
            CreateBox("Wall_North_Trim", dining.transform, new Vector3(0f, 1.8f, 3.38f), new Vector3(16f, 0.1f, 0.08f), mats.brassGold);

            // Pared Sur: Cutaway bajo (0.45m)
            CreateBox("Wall_South_Cutaway", dining.transform, new Vector3(0f, 0.22f, -3.5f), new Vector3(16f, 0.45f, 0.3f), mats.darkIron);
            CreateBox("Wall_South_Trim", dining.transform, new Vector3(0f, 0.46f, -3.5f), new Vector3(16f, 0.08f, 0.35f), mats.brassGold);

            CreateRoofArch(dining.transform, -5.5f, mats);
            CreateRoofArch(dining.transform, -0.5f, mats);
            CreateRoofArch(dining.transform, 4.5f, mats);

            for (int i = -3; i <= 3; i += 2)
            {
                CreateBox($"Window_{i}", dining.transform, new Vector3(i * 2.2f, 2.3f, 3.38f), new Vector3(1.5f, 1.1f, 0.05f), mats.windowGlow);
                CreateBox($"WinFrame_{i}", dining.transform, new Vector3(i * 2.2f, 2.3f, 3.35f), new Vector3(1.55f, 1.15f, 0.02f), mats.brassGold);
            }

            CreateLantern(dining.transform, new Vector3(-3.5f, 2.6f, 2.8f));
            CreateLantern(dining.transform, new Vector3(3.5f, 2.6f, 2.8f));

            // --- 2 FILAS DE MESAS DE COMEDOR (Norte y Sur) ---
            float[] xOffsets = new float[] { -5.0f, -1.8f, 1.4f, 4.6f };
            int tableIndex = 1;

            foreach (float x in xOffsets)
            {
                var tableN = CreateDiningTable(dining.transform, new Vector3(x, 0f, 2.0f), tableIndex++, mats);
                diningMgr.tables.Add(tableN);

                var tableS = CreateDiningTable(dining.transform, new Vector3(x, 0f, -2.0f), tableIndex++, mats);
                diningMgr.tables.Add(tableS);
            }

            return diningMgr;
        }

        // ==========================================
        // VAGÓN 4: SALA DE INVOCACIÓN (X: +18 a +28)
        // ==========================================
        private static void BuildInvocationWagon(Transform parent, GameMaterials mats, DiningCarManager diningMgr)
        {
            GameObject invoc = new GameObject("Wagon_4_InvocationCar");
            invoc.transform.SetParent(parent, false);
            invoc.transform.localPosition = new Vector3(23f, 0f, 0f);

            // Suelo de piedra volcánica (10m largo x 7m ancho)
            CreateBox("Floor", invoc.transform, new Vector3(0f, -0.1f, 0f), new Vector3(10f, 0.2f, 7f), mats.stoneFloor);

            // Pared Norte de piedra oscura
            CreateBox("Wall_North", invoc.transform, new Vector3(0f, 1.8f, 3.5f), new Vector3(10f, 3.6f, 0.3f), mats.darkIron);

            // Pared Sur: Cutaway
            CreateBox("Wall_South_Cutaway", invoc.transform, new Vector3(0f, 0.22f, -3.5f), new Vector3(10f, 0.45f, 0.3f), mats.darkIron);
            CreateBox("Wall_South_Trim", invoc.transform, new Vector3(0f, 0.46f, -3.5f), new Vector3(10f, 0.08f, 0.35f), mats.brassGold);

            // Pared Este (Cola del tren) con ventana circular de runas
            CreateBox("Wall_East_Back", invoc.transform, new Vector3(5f, 1.8f, 0f), new Vector3(0.4f, 3.6f, 7f), mats.darkIron);
            var oculus = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            oculus.name = "OculusRuneWindow";
            oculus.transform.SetParent(invoc.transform, false);
            oculus.transform.localPosition = new Vector3(4.85f, 2.0f, 0f);
            oculus.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            oculus.transform.localScale = new Vector3(2.2f, 0.08f, 2.2f);
            SafeDestroy(oculus.GetComponent<Collider>());
            oculus.GetComponent<Renderer>().material = mats.purpleRunes;

            CreateRoofArch(invoc.transform, -2.5f, mats);
            CreateRoofArch(invoc.transform, 2.5f, mats);

            // --- PENTAGRAMA DEMONÍACO GIGANTE EN EL SUELO ---
            CreatePentagramRitualCircle(invoc.transform, new Vector3(0f, 0.02f, 0f), mats, diningMgr);
        }

        private static void CreatePentagramRitualCircle(Transform parent, Vector3 localPos, GameMaterials mats, DiningCarManager diningMgr)
        {
            GameObject circleObj = new GameObject("GiantDemonicPentagram");
            circleObj.transform.SetParent(parent, false);
            circleObj.transform.localPosition = localPos;

            var plat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plat.name = "Platform";
            plat.transform.SetParent(circleObj.transform, false);
            plat.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            plat.transform.localScale = new Vector3(5.2f, 0.04f, 5.2f);
            SafeDestroy(plat.GetComponent<Collider>());
            plat.GetComponent<Renderer>().material = mats.darkIron;

            var runeRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            runeRing.name = "RuneRing";
            runeRing.transform.SetParent(circleObj.transform, false);
            runeRing.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            runeRing.transform.localScale = new Vector3(4.8f, 0.02f, 4.8f);
            SafeDestroy(runeRing.GetComponent<Collider>());
            runeRing.GetComponent<Renderer>().material = mats.purpleRunes;

            var innerDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            innerDisc.name = "InnerDisc";
            innerDisc.transform.SetParent(circleObj.transform, false);
            innerDisc.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            innerDisc.transform.localScale = new Vector3(4.2f, 0.02f, 4.2f);
            SafeDestroy(innerDisc.GetComponent<Collider>());
            innerDisc.GetComponent<Renderer>().material = mats.stoneFloor;

            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f;
                var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beam.name = $"PentagramLine_{i}";
                beam.transform.SetParent(circleObj.transform, false);
                beam.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                beam.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
                beam.transform.localScale = new Vector3(0.12f, 0.02f, 3.8f);
                SafeDestroy(beam.GetComponent<Collider>());
                beam.GetComponent<Renderer>().material = mats.soulRed;
            }

            CreateBrazier(circleObj.transform, new Vector3(-2.6f, 0f, 2.2f), mats);
            CreateBrazier(circleObj.transform, new Vector3(2.6f, 0f, 2.2f), mats);
            CreateBrazier(circleObj.transform, new Vector3(-2.6f, 0f, -2.2f), mats);
            CreateBrazier(circleObj.transform, new Vector3(2.6f, 0f, -2.2f), mats);

            var spawnPt = new GameObject("CustomerSpawnPoint");
            spawnPt.transform.SetParent(circleObj.transform, false);
            spawnPt.transform.localPosition = new Vector3(0f, 0f, 0f);
            diningMgr.customerSpawnPoint = spawnPt.transform;

            var exitPt = new GameObject("CustomerExitPoint");
            exitPt.transform.SetParent(circleObj.transform, false);
            exitPt.transform.localPosition = new Vector3(1.5f, 0f, 0f);
            diningMgr.customerExitPoint = exitPt.transform;
        }

        private static void CreateBrazier(Transform parent, Vector3 localPos, GameMaterials mats)
        {
            GameObject bObj = new GameObject("InfernalBrazier");
            bObj.transform.SetParent(parent, false);
            bObj.transform.localPosition = localPos;

            CreateBox("Pillar", bObj.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.4f, 1.0f, 0.4f), mats.darkIron);
            var bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.transform.SetParent(bObj.transform, false);
            bowl.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            bowl.transform.localScale = new Vector3(0.7f, 0.2f, 0.7f);
            SafeDestroy(bowl.GetComponent<Collider>());
            bowl.GetComponent<Renderer>().material = mats.brassGold;

            var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.transform.SetParent(bObj.transform, false);
            flame.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            flame.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
            SafeDestroy(flame.GetComponent<Collider>());
            flame.GetComponent<Renderer>().material = mats.emberFire;

            var light = bObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.45f, 0.1f);
            light.range = 6f;
            light.intensity = 1.5f;
        }

        private static void CreateRoofArch(Transform parent, float xPos, GameMaterials mats)
        {
            GameObject arch = new GameObject($"RoofArch_{xPos}");
            arch.transform.SetParent(parent, false);
            arch.transform.localPosition = new Vector3(xPos, 3.4f, 0f);

            // Viga horizontal superior
            CreateBox("BeamTop", arch.transform, new Vector3(0f, 0f, 0f), new Vector3(0.18f, 0.18f, 7.0f), mats.darkIron);
            // Soportes curvados en las esquinas superiores
            CreateBox("RibNorth", arch.transform, new Vector3(0f, -0.3f, 3.2f), new Vector3(0.18f, 0.5f, 0.18f), mats.brassGold);
            CreateBox("RibSouth", arch.transform, new Vector3(0f, -0.3f, -3.2f), new Vector3(0.18f, 0.5f, 0.18f), mats.brassGold);
        }

        // --- ESTACIONES Y PROPS ---
        private static void CreateFoodLickerStation(Transform parent, Vector3 localPos, GameMaterials mats, int index)
        {
            GameObject cookerObj = new GameObject($"Prop_FoodLicker_{index}");
            cookerObj.transform.SetParent(parent, false);
            cookerObj.transform.localPosition = localPos;

            CreateBox("CookerBase", cookerObj.transform, new Vector3(0f, 0.4f, 0f), new Vector3(1.3f, 0.8f, 1.1f), mats.darkIron);
            CreateBox("BaseTrim", cookerObj.transform, new Vector3(0f, 0.78f, 0f), new Vector3(1.35f, 0.06f, 1.15f), mats.brassGold);

            var cauldron = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cauldron.name = "CauldronBody";
            cauldron.transform.SetParent(cookerObj.transform, false);
            cauldron.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            cauldron.transform.localScale = new Vector3(0.85f, 0.45f, 0.85f);
            SafeDestroy(cauldron.GetComponent<Collider>());
            cauldron.GetComponent<Renderer>().material = mats.darkIron;

            var fire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fire.name = "LavaSurface";
            fire.transform.SetParent(cauldron.transform, false);
            fire.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            fire.transform.localScale = new Vector3(0.82f, 0.1f, 0.82f);
            SafeDestroy(fire.GetComponent<Collider>());
            fire.GetComponent<Renderer>().material = mats.soulRed;

            // Ojos demoníacos del caldero FoodLicker
            for (int i = -1; i <= 1; i += 2)
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.transform.SetParent(cauldron.transform, false);
                eye.transform.localPosition = new Vector3(i * 0.25f, 0.1f, 0.42f);
                eye.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
                SafeDestroy(eye.GetComponent<Collider>());
                eye.GetComponent<Renderer>().material = mats.soulYellow;
            }

            var col = cookerObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.6f, 0f);
            col.size = new Vector3(1.4f, 1.4f, 1.3f);

            var cooker = cookerObj.AddComponent<FoodLickerCooker>();
            cooker.cauldronIndex = index;
        }

        private static void CreateSinkStation(Transform parent, Vector3 localPos, GameMaterials mats)
        {
            GameObject sinkObj = new GameObject("Prop_Sink");
            sinkObj.transform.SetParent(parent, false);
            sinkObj.transform.localPosition = localPos;

            CreateBox("SinkCabinet", sinkObj.transform, new Vector3(0f, 0.45f, 0f), new Vector3(1.2f, 0.9f, 1.0f), mats.darkIron);
            CreateBox("SinkTrim", sinkObj.transform, new Vector3(0f, 0.88f, 0f), new Vector3(1.25f, 0.05f, 1.05f), mats.brassGold);

            var basin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basin.name = "Basin";
            basin.transform.SetParent(sinkObj.transform, false);
            basin.transform.localPosition = new Vector3(0f, 0.88f, 0f);
            basin.transform.localScale = new Vector3(0.85f, 0.15f, 0.7f);
            SafeDestroy(basin.GetComponent<Collider>());
            basin.GetComponent<Renderer>().material = mats.soulBlue;

            // Grifo de cobre
            var tap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tap.transform.SetParent(sinkObj.transform, false);
            tap.transform.localPosition = new Vector3(0f, 1.15f, -0.3f);
            tap.transform.localScale = new Vector3(0.06f, 0.25f, 0.06f);
            SafeDestroy(tap.GetComponent<Collider>());
            tap.GetComponent<Renderer>().material = mats.copperTrim;

            var col = sinkObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(1.4f, 1.2f, 1.2f);

            sinkObj.AddComponent<SinkStation>();
        }

        private static void CreateCleanPlateRack(Transform parent, Vector3 localPos, GameMaterials mats)
        {
            GameObject rackObj = new GameObject("Prop_CleanPlateRack");
            rackObj.transform.SetParent(parent, false);
            rackObj.transform.localPosition = localPos;

            CreateBox("RackCabinet", rackObj.transform, new Vector3(0f, 0.45f, 0f), new Vector3(1.0f, 0.9f, 0.9f), mats.darkIron);
            CreateBox("RackTrim", rackObj.transform, new Vector3(0f, 0.88f, 0f), new Vector3(1.05f, 0.05f, 0.95f), mats.brassGold);

            for (int i = 0; i < 4; i++)
            {
                var plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                plate.transform.SetParent(rackObj.transform, false);
                plate.transform.localPosition = new Vector3(0f, 0.92f + i * 0.05f, 0f);
                plate.transform.localScale = new Vector3(0.48f, 0.03f, 0.48f);
                SafeDestroy(plate.GetComponent<Collider>());
                plate.GetComponent<Renderer>().material = mats.soulWhite;
            }

            var col = rackObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(1.2f, 1.2f, 1.2f);

            rackObj.AddComponent<CleanPlateStack>();
        }

        private static void CreateSoulCrate(Transform parent, Vector3 localPos, SoulType soul, Material mat)
        {
            GameObject crateObj = new GameObject("Crate_Soul_" + soul);
            crateObj.transform.SetParent(parent, false);
            crateObj.transform.localPosition = localPos;

            // Pedestal / nicho elegante para el alma
            CreateBox("Pedestal", crateObj.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.75f, 0.8f, 0.75f), mat);

            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.transform.SetParent(crateObj.transform, false);
            orb.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            orb.transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);
            SafeDestroy(orb.GetComponent<Collider>());
            orb.GetComponent<Renderer>().material = mat;

            var textObj = new GameObject("SoulLabel");
            textObj.transform.SetParent(crateObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            textObj.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            var text = textObj.AddComponent<TextMesh>();
            text.characterSize = 0.07f;
            text.fontSize = 24;
            text.alignment = TextAlignment.Center;
            text.anchor = TextAnchor.MiddleCenter;

            string sName = "";
            Color sColor = Color.white;
            switch (soul)
            {
                case SoulType.White: sName = "Alma Blanca\n(Dulces)"; sColor = Color.white; break;
                case SoulType.Blue: sName = "Alma Azul\n(Pescado)"; sColor = new Color(0.3f, 0.7f, 1f); break;
                case SoulType.Yellow: sName = "Alma Amarilla\n(Pasta)"; sColor = new Color(1f, 0.9f, 0.2f); break;
                case SoulType.Red: sName = "Alma Roja\n(Carne)"; sColor = new Color(1f, 0.3f, 0.3f); break;
            }
            text.color = sColor;
            text.text = sName;

            // Collider trigger ajustado para que Francis pueda recogerla sin tropezar
            var col = crateObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(1.1f, 1.2f, 1.1f);

            var disp = crateObj.AddComponent<SoulDispenser>();
            disp.soulType = soul;
        }

        // Máquina DrinkLin integrada con 3 dispensadores
        private static void CreateDrinkLinStation(Transform parent, Vector3 localPos, GameMaterials mats)
        {
            GameObject drinkLin = new GameObject("Machine_DrinkLin");
            drinkLin.transform.SetParent(parent, false);
            drinkLin.transform.localPosition = localPos;

            // Mueble central de la máquina
            CreateBox("MachineBody", drinkLin.transform, new Vector3(0f, 0.9f, 0f), new Vector3(3.4f, 1.8f, 0.85f), mats.darkIron);
            CreateBox("HeaderMarquee", drinkLin.transform, new Vector3(0f, 1.85f, 0f), new Vector3(3.5f, 0.3f, 0.9f), mats.brassGold);

            var titleObj = new GameObject("DrinkLinTitle");
            titleObj.transform.SetParent(drinkLin.transform, false);
            titleObj.transform.localPosition = new Vector3(0f, 1.85f, -0.47f);
            var tt = titleObj.AddComponent<TextMesh>();
            tt.characterSize = 0.08f;
            tt.fontSize = 28;
            tt.alignment = TextAlignment.Center;
            tt.anchor = TextAnchor.MiddleCenter;
            tt.color = Color.black;
            tt.text = "★ DRINKLIN FENRE ★";

            // 3 Dispensadores individuales
            CreateSingleDrinkTap(drinkLin.transform, new Vector3(-1.1f, 0f, 0f), DrinkType.Hellbull, mats.soulRed);
            CreateSingleDrinkTap(drinkLin.transform, new Vector3(0f, 0f, 0f), DrinkType.Damn, mats.brassGold);
            CreateSingleDrinkTap(drinkLin.transform, new Vector3(1.1f, 0f, 0f), DrinkType.FizzUp, mats.soulBlue);
        }

        private static void CreateSingleDrinkTap(Transform parent, Vector3 localPos, DrinkType drink, Material mat)
        {
            GameObject tapObj = new GameObject("Tap_" + drink);
            tapObj.transform.SetParent(parent, false);
            tapObj.transform.localPosition = localPos;

            // Surtidor con tanque de líquido iluminado
            var tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tank.transform.SetParent(tapObj.transform, false);
            tank.transform.localPosition = new Vector3(0f, 1.1f, -0.2f);
            tank.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
            SafeDestroy(tank.GetComponent<Collider>());
            tank.GetComponent<Renderer>().material = mat;

            var col = tapObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.7f, 0f);
            col.size = new Vector3(0.9f, 1.4f, 0.9f);

            var disp = tapObj.AddComponent<DrinkDispenser>();
            disp.drinkType = drink;
        }

        private static void CreateCupDispenserProp(Transform parent, Vector3 localPos, GameMaterials mats)
        {
            GameObject cupObj = new GameObject("Prop_CupDispenser");
            cupObj.transform.SetParent(parent, false);
            cupObj.transform.localPosition = localPos;

            CreateBox("CupBase", cupObj.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.9f, 0.65f), mats.darkIron);

            // Tubo dispensador transparente
            var tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tube.transform.SetParent(cupObj.transform, false);
            tube.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            tube.transform.localScale = new Vector3(0.28f, 0.45f, 0.28f);
            SafeDestroy(tube.GetComponent<Collider>());
            tube.GetComponent<Renderer>().material = mats.soulWhite;

            var cupTextObj = new GameObject("CupLabel");
            cupTextObj.transform.SetParent(cupObj.transform, false);
            cupTextObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cupTextObj.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            var ct = cupTextObj.AddComponent<TextMesh>();
            ct.characterSize = 0.07f;
            ct.fontSize = 24;
            ct.alignment = TextAlignment.Center;
            ct.anchor = TextAnchor.MiddleCenter;
            ct.color = Color.white;
            ct.text = "Dispensador\nde Vasos";

            var cCol = cupObj.AddComponent<BoxCollider>();
            cCol.center = new Vector3(0f, 0.7f, 0f);
            cCol.size = new Vector3(0.85f, 1.4f, 0.85f);
            cupObj.AddComponent<CupDispenser>();
        }

        private static void CreatePrepCounterIsland(Transform parent, Vector3 localPos, GameMaterials mats)
        {
            GameObject island = new GameObject("Island_PrepCounter");
            island.transform.SetParent(parent, false);
            island.transform.localPosition = localPos;

            // Bloque alargado de apoyo (3 módulos unidos con detalles de carnicero)
            CreatePrepCounter(island.transform, new Vector3(-1.2f, 0f, 0f), mats);
            CreatePrepCounter(island.transform, new Vector3(0f, 0f, 0f), mats);
            CreatePrepCounter(island.transform, new Vector3(1.2f, 0f, 0f), mats);
        }

        private static void CreatePrepCounter(Transform parent, Vector3 localPos, GameMaterials mats)
        {
            GameObject tableObj = new GameObject("Prop_CounterTable");
            tableObj.transform.SetParent(parent, false);
            tableObj.transform.localPosition = localPos;

            CreateBox("CounterSurface", tableObj.transform, new Vector3(0f, 0.45f, 0f), new Vector3(1.15f, 0.9f, 0.9f), mats.darkWoodPanel);
            CreateBox("CounterTop", tableObj.transform, new Vector3(0f, 0.91f, 0f), new Vector3(1.18f, 0.04f, 0.92f), mats.woodFloor);

            var textObj = new GameObject("TableLabel");
            textObj.transform.SetParent(tableObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            textObj.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            var text = textObj.AddComponent<TextMesh>();
            text.characterSize = 0.065f;
            text.fontSize = 22;
            text.alignment = TextAlignment.Center;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(0.9f, 0.85f, 0.7f);
            text.text = "Mesa para\nApoyar";

            var col = tableObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.45f, 0f);
            col.size = new Vector3(1.2f, 1.1f, 1.0f);
            tableObj.AddComponent<CounterTable>();
        }

        private static void CreateTrashBinProp(Transform parent, Vector3 localPos, GameMaterials mats)
        {
            GameObject binObj = new GameObject("Prop_TrashBin");
            binObj.transform.SetParent(parent, false);
            binObj.transform.localPosition = localPos;

            var binCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            binCylinder.name = "BinMesh";
            binCylinder.transform.SetParent(binObj.transform, false);
            binCylinder.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            binCylinder.transform.localScale = new Vector3(0.55f, 0.45f, 0.55f);
            SafeDestroy(binCylinder.GetComponent<Collider>());
            binCylinder.GetComponent<Renderer>().material = mats.darkIron;

            var textObj = new GameObject("BinLabel");
            textObj.transform.SetParent(binObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            textObj.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            var text = textObj.AddComponent<TextMesh>();
            text.characterSize = 0.065f;
            text.fontSize = 22;
            text.alignment = TextAlignment.Center;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(0.7f, 0.7f, 0.75f);
            text.text = "Cubo de\nBasura";

            var col = binObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.45f, 0f);
            col.size = new Vector3(0.9f, 1.1f, 0.9f);
            binObj.AddComponent<TrashBin>();
        }

        // TABLERO DE NIVELES MONTADO EN LA PARED NORTE (Sin colisiones frontales que tapen las almas)
        private static void CreateWallMountedLevelBoard(Transform parent, Vector3 localPos, GameMaterials mats)
        {
            GameObject boardStand = new GameObject("Prop_LevelBoardWall");
            boardStand.transform.SetParent(parent, false);
            boardStand.transform.localPosition = localPos;

            // Marco dorado plano en la pared
            CreateBox("BoardFrame", boardStand.transform, new Vector3(0f, 0f, 0f), new Vector3(2.4f, 1.4f, 0.10f), mats.brassGold);
            // Pizarra oscura
            var surface = CreateBox("BoardSurface", boardStand.transform, new Vector3(0f, 0f, -0.06f), new Vector3(2.2f, 1.2f, 0.04f), mats.darkIron);

            var textObj = new GameObject("BoardLabel");
            textObj.transform.SetParent(surface.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.04f);
            var text = textObj.AddComponent<TextMesh>();
            text.characterSize = 0.07f;
            text.fontSize = 26;
            text.alignment = TextAlignment.Center;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = Color.yellow;
            text.text = "TABLERO DE NIVELES\n[E] Abrir Selección de Nivel\n(Solo entre servicios)";

            // Runa brillante superior
            var beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "Beacon";
            beacon.transform.SetParent(boardStand.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            beacon.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);
            SafeDestroy(beacon.GetComponent<Collider>());
            beacon.GetComponent<Renderer>().material = mats.purpleRunes;

            // Collider pegado a la pared para no obstaculizar el pasillo ni las almas
            var col = boardStand.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0f, -0.2f);
            col.size = new Vector3(2.6f, 1.6f, 0.5f);

            boardStand.AddComponent<ManagementBoard>();
        }

        private static CustomerTable CreateDiningTable(Transform parent, Vector3 localPos, int tableIdx, GameMaterials mats)
        {
            GameObject tableObj = new GameObject($"DiningTable_{tableIdx}");
            tableObj.transform.SetParent(parent, false);
            tableObj.transform.localPosition = localPos;

            CreateBox("TableTop", tableObj.transform, new Vector3(0f, 0.42f, 0f), new Vector3(1.6f, 0.84f, 1.1f), mats.darkWoodPanel);
            CreateBox("TableCloth", tableObj.transform, new Vector3(0f, 0.85f, 0f), new Vector3(1.4f, 0.02f, 0.9f), mats.woodFloor);

            CreateBox("BoothBackN", tableObj.transform, new Vector3(0f, 0.65f, 0.85f), new Vector3(1.5f, 0.9f, 0.35f), mats.redVelvet);
            CreateBox("BoothBackS", tableObj.transform, new Vector3(0f, 0.65f, -0.85f), new Vector3(1.5f, 0.9f, 0.35f), mats.redVelvet);

            // Pequeña vela en la mesa
            var candle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            candle.transform.SetParent(tableObj.transform, false);
            candle.transform.localPosition = new Vector3(0.5f, 0.95f, 0f);
            candle.transform.localScale = new Vector3(0.06f, 0.15f, 0.06f);
            SafeDestroy(candle.GetComponent<Collider>());
            candle.GetComponent<Renderer>().material = mats.soulYellow;

            var col = tableObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(1.8f, 1.2f, 2.0f);

            var cTable = tableObj.AddComponent<CustomerTable>();
            cTable.tableNumber = tableIdx;
            return cTable;
        }

        private static GameObject CreateLantern(Transform parent, Vector3 localPos)
        {
            GameObject lanternObj = new GameObject("InfernalLantern");
            lanternObj.transform.SetParent(parent, false);
            lanternObj.transform.localPosition = localPos;

            var light = lanternObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.6f, 0.25f);
            light.range = 8f;
            light.intensity = 1.7f;

            return lanternObj;
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPos;
            box.transform.localScale = localScale;
            var rend = box.GetComponent<Renderer>();
            rend.material = mat;
            return box;
        }

        // --- PERSONAJES ---
        private static GameObject BuildPlayer(Transform parent, GameMaterials mats)
        {
            GameObject player = new GameObject("Player_Francis");
            player.transform.SetParent(parent, false);
            player.transform.localPosition = new Vector3(-3.5f, 0f, 0f);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.6f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.8f, 0f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(visual.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            body.transform.localScale = new Vector3(0.6f, 0.55f, 0.6f);
            SafeDestroy(body.GetComponent<Collider>());
            var bRend = body.GetComponent<Renderer>();
            bRend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            bRend.material.color = new Color(0.2f, 0.4f, 0.75f);

            var apron = GameObject.CreatePrimitive(PrimitiveType.Cube);
            apron.name = "Apron";
            apron.transform.SetParent(body.transform, false);
            apron.transform.localPosition = new Vector3(0f, 0f, 0.35f);
            apron.transform.localScale = new Vector3(0.75f, 0.9f, 0.1f);
            SafeDestroy(apron.GetComponent<Collider>());
            var aRend = apron.GetComponent<Renderer>();
            aRend.material = mats.soulWhite;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(visual.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            head.transform.localScale = new Vector3(0.48f, 0.48f, 0.48f);
            SafeDestroy(head.GetComponent<Collider>());
            var hRend = head.GetComponent<Renderer>();
            hRend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            hRend.material.color = new Color(0.92f, 0.75f, 0.65f);

            var hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.name = "ChefHat";
            hat.transform.SetParent(head.transform, false);
            hat.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            hat.transform.localScale = new Vector3(0.45f, 0.3f, 0.45f);
            SafeDestroy(hat.GetComponent<Collider>());
            var hatRend = hat.GetComponent<Renderer>();
            hatRend.material = mats.soulWhite;

            player.AddComponent<PlayerController>();
            return player;
        }

        private static GameObject BuildGiuseppe(Transform parent, GameMaterials mats)
        {
            GameObject giuseppe = new GameObject("Helper_Giuseppe");
            giuseppe.transform.SetParent(parent, false);
            giuseppe.transform.localPosition = new Vector3(-6.5f, 0f, -1.2f);
            giuseppe.AddComponent<GiuseppeHelper>();
            return giuseppe;
        }

        private static GameObject BuildConductor(Transform parent, GameMaterials mats)
        {
            GameObject conductor = new GameObject("NPC_Migafees");
            conductor.transform.SetParent(parent, false);
            conductor.transform.localPosition = new Vector3(-17.5f, 0f, 0f);
            conductor.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            conductor.AddComponent<BoxCollider>().size = new Vector3(2.0f, 2.2f, 2.0f);
            conductor.AddComponent<ConductorNPC>();

            var textObj = new GameObject("ConductorLabel");
            textObj.transform.SetParent(conductor.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            textObj.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            var text = textObj.AddComponent<TextMesh>();
            text.characterSize = 0.075f;
            text.fontSize = 24;
            text.alignment = TextAlignment.Center;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.85f, 0.2f);
            text.text = "Lord Mr. Sir Dr. Migafees\n(Maquinista)\n[E] Abrir Tienda de Mejoras";

            return conductor;
        }

        private static void SetupCamera(Transform playerTransform)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            var follow = cam.GetComponent<TrainCameraController>();
            if (follow == null) follow = cam.gameObject.AddComponent<TrainCameraController>();
            follow.target = playerTransform;
            follow.offset = new Vector3(0f, 9.5f, -8.0f);
            follow.minX = -18f;
            follow.maxX = 25f;
            cam.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
        }

        // --- CONSTRUCCIÓN DE LA INTERFAZ UI ---
        private static void BuildUI(Transform parent)
        {
            GameObject canvasObj = new GameObject("SoulsALaCarte_Canvas");
            canvasObj.transform.SetParent(parent, false);

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(parent, false);
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();

                var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null)
                {
                    es.AddComponent(inputModuleType);
                }
                else
                {
                    es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            var uiMgr = canvasObj.AddComponent<UIManager>();
            Font font = GetSafeFont();

            uiMgr.mainMenuPanel = BuildMainMenu(canvasObj.transform, uiMgr, font);
            uiMgr.gameSelectPanel = BuildGameSelect(canvasObj.transform, uiMgr, font);
            uiMgr.storyDialoguePanel = BuildDialoguePanel(canvasObj.transform, uiMgr, font);

            // PANELES SEPARADOS DE NIVELES Y TIENDA
            uiMgr.levelSelectPanel = BuildLevelSelectPanel(canvasObj.transform, font);
            uiMgr.shopPanel = BuildShopPanel(canvasObj.transform, font);

            uiMgr.hudPanel = BuildInGameHUD(canvasObj.transform, uiMgr, font);
            uiMgr.pausePanel = BuildPauseMenu(canvasObj.transform, uiMgr, font);
            uiMgr.resultsPanel = BuildResultsMenu(canvasObj.transform, uiMgr, font);
            uiMgr.victoryPanel = BuildVictoryMenu(canvasObj.transform, uiMgr, font);
            uiMgr.optionsPanel = BuildOptionsMenu(canvasObj.transform, uiMgr, font);
            uiMgr.creditsPanel = BuildCreditsMenu(canvasObj.transform, uiMgr, font);
        }

        private static GameObject BuildMainMenu(Transform canvas, UIManager mgr, Font font)
        {
            var panel = CreateUIPanel("MainMenuPanel", canvas, new Color(0.08f, 0.05f, 0.09f, 0.95f));

            CreateUIText("Title", panel.transform, "SOULS A LA CARTE", 72, Color.red, font, new Vector2(0f, 260f), new Vector2(900f, 120f));
            CreateUIText("Subtitle", panel.transform, "El restaurante caótico del Soul Express", 26, new Color(1f, 0.8f, 0.3f), font, new Vector2(0f, 180f), new Vector2(800f, 60f));

            CreateUIButton("Btn_Play", panel.transform, "JUGAR", 28, font, new Vector2(0f, 60f), new Vector2(360f, 65f), mgr.OnClickPlay);
            CreateUIButton("Btn_Coop", panel.transform, "COOPERATIVO LOCAL", 22, font, new Vector2(0f, -15f), new Vector2(360f, 60f), mgr.OnClickCoopInfo);
            CreateUIButton("Btn_Options", panel.transform, "OPCIONES", 24, font, new Vector2(0f, -85f), new Vector2(360f, 60f), mgr.OnClickOptions);
            CreateUIButton("Btn_Credits", panel.transform, "CRÉDITOS", 24, font, new Vector2(0f, -155f), new Vector2(360f, 60f), mgr.OnClickCredits);
            CreateUIButton("Btn_Exit", panel.transform, "SALIR", 24, font, new Vector2(0f, -225f), new Vector2(360f, 60f), mgr.OnClickExit);

            return panel;
        }

        private static GameObject BuildGameSelect(Transform canvas, UIManager mgr, Font font)
        {
            var panel = CreateUIPanel("GameSelectPanel", canvas, new Color(0.1f, 0.08f, 0.12f, 0.98f));
            panel.SetActive(false);

            CreateUIText("Title", panel.transform, "SELECCIÓN DE PARTIDA", 52, Color.yellow, font, new Vector2(0f, 220f), new Vector2(700f, 80f));

            CreateUIButton("Btn_New", panel.transform, "NUEVA PARTIDA (Empezar Historia)", 24, font, new Vector2(0f, 60f), new Vector2(480f, 70f), mgr.OnClickNewGame);
            CreateUIButton("Btn_Continue", panel.transform, "CONTINUAR (Cargar Partida Guardada)", 24, font, new Vector2(0f, -30f), new Vector2(480f, 70f), mgr.OnClickContinue);
            CreateUIButton("Btn_Back", panel.transform, "Volver al Menú", 22, font, new Vector2(0f, -140f), new Vector2(300f, 55f), mgr.OnClickReturnToMainMenu);

            return panel;
        }

        private static GameObject BuildDialoguePanel(Transform canvas, UIManager mgr, Font font)
        {
            var panel = CreateUIPanel("StoryDialoguePanel", canvas, new Color(0.05f, 0.05f, 0.08f, 0.92f));
            panel.SetActive(false);

            var box = CreateUIPanel("DialogueBox", panel.transform, new Color(0.15f, 0.12f, 0.18f, 0.98f));
            var boxRect = box.GetComponent<RectTransform>();
            boxRect.sizeDelta = new Vector2(1100f, 480f);
            boxRect.anchoredPosition = new Vector2(0f, -50f);

            mgr.dialogueSpeakerText = CreateUIText("Speaker", box.transform, "Lord Mr. Sir Dr. Migafees", 30, Color.yellow, font, new Vector2(0f, 180f), new Vector2(1000f, 50f));
            mgr.dialogueContentText = CreateUIText("Content", box.transform, "...", 22, Color.white, font, new Vector2(0f, 20f), new Vector2(980f, 240f));

            mgr.dialogueNextButton = CreateUIButton("Btn_Next", box.transform, "Iniciar Tutorial (Nivel 1)", 24, font, new Vector2(0f, -165f), new Vector2(450f, 60f), null);
            mgr.dialogueBribeButton = CreateUIButton("Btn_Bribe", box.transform, "¡Pagar 50.000 y Escapar del Infierno!", 22, font, new Vector2(0f, -165f), new Vector2(450f, 60f), null);
            mgr.dialogueBribeButton.gameObject.SetActive(false);

            return panel;
        }

        // ========================================================
        // PANEL 1 SEPARADO: SELECTOR DE NIVELES (Tablero de la Cocina)
        // ========================================================
        private static GameObject BuildLevelSelectPanel(Transform canvas, Font font)
        {
            var panel = CreateUIPanel("LevelSelectPanel", canvas, new Color(0.08f, 0.06f, 0.1f, 0.98f));
            panel.SetActive(false);

            var lsScript = panel.AddComponent<LevelSelectUI>();

            // Cabecera
            var header = CreateUIPanel("Header", panel.transform, new Color(0.18f, 0.14f, 0.22f, 1f));
            var hRect = header.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 1f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.pivot = new Vector2(0.5f, 1f);
            hRect.sizeDelta = new Vector2(0f, 100f);
            hRect.anchoredPosition = Vector2.zero;

            CreateUIText("Logo", header.transform, "TABLERO DEL SOUL EXPRESS\nSelección de Nivel", 28, Color.yellow, font, new Vector2(-550f, 0f), new Vector2(600f, 80f));
            lsScript.headerMoneyText = CreateUIText("Money", header.transform, "600 ❖", 32, new Color(1f, 0.85f, 0.2f), font, new Vector2(300f, 10f), new Vector2(300f, 50f));
            lsScript.headerLevelCountText = CreateUIText("LvlCount", header.transform, "Niveles: 1 / 10", 18, Color.white, font, new Vector2(300f, -25f), new Vector2(300f, 30f));

            CreateUIButton("Btn_CloseLevels", header.transform, "Volver al Tren", 20, font, new Vector2(750f, 0f), new Vector2(200f, 55f), lsScript.ClosePanel);

            // Contenedor Grid Niveles
            var gridObj = new GameObject("LevelGrid");
            gridObj.transform.SetParent(panel.transform, false);
            var gRect = gridObj.AddComponent<RectTransform>();
            gRect.anchorMin = new Vector2(0.05f, 0.05f);
            gRect.anchorMax = new Vector2(0.62f, 0.85f);
            gRect.offsetMin = Vector2.zero;
            gRect.offsetMax = Vector2.zero;

            var grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(200f, 140f);
            grid.spacing = new Vector2(20f, 20f);
            lsScript.levelCardsParent = gridObj.transform;

            for (int i = 1; i <= 10; i++)
            {
                CreateUIButton($"Card_Lvl_{i}", gridObj.transform, $"Nivel {i}", 16, font, Vector2.zero, Vector2.zero, null);
            }

            // Panel Inspector de Nivel
            var insp = CreateUIPanel("LevelInspector", panel.transform, new Color(0.18f, 0.13f, 0.22f, 1f));
            var iRect = insp.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0.65f, 0.05f);
            iRect.anchorMax = new Vector2(0.95f, 0.85f);
            iRect.offsetMin = Vector2.zero;
            iRect.offsetMax = Vector2.zero;

            lsScript.selectedLevelTitleText = CreateUIText("Title", insp.transform, "NIVEL 1", 28, Color.yellow, font, new Vector2(0f, 320f), new Vector2(450f, 60f));
            lsScript.selectedLevelSubText = CreateUIText("Sub", insp.transform, "Clase: Tonius", 20, Color.cyan, font, new Vector2(0f, 260f), new Vector2(450f, 40f));
            lsScript.selectedLevelDescText = CreateUIText("Desc", insp.transform, "Descripción...", 16, Color.white, font, new Vector2(0f, 130f), new Vector2(450f, 200f));
            lsScript.selectedLevelRewardsText = CreateUIText("Rewards", insp.transform, "Recompensas...", 16, Color.green, font, new Vector2(0f, -50f), new Vector2(450f, 80f));
            lsScript.selectedLevelConditionsText = CreateUIText("Cond", insp.transform, "Condiciones...", 16, new Color(1f, 0.5f, 0.2f), font, new Vector2(0f, -140f), new Vector2(450f, 70f));

            lsScript.startLevelButton = CreateUIButton("Btn_StartLevel", insp.transform, "¡INICIAR NIVEL!", 26, font, new Vector2(0f, -280f), new Vector2(420f, 75f), null);

            return panel;
        }

        // ========================================================
        // PANEL 2 SEPARADO: TIENDA DE MIGAFEES (En la Cabina del Maquinista)
        // ========================================================
        private static GameObject BuildShopPanel(Transform canvas, Font font)
        {
            var panel = CreateUIPanel("ShopPanel", canvas, new Color(0.08f, 0.06f, 0.1f, 0.98f));
            panel.SetActive(false);

            var shopScript = panel.AddComponent<ShopUI>();

            // Cabecera de la tienda
            var header = CreateUIPanel("Header", panel.transform, new Color(0.22f, 0.15f, 0.10f, 1f));
            var hRect = header.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 1f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.pivot = new Vector2(0.5f, 1f);
            hRect.sizeDelta = new Vector2(0f, 110f);
            hRect.anchoredPosition = Vector2.zero;

            CreateUIText("Logo", header.transform, "TIENDA DE LORD MIGAFEES\nMejoras & Ayudantes", 28, new Color(1f, 0.85f, 0.2f), font, new Vector2(-550f, 0f), new Vector2(600f, 80f));
            shopScript.headerMoneyText = CreateUIText("Money", header.transform, "600 ❖", 32, new Color(1f, 0.85f, 0.2f), font, new Vector2(150f, 15f), new Vector2(250f, 50f));
            shopScript.headerTargetText = CreateUIText("Target", header.transform, "Soborno: 50.000 ❖", 18, Color.white, font, new Vector2(450f, 25f), new Vector2(300f, 35f));

            var sliderObj = new GameObject("FreedomSlider");
            sliderObj.transform.SetParent(header.transform, false);
            var sRect = sliderObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(450f, -15f);
            sRect.sizeDelta = new Vector2(300f, 20f);
            var slider = sliderObj.AddComponent<Slider>();
            shopScript.headerFreedomSlider = slider;

            var slBg = CreateUIPanel("SlBg", sliderObj.transform, Color.gray);
            var slFill = CreateUIPanel("SlFill", sliderObj.transform, Color.green);
            slider.fillRect = slFill.GetComponent<RectTransform>();

            shopScript.headerPercentageText = CreateUIText("Pct", header.transform, "1%", 18, Color.white, font, new Vector2(625f, -15f), new Vector2(80f, 30f));

            shopScript.closeButton = CreateUIButton("Btn_CloseShop", header.transform, "Volver al Tren", 20, font, new Vector2(780f, 0f), new Vector2(180f, 55f), shopScript.ClosePanel);

            // Grid de Mejoras (Lado izquierdo)
            var shopGrid = new GameObject("ShopGrid");
            shopGrid.transform.SetParent(panel.transform, false);
            var sgRect = shopGrid.AddComponent<RectTransform>();
            sgRect.anchorMin = new Vector2(0.04f, 0.05f);
            sgRect.anchorMax = new Vector2(0.62f, 0.85f);
            sgRect.offsetMin = Vector2.zero;
            sgRect.offsetMax = Vector2.zero;

            var sGrid = shopGrid.AddComponent<GridLayoutGroup>();
            sGrid.cellSize = new Vector2(330f, 210f);
            sGrid.spacing = new Vector2(20f, 20f);
            shopScript.shopCardsParent = shopGrid.transform;

            string[] shopIds = new[] { "Card_Speed", "Card_CookSpeed", "Card_Plates", "Card_Drinks", "Card_Tips", "Card_Color" };
            foreach (var id in shopIds)
            {
                var card = CreateUIPanel(id, shopGrid.transform, new Color(0.2f, 0.15f, 0.25f, 1f));
                CreateUIText("Title", card.transform, "Mejora", 20, Color.yellow, font, new Vector2(0f, 65f), new Vector2(310f, 50f));
                CreateUIText("Desc", card.transform, "Detalles...", 15, Color.white, font, new Vector2(0f, 5f), new Vector2(310f, 70f));
                CreateUIButton("Btn_Buy", card.transform, "Comprar", 18, font, new Vector2(0f, -65f), new Vector2(200f, 45f), null);
            }

            // Panel Lateral Derecho: Ayudante Giuseppe + Soborno de Libertad
            var sidePanel = CreateUIPanel("SidePanel", panel.transform, new Color(0.14f, 0.12f, 0.18f, 1f));
            var spRect = sidePanel.GetComponent<RectTransform>();
            spRect.anchorMin = new Vector2(0.64f, 0.05f);
            spRect.anchorMax = new Vector2(0.96f, 0.85f);
            spRect.offsetMin = Vector2.zero;
            spRect.offsetMax = Vector2.zero;

            // Ficha Giuseppe
            var gCard = CreateUIPanel("GiuseppeCard", sidePanel.transform, new Color(0.18f, 0.25f, 0.2f, 1f));
            var gcRect = gCard.GetComponent<RectTransform>();
            gcRect.anchorMin = new Vector2(0f, 0.45f);
            gcRect.anchorMax = new Vector2(1f, 1f);
            gcRect.offsetMin = new Vector2(15f, 15f);
            gcRect.offsetMax = new Vector2(-15f, -15f);

            CreateUIText("GTitle", gCard.transform, "AYUDANTE: GIUSEPPE", 28, Color.green, font, new Vector2(0f, 140f), new Vector2(500f, 50f));
            shopScript.giuseppeStatusText = CreateUIText("GStatus", gCard.transform, "...", 18, Color.white, font, new Vector2(0f, 30f), new Vector2(500f, 140f));
            shopScript.hireGiuseppeButton = CreateUIButton("Btn_Hire", gCard.transform, "Contratar (80 ❖)", 22, font, new Vector2(0f, -120f), new Vector2(360f, 60f), null);
            shopScript.hireGiuseppeButtonText = shopScript.hireGiuseppeButton.GetComponentInChildren<Text>();

            // Ficha Soborno de Libertad
            var bCard = CreateUIPanel("BribeCard", sidePanel.transform, new Color(0.28f, 0.18f, 0.1f, 1f));
            var bcRect = bCard.GetComponent<RectTransform>();
            bcRect.anchorMin = new Vector2(0f, 0f);
            bcRect.anchorMax = new Vector2(1f, 0.42f);
            bcRect.offsetMin = new Vector2(15f, 15f);
            bcRect.offsetMax = new Vector2(-15f, -15f);

            CreateUIText("BTitle", bCard.transform, "SOBORNO DE LIBERTAD", 26, Color.yellow, font, new Vector2(0f, 90f), new Vector2(500f, 40f));
            shopScript.bribeStatusText = CreateUIText("BStatus", bCard.transform, "Paga 50.000 óbolos para escapar del Infierno al Paraíso.", 17, Color.white, font, new Vector2(0f, 25f), new Vector2(480f, 70f));
            shopScript.bribeFreedomButton = CreateUIButton("Btn_BribeFreedom", bCard.transform, "¡Comprar Libertad!", 22, font, new Vector2(0f, -50f), new Vector2(360f, 60f), null);

            return panel;
        }

        private static GameObject BuildInGameHUD(Transform canvas, UIManager mgr, Font font)
        {
            var panel = CreateUIPanel("HUDPanel", canvas, Color.clear);
            panel.SetActive(false);

            // Banner Tutorial
            var tutBanner = CreateUIPanel("TutorialBanner", panel.transform, new Color(0.25f, 0.18f, 0.05f, 0.95f));
            var tbRect = tutBanner.GetComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0.5f, 1f);
            tbRect.anchorMax = new Vector2(0.5f, 1f);
            tbRect.pivot = new Vector2(0.5f, 1f);
            tbRect.sizeDelta = new Vector2(850f, 65f);
            tbRect.anchoredPosition = new Vector2(0f, -15f);
            mgr.hudTutorialBannerObj = tutBanner;
            mgr.hudTutorialBannerText = CreateUIText("TutorialText", tutBanner.transform, "Tutorial", 18, Color.yellow, font, Vector2.zero, new Vector2(820f, 55f));
            tutBanner.SetActive(false);

            // Banner Superior Izquierdo: Clase de Clientes
            var classBanner = CreateUIPanel("ClassBanner", panel.transform, new Color(0.12f, 0.08f, 0.16f, 0.92f));
            var cbRect = classBanner.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0f, 1f);
            cbRect.anchorMax = new Vector2(0f, 1f);
            cbRect.pivot = new Vector2(0f, 1f);
            cbRect.sizeDelta = new Vector2(400f, 105f);
            cbRect.anchoredPosition = new Vector2(25f, -20f);

            CreateUIText("ClassHeader", classBanner.transform, "CLASE DE CLIENTES", 13, new Color(0.95f, 0.8f, 0.3f), font, new Vector2(0f, 35f), new Vector2(380f, 25f));
            mgr.hudClassTitleText = CreateUIText("Title", classBanner.transform, "TONIUS", 26, Color.red, font, new Vector2(0f, 10f), new Vector2(380f, 36f));
            mgr.hudClassPhraseText = CreateUIText("Phrase", classBanner.transform, "«Poca comida, mucha prisa»", 15, new Color(0.9f, 0.9f, 0.95f), font, new Vector2(0f, -22f), new Vector2(380f, 30f));

            // Panel Lateral Izquierdo: Lista de Pedidos
            var ordersPanel = CreateUIPanel("OrdersPanel", panel.transform, new Color(0.10f, 0.07f, 0.14f, 0.88f));
            var opRect = ordersPanel.GetComponent<RectTransform>();
            opRect.anchorMin = new Vector2(0f, 0.5f);
            opRect.anchorMax = new Vector2(0f, 0.5f);
            opRect.pivot = new Vector2(0f, 0.5f);
            opRect.sizeDelta = new Vector2(280f, 480f);
            opRect.anchoredPosition = new Vector2(25f, -30f);

            var ordersHeader = CreateUIPanel("OrdersHeader", ordersPanel.transform, new Color(0.24f, 0.08f, 0.12f, 0.95f));
            var ohRect = ordersHeader.GetComponent<RectTransform>();
            ohRect.anchorMin = new Vector2(0f, 1f);
            ohRect.anchorMax = new Vector2(1f, 1f);
            ohRect.pivot = new Vector2(0.5f, 1f);
            ohRect.sizeDelta = new Vector2(0f, 36f);
            ohRect.anchoredPosition = Vector2.zero;
            CreateUIText("HeaderLabel", ordersHeader.transform, "PEDIDOS PENDIENTES", 15, new Color(1f, 0.85f, 0.2f), font, Vector2.zero, new Vector2(260f, 32f));

            var ordersContainer = new GameObject("OrdersContainer");
            ordersContainer.transform.SetParent(ordersPanel.transform, false);
            var ocRect = ordersContainer.AddComponent<RectTransform>();
            ocRect.anchorMin = new Vector2(0f, 0f);
            ocRect.anchorMax = new Vector2(1f, 1f);
            ocRect.offsetMin = new Vector2(10f, 10f);
            ocRect.offsetMax = new Vector2(-10f, -44f);

            var vlg = ordersContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            mgr.hudOrdersContainer = ordersContainer.transform;

            // Superior Derecho: Dinero, Tiempo, Libertad y Vidas/Fallos
            // Nota: En nivel activo NO se muestra botón de abrir menú, solo ESC para pausa
            var statsBanner = CreateUIPanel("StatsBanner", panel.transform, new Color(0.12f, 0.08f, 0.16f, 0.92f));
            var sbRect = statsBanner.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(1f, 1f);
            sbRect.anchorMax = new Vector2(1f, 1f);
            sbRect.pivot = new Vector2(1f, 1f);
            sbRect.sizeDelta = new Vector2(560f, 155f);
            sbRect.anchoredPosition = new Vector2(-25f, -20f);

            mgr.hudTimerText = CreateUIText("Timer", statsBanner.transform, "Tiempo: 01:30", 24, Color.cyan, font, new Vector2(0f, 48f), new Vector2(400f, 35f));

            mgr.hudMoneyText = CreateUIText("Money", statsBanner.transform, "Ronda: +0 ❖  |  Total: 600 ❖ / 50.000 ❖", 17, Color.yellow, font, new Vector2(0f, 12f), new Vector2(520f, 30f));

            var sliderObj = new GameObject("FreedomBar");
            sliderObj.transform.SetParent(statsBanner.transform, false);
            var slRect = sliderObj.AddComponent<RectTransform>();
            slRect.anchoredPosition = new Vector2(0f, -22f);
            slRect.sizeDelta = new Vector2(520f, 16f);
            var slider = sliderObj.AddComponent<Slider>();
            mgr.hudFreedomSlider = slider;
            var slBg = CreateUIPanel("Bg", sliderObj.transform, new Color(0.25f, 0.25f, 0.25f, 0.9f));
            var slFill = CreateUIPanel("Fill", sliderObj.transform, new Color(0.1f, 0.85f, 0.3f));
            slider.fillRect = slFill.GetComponent<RectTransform>();

            mgr.hudFailuresText = CreateUIText("FailuresText", statsBanner.transform, $"Fallos: 0/{GameManager.MAX_ALLOWED_FAILURES} (Con {GameManager.MAX_ALLOWED_FAILURES} pierdes)", 16, new Color(1f, 0.4f, 0.4f), font, new Vector2(0f, -50f), new Vector2(520f, 28f));

            // Inferior Central: Objeto en Manos + Interactuable cercano + Acción
            var promptBanner = CreateUIPanel("InteractionHUD", panel.transform, new Color(0.11f, 0.08f, 0.15f, 0.94f));
            var pbRect = promptBanner.GetComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0.5f, 0f);
            pbRect.anchorMax = new Vector2(0.5f, 0f);
            pbRect.pivot = new Vector2(0.5f, 0f);
            pbRect.sizeDelta = new Vector2(850f, 105f);
            pbRect.anchoredPosition = new Vector2(0f, 18f);

            mgr.hudCarriedItemText = CreateUIText("CarriedItem", promptBanner.transform, "En manos: Manos vacías  |  [Q] Soltar", 17, new Color(1f, 0.85f, 0.2f), font, new Vector2(0f, 30f), new Vector2(820f, 28f));
            mgr.hudTargetNameText = CreateUIText("TargetName", promptBanner.transform, "Frente a: Ninguno cercano", 16, Color.white, font, new Vector2(0f, 2f), new Vector2(820f, 26f));
            mgr.hudActionPromptText = CreateUIText("ActionPrompt", promptBanner.transform, "Acción: [WASD] Moverse  |  [Shift] Correr  |  [E] Interactuar", 15, new Color(0.2f, 0.95f, 0.8f), font, new Vector2(0f, -26f), new Vector2(820f, 26f));
            mgr.hudPromptText = mgr.hudActionPromptText;

            // Overlays
            mgr.blurSprayOverlay = CreateUIPanel("BlurSprayOverlay", panel.transform, new Color(0.95f, 0.9f, 0.7f, 0.65f));
            mgr.blurSprayOverlay.SetActive(false);
            CreateUIText("BlurMsg", mgr.blurSprayOverlay.transform, "¡SPRAY A LOS OJOS!\nVisión nublada durante 5 segundos...", 46, Color.red, font, Vector2.zero, new Vector2(900f, 160f));

            mgr.bossWarningBanner = CreateUIPanel("BossWarning", panel.transform, new Color(0.85f, 0.1f, 0.1f, 0.92f));
            var bwRect = mgr.bossWarningBanner.GetComponent<RectTransform>();
            bwRect.sizeDelta = new Vector2(900f, 140f);
            bwRect.anchoredPosition = new Vector2(0f, 220f);
            mgr.bossWarningText = CreateUIText("BossText", mgr.bossWarningBanner.transform, "¡ALERTA DE JEFE!", 32, Color.yellow, font, Vector2.zero, new Vector2(860f, 120f));
            mgr.bossWarningBanner.SetActive(false);

            mgr.hudNotificationText = CreateUIText("NotificationText", panel.transform, "Aviso", 26, Color.yellow, font, new Vector2(0f, 140f), new Vector2(900f, 65f));
            mgr.hudNotificationText.gameObject.SetActive(false);

            return panel;
        }

        private static GameObject BuildPauseMenu(Transform canvas, UIManager mgr, Font font)
        {
            var panel = CreateUIPanel("PausePanel", canvas, new Color(0.05f, 0.05f, 0.08f, 0.9f));
            panel.SetActive(false);

            CreateUIText("Title", panel.transform, "PAUSA", 56, Color.yellow, font, new Vector2(0f, 180f), new Vector2(400f, 80f));

            CreateUIButton("Btn_Resume", panel.transform, "Reanudar", 24, font, new Vector2(0f, 50f), new Vector2(340f, 65f), mgr.OnClickResume);
            CreateUIButton("Btn_Options", panel.transform, "Opciones y Controles", 24, font, new Vector2(0f, -30f), new Vector2(340f, 65f), mgr.OnClickOptions);
            CreateUIButton("Btn_Abandon", panel.transform, "Abandonar al Tren", 24, font, new Vector2(0f, -110f), new Vector2(340f, 65f), mgr.OnClickAbandonRound);
            CreateUIButton("Btn_MainMenu", panel.transform, "Menú Principal", 24, font, new Vector2(0f, -190f), new Vector2(340f, 65f), mgr.OnClickReturnToMainMenu);

            return panel;
        }

        private static GameObject BuildResultsMenu(Transform canvas, UIManager mgr, Font font)
        {
            var panel = CreateUIPanel("ResultsPanel", canvas, new Color(0.08f, 0.06f, 0.1f, 0.95f));
            panel.SetActive(false);

            var box = CreateUIPanel("Box", panel.transform, new Color(0.16f, 0.12f, 0.22f, 1f));
            var bRect = box.GetComponent<RectTransform>();
            bRect.sizeDelta = new Vector2(750f, 600f);

            mgr.resultsTitleText = CreateUIText("Title", box.transform, "¡Nivel Completado!", 36, Color.yellow, font, new Vector2(0f, 220f), new Vector2(700f, 60f));
            mgr.resultsEarnedText = CreateUIText("Earned", box.transform, "Ganancias: +500 ❖", 22, Color.white, font, new Vector2(0f, 140f), new Vector2(600f, 40f));
            mgr.resultsTipsText = CreateUIText("Tips", box.transform, "Propinas: +120 ❖", 22, Color.green, font, new Vector2(0f, 90f), new Vector2(600f, 40f));
            mgr.resultsPenaltiesText = CreateUIText("Penalties", box.transform, "Sanciones: -0 ❖", 22, Color.red, font, new Vector2(0f, 40f), new Vector2(600f, 40f));
            mgr.resultsOrdersText = CreateUIText("Orders", box.transform, "Entregados: 8 | Fallidos: 0", 22, Color.cyan, font, new Vector2(0f, -10f), new Vector2(600f, 40f));
            mgr.resultsStarsText = CreateUIText("Stars", box.transform, "Calificación: ★ ★ ★", 30, Color.yellow, font, new Vector2(0f, -70f), new Vector2(600f, 50f));

            CreateUIButton("Btn_Continue", box.transform, "Continuar al Tren", 24, font, new Vector2(0f, -160f), new Vector2(380f, 65f), mgr.OnClickResultsContinue);
            CreateUIButton("Btn_Retry", box.transform, "Repetir Nivel", 22, font, new Vector2(0f, -235f), new Vector2(380f, 55f), mgr.OnClickResultsRetry);

            return panel;
        }

        private static GameObject BuildVictoryMenu(Transform canvas, UIManager mgr, Font font)
        {
            var panel = CreateUIPanel("VictoryPanel", canvas, new Color(0.05f, 0.04f, 0.08f, 0.98f));
            panel.SetActive(false);

            CreateUIText("Title", panel.transform, "¡HAS COMPRADO TU LIBERTAD!", 54, Color.yellow, font, new Vector2(0f, 200f), new Vector2(1100f, 80f));
            CreateUIText("Story", panel.transform, "Francis pagó el colosal soborno de 50.000 monedas a Lord Mr. Sir Dr. Migafees Do Santos III jr.\n\nEl Soul Express frenó ante las puertas doradas del Paraíso.\n\nTu alma ha sido liberada del contrato del Diablo.", 24, Color.white, font, new Vector2(0f, 0f), new Vector2(900f, 220f));

            CreateUIButton("Btn_PlayAgain", panel.transform, "Volver al Menú Principal", 26, font, new Vector2(0f, -180f), new Vector2(420f, 70f), mgr.OnClickReturnToMainMenu);

            return panel;
        }

        private static GameObject BuildOptionsMenu(Transform canvas, UIManager mgr, Font font)
        {
            var panel = CreateUIPanel("OptionsPanel", canvas, new Color(0.08f, 0.08f, 0.12f, 0.98f));
            panel.SetActive(false);

            CreateUIText("Title", panel.transform, "OPCIONES Y CONTROLES", 46, Color.yellow, font, new Vector2(0f, 220f), new Vector2(800f, 70f));
            CreateUIText("Controls", panel.transform, "CONTROLES:\n\n[W, A, S, D] o [Flechas]: Moverse por los 4 vagones\n[Shift Izquierdo]: Correr\n[E] o [Barra Espaciadora]: Tomar comanda / Cocinar / Servir / Fregar / Hablar con Migafees / Tablero\n[Q]: Soltar objeto llevado\n[Escape]: Menú de Pausa (único menú accesible durante la ronda)", 22, Color.white, font, new Vector2(0f, 40f), new Vector2(850f, 230f));

            var volLabel = CreateUIText("VolLabel", panel.transform, "Volumen General:", 22, Color.yellow, font, new Vector2(-150f, -110f), new Vector2(250f, 40f));
            var sliderObj = new GameObject("VolumeSlider");
            sliderObj.transform.SetParent(panel.transform, false);
            var slRect = sliderObj.AddComponent<RectTransform>();
            slRect.anchoredPosition = new Vector2(100f, -110f);
            slRect.sizeDelta = new Vector2(280f, 25f);
            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            mgr.optionsVolumeSlider = slider;

            var slBg = CreateUIPanel("Bg", sliderObj.transform, Color.gray);
            var slFill = CreateUIPanel("Fill", sliderObj.transform, Color.yellow);
            slider.fillRect = slFill.GetComponent<RectTransform>();

            CreateUIButton("Btn_Close", panel.transform, "Cerrar", 22, font, new Vector2(0f, -220f), new Vector2(260f, 60f), mgr.OnClickCloseOptions);

            return panel;
        }

        private static GameObject BuildCreditsMenu(Transform canvas, UIManager mgr, Font font)
        {
            var panel = CreateUIPanel("CreditsPanel", canvas, new Color(0.08f, 0.08f, 0.12f, 0.98f));
            panel.SetActive(false);

            CreateUIText("Title", panel.transform, "CRÉDITOS", 52, Color.yellow, font, new Vector2(0f, 220f), new Vector2(600f, 80f));
            CreateUIText("Info", panel.transform, "SOULS A LA CARTE\n\nDesarrollado para Unity 6\nBasado en el Game Design Document del Soul Express (Fenre)\n\nPersonajes:\nFrancis (Cocinero)\nLord Mr. Sir Dr. Migafees Do Santos III jr (Maquinista)\nGiuseppe (Ayudante de cocina)\nJohanus, Traitor & Kalaya (Jefes de los comensales)\n\n¡Gracias por jugar!", 22, Color.white, font, new Vector2(0f, 10f), new Vector2(850f, 320f));

            CreateUIButton("Btn_Close", panel.transform, "Cerrar", 22, font, new Vector2(0f, -220f), new Vector2(260f, 60f), mgr.OnClickCloseCredits);

            return panel;
        }

        private static GameObject CreateUIPanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static Text CreateUIText(string name, Transform parent, string content, int fontSize, Color color, Font font, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var txt = go.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.font = font;
            txt.alignment = TextAnchor.MiddleCenter;
            return txt;
        }

        private static Button CreateUIButton(string name, Transform parent, string label, int fontSize, Font font, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            if (pos != Vector2.zero || size != Vector2.zero)
            {
                rect.anchoredPosition = pos;
                rect.sizeDelta = size;
            }

            var img = go.AddComponent<Image>();
            img.color = new Color(0.75f, 0.16f, 0.12f, 1f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.95f, 0.45f, 0.15f, 1f);
            colors.pressedColor = new Color(0.55f, 0.1f, 0.08f, 1f);
            btn.colors = colors;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(go.transform, false);
            var tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            var txt = textObj.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.font = font;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;

            if (onClick != null)
            {
                btn.onClick.AddListener(onClick);
            }

            return btn;
        }
    }
}
