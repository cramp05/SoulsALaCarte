using UnityEngine;
using SoulsALaCarte.Cooking;
using SoulsALaCarte.Dining;
using SoulsALaCarte.Core;
using SoulsALaCarte.UI;

namespace SoulsALaCarte.Characters
{
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        public float baseMoveSpeed = 4.8f;
        public float sprintMultiplier = 1.35f;
        public float interactionRange = 2.25f; // Rango amplio para alcanzar almas y mesas cómodamente

        public Transform handSlot;
        public HeldItem carriedItem;

        public bool isStunned = false;
        private float stunTimer = 0f;

        private bool isBusyWithAction = false;
        private System.Action busyCancelAction = null;

        private CharacterController characterController;
        private GameObject chefHat;
        private GameObject stunStarsVisual;

        private void Awake()
        {
            Instance = this;
            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
                characterController.height = 1.5f;
                characterController.radius = 0.35f;
                characterController.center = new Vector3(0f, 0.75f, 0f);
            }

            SetupVisuals();
        }

        private void SetupVisuals()
        {
            handSlot = transform.Find("HandSlot");
            if (handSlot == null)
            {
                var go = new GameObject("HandSlot");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 0.75f, 0.55f);
                handSlot = go.transform;
            }

            var hatTransform = transform.Find("Visual/Head/ChefHat");
            if (hatTransform != null) chefHat = hatTransform.gameObject;

            stunStarsVisual = new GameObject("StunStars");
            stunStarsVisual.transform.SetParent(transform, false);
            stunStarsVisual.transform.localPosition = new Vector3(0f, 1.85f, 0f);

            for (int i = 0; i < 3; i++)
            {
                var star = GameObject.CreatePrimitive(PrimitiveType.Cube);
                star.transform.SetParent(stunStarsVisual.transform, false);
                float angle = i * (360f / 3f);
                star.transform.localPosition = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 0.35f, 0f, Mathf.Sin(angle * Mathf.Deg2Rad) * 0.35f);
                star.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                Destroy(star.GetComponent<Collider>());
                var rend = star.GetComponent<Renderer>();
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
                rend.material.color = Color.yellow;
            }
            stunStarsVisual.SetActive(false);
        }

        private void Update()
        {
            if (isStunned)
            {
                stunTimer -= Time.deltaTime;
                if (stunStarsVisual != null)
                {
                    stunStarsVisual.transform.Rotate(0f, 320f * Time.deltaTime, 0f);
                }

                if (stunTimer <= 0f)
                {
                    isStunned = false;
                    if (stunStarsVisual != null) stunStarsVisual.SetActive(false);
                }
                return;
            }

            if (isBusyWithAction)
            {
                if (Mathf.Abs(InputBridge.GetHorizontal()) > 0.1f || Mathf.Abs(InputBridge.GetVertical()) > 0.1f || InputBridge.GetSecondaryActionDown())
                {
                    CancelBusyAction();
                }
                return;
            }

            // Tecla Q para soltar objeto llevado
            if (InputBridge.GetSecondaryActionDown() && carriedItem != null)
            {
                DropItem();
                return;
            }

            HandleMovement();
            HandleInteractionDetection();
        }

        private void HandleMovement()
        {
            float h = InputBridge.GetHorizontal();
            float v = InputBridge.GetVertical();

            Vector3 moveDir = new Vector3(h, 0f, v).normalized;

            if (moveDir.sqrMagnitude > 0.01f)
            {
                float speedBonus = GameManager.Instance != null ? GameManager.Instance.GetSpeedMultiplier() : 1f;
                float currentSpeed = baseMoveSpeed * speedBonus;
                if (InputBridge.GetSprint()) currentSpeed *= sprintMultiplier;

                characterController.Move(moveDir * currentSpeed * Time.deltaTime);
                characterController.Move(Vector3.down * 9.8f * Time.deltaTime);

                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 15f * Time.deltaTime);

                if (chefHat != null)
                {
                    chefHat.transform.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 12f) * 6f, 0f, 0f);
                }
            }
        }

        private void HandleInteractionDetection()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up * 0.5f, interactionRange);

            MonoBehaviour closestTarget = null;
            float minDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var target = hit.GetComponentInParent<MonoBehaviour>();
                if (target == null || target == this) continue;

                if (target is FoodLickerCooker || target is DrinkDispenser || target is CupDispenser ||
                    target is SoulDispenser || target is CleanPlateStack || target is SinkStation ||
                    target is TrashBin || target is CounterTable || target is CustomerTable || target is ConductorNPC ||
                    target is ManagementBoard || (target is HeldItem groundItem && groundItem != carriedItem && groundItem.transform.parent == null))
                {
                    float dist = Vector3.Distance(transform.position, target.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestTarget = target;
                    }
                }
            }

            string carriedText = carriedItem != null ? carriedItem.itemName : "Manos vacías";
            string targetName = closestTarget != null ? GetInteractableName(closestTarget) : "Ninguno cercano";
            string actionText = closestTarget != null ? GetInteractionAction(closestTarget) : "[WASD]: Moverse  |  [Shift]: Correr";

            if (closestTarget != null && InputBridge.GetInteractDown())
            {
                PerformInteraction(closestTarget);
            }

            UIManager.Instance?.SetInteractionInfo(carriedText, targetName, actionText);
        }

        public static string GetInteractableName(MonoBehaviour target)
        {
            if (target is FoodLickerCooker cooker) return $"FoodLicker #{cooker.cauldronIndex} (Caldero Sentiente)";
            if (target is DrinkDispenser dispenser)
            {
                string dName = dispenser.drinkType == DrinkType.FizzUp ? "Elixir" : dispenser.drinkType.ToString();
                return $"Surtidor de Bebida ({dName})";
            }
            if (target is CupDispenser) return "Dispensador de Vasos Desechables";
            if (target is SoulDispenser sDisp)
            {
                string sName = "";
                switch (sDisp.soulType)
                {
                    case SoulType.White: sName = "Dulce (Blanca)"; break;
                    case SoulType.Blue: sName = "Pescado (Azul)"; break;
                    case SoulType.Yellow: sName = "Pasta (Amarilla)"; break;
                    case SoulType.Red: sName = "Carne (Roja)"; break;
                }
                return $"Estantería de Almas: {sName}";
            }
            if (target is CleanPlateStack) return "Estantería de Platos Limpios";
            if (target is SinkStation) return "Fregadero de Platos Sucios";
            if (target is TrashBin) return "Cubo de Basura Infernal";
            if (target is CounterTable) return "Mesa de Preparación (Apoyo)";
            if (target is CustomerTable cTable) return $"Mesa de Comedor #{cTable.tableNumber}";
            if (target is ConductorNPC) return "Lord Mr. Sir Dr. Migafees (Maquinista)";
            if (target is ManagementBoard) return "Tablero de Niveles del Soul Express";
            if (target is HeldItem item) return $"{item.itemName} (Suelo)";

            return "Objeto interactivo";
        }

        private string GetInteractionAction(MonoBehaviour target)
        {
            if (target is CustomerTable cTable)
            {
                if (cTable.currentCustomer != null && !cTable.currentCustomer.isOrderTaken)
                {
                    return "[E / ESPACIO] Tomar comanda del cliente";
                }
                if (carriedItem != null && (carriedItem.category == ItemCategory.CookedDish || carriedItem.category == ItemCategory.FilledDrink))
                {
                    return "[E / ESPACIO] Servir pedido al cliente";
                }
                if (carriedItem == null && cTable.HasDirtyPlate)
                {
                    return "[E / ESPACIO] Recoger plato sucio";
                }
                return cTable.currentCustomer != null ? "Esperando la comida..." : "Mesa libre";
            }
            if (target is FoodLickerCooker cooker)
            {
                if (cooker.state == FoodLickerCooker.CookerState.Ready)
                {
                    return carriedItem == null ? "[E / ESPACIO] Recoger plato preparado" : "¡Manos ocupadas para recoger!";
                }
                if (carriedItem != null && (carriedItem.category == ItemCategory.CleanPlate || carriedItem.category == ItemCategory.SoulIngredient))
                {
                    return "[E / ESPACIO] Añadir ingrediente al caldero";
                }
                if (carriedItem == null && cooker.CanManualStartCooking())
                {
                    return "[E / ESPACIO] ¡Iniciar cocción en FoodLicker!";
                }
                return "Añade un plato y almas para cocinar";
            }
            if (target is DrinkDispenser dispenser)
            {
                if (carriedItem != null && carriedItem.category == ItemCategory.EmptyCup)
                    return $"[E / ESPACIO] Llenar vaso con {dispenser.drinkType}";
                return "Lleva un vaso vacío para rellenarlo";
            }
            if (target is CupDispenser)
            {
                if (carriedItem == null) return "[E / ESPACIO] Coger vaso desechable";
                return "Tienes las manos ocupadas";
            }
            if (target is SoulDispenser sDisp)
            {
                if (carriedItem == null) return $"[E / ESPACIO] Coger alma {sDisp.soulType}";
                return "Tienes las manos ocupadas";
            }
            if (target is CleanPlateStack pStack)
            {
                if (carriedItem == null) return "[E / ESPACIO] Coger plato limpio";
                if (carriedItem.category == ItemCategory.CleanPlate) return "[E / ESPACIO] Devolver plato a la estantería";
                return "Manos ocupadas";
            }
            if (target is SinkStation sink)
            {
                if (carriedItem != null && carriedItem.category == ItemCategory.DirtyPlate)
                    return "[E / ESPACIO] Dejar plato sucio para lavar";
                if (sink.hasDirtyPlate && !sink.isWashing)
                    return "[E / ESPACIO] Lavar plato en el fregadero";
                return sink.isWashing ? "Lavando..." : "Fregadero vacío";
            }
            if (target is TrashBin)
            {
                if (carriedItem != null) return "[E / ESPACIO] Tirar a la basura";
                return "Cubo de basura";
            }
            if (target is CounterTable table)
            {
                if (carriedItem != null && table.CanPlaceItem()) return "[E / ESPACIO] Dejar objeto en la mesa";
                if (carriedItem == null && table.HasItem()) return "[E / ESPACIO] Coger objeto de la mesa";
                return table.HasItem() ? "Mesa ocupada" : "Mesa libre de apoyo";
            }
            if (target is ConductorNPC)
            {
                return "[E / ESPACIO] Hablar con Migafees (Abrir Tienda / Mejoras)";
            }
            if (target is ManagementBoard)
            {
                return "[E / ESPACIO] Abrir Panel de Niveles";
            }
            if (target is HeldItem groundItem)
            {
                if (carriedItem == null) return $"[E / ESPACIO] Recoger {groundItem.itemName}";
                return "Manos ocupadas";
            }

            return "[E / ESPACIO] Interactuar";
        }

        private void PerformInteraction(MonoBehaviour target)
        {
            if (target is CustomerTable cTable)
            {
                if (cTable.currentCustomer != null && !cTable.currentCustomer.isOrderTaken)
                {
                    cTable.currentCustomer.TakeOrder();
                    return;
                }
                if (carriedItem != null && (carriedItem.category == ItemCategory.CookedDish || carriedItem.category == ItemCategory.FilledDrink))
                {
                    int coins, tip;
                    if (cTable.TryServe(carriedItem, out coins, out tip))
                    {
                        carriedItem = null;
                    }
                    else
                    {
                        SoundManager.Instance?.PlayBuzzer();
                    }
                    return;
                }
                if (carriedItem == null && cTable.HasDirtyPlate)
                {
                    var dp = cTable.TakeDirtyPlate();
                    if (dp != null) HoldItem(dp);
                    return;
                }
            }
            else if (target is FoodLickerCooker cooker)
            {
                if (cooker.state == FoodLickerCooker.CookerState.Ready && carriedItem == null)
                {
                    var dish = cooker.TakeCookedDish();
                    if (dish != null)
                    {
                        HoldItem(dish);
                        if (GameManager.Instance != null && GameManager.Instance.currentLevelIndex == 1)
                        {
                            UIManager.Instance?.SetTutorialHint("¡Plato preparado! Llévaselo al cliente en su mesa en el comedor.");
                        }
                    }
                    return;
                }
                if (carriedItem != null && cooker.TryAddIngredient(carriedItem))
                {
                    Destroy(carriedItem.gameObject);
                    carriedItem = null;

                    if (GameManager.Instance != null && GameManager.Instance.currentLevelIndex == 1)
                    {
                        if (cooker.CanManualStartCooking())
                        {
                            UIManager.Instance?.SetTutorialHint("¡Ingredientes listos en el FoodLicker! Pulsa [E] con las manos vacías para cocinar.");
                        }
                    }
                    return;
                }
                if (carriedItem == null && cooker.CanManualStartCooking())
                {
                    cooker.StartCookingRecipe();
                    return;
                }
            }
            else if (target is HeldItem groundItem && carriedItem == null)
            {
                HoldItem(groundItem);
                SoundManager.Instance?.PlayPickup();
                return;
            }
            else if (target is DrinkDispenser dispenser)
            {
                if (carriedItem != null && dispenser.CanFill(carriedItem))
                {
                    dispenser.StartFilling(carriedItem);
                    StartBusyAction(() => dispenser.CancelFilling(), (dt) =>
                    {
                        bool done;
                        dispenser.UpdateFilling(dt, out done);
                        return done;
                    });
                    return;
                }
            }
            else if (target is CupDispenser cDisp && carriedItem == null)
            {
                var cup = cDisp.SpawnCup();
                if (cup != null) HoldItem(cup.GetComponent<HeldItem>());
                return;
            }
            else if (target is SoulDispenser sDisp && carriedItem == null)
            {
                var soul = sDisp.SpawnSoul();
                if (soul != null)
                {
                    HoldItem(soul.GetComponent<HeldItem>());
                    if (GameManager.Instance != null && GameManager.Instance.currentLevelIndex == 1)
                    {
                        UIManager.Instance?.SetTutorialHint("¡Llevas el alma! Métela al FoodLicker (el caldero negro con fuego).");
                    }
                }
                return;
            }
            else if (target is CleanPlateStack pStack)
            {
                if (carriedItem == null)
                {
                    var plate = pStack.TakePlate();
                    if (plate != null)
                    {
                        HoldItem(plate.GetComponent<HeldItem>());
                        if (GameManager.Instance != null && GameManager.Instance.currentLevelIndex == 1)
                        {
                            UIManager.Instance?.SetTutorialHint("¡Llevas el plato! Mételo al FoodLicker o coge el alma indicada.");
                        }
                    }
                    return;
                }
                if (carriedItem.category == ItemCategory.CleanPlate && pStack.ReturnCleanPlate())
                {
                    Destroy(carriedItem.gameObject);
                    carriedItem = null;
                    return;
                }
            }
            else if (target is SinkStation sink)
            {
                if (carriedItem != null && carriedItem.category == ItemCategory.DirtyPlate)
                {
                    if (sink.TryPlaceDirtyPlate(carriedItem))
                    {
                        Destroy(carriedItem.gameObject);
                        carriedItem = null;
                        if (GameManager.Instance != null && GameManager.Instance.currentLevelIndex == 1)
                        {
                            UIManager.Instance?.SetTutorialHint("¡Plato en el fregadero! Pulsa [E] para fregarlo.");
                        }
                    }
                    return;
                }
                if (sink.hasDirtyPlate && !sink.isWashing)
                {
                    sink.StartWashing();
                    StartBusyAction(() => sink.CancelWashing(), (dt) =>
                    {
                        GameObject cleanPlate;
                        bool done = sink.UpdateWashing(dt, out cleanPlate);
                        if (done && cleanPlate != null)
                        {
                            HoldItem(cleanPlate.GetComponent<HeldItem>());
                        }
                        return done;
                    });
                    return;
                }
            }
            else if (target is TrashBin bin && carriedItem != null)
            {
                bin.ThrowAway(carriedItem);
                carriedItem = null;
                return;
            }
            else if (target is CounterTable table)
            {
                if (carriedItem != null && table.CanPlaceItem())
                {
                    table.PlaceItem(carriedItem);
                    carriedItem = null;
                    return;
                }
                if (carriedItem == null && table.HasItem())
                {
                    var item = table.TakeItem();
                    if (item != null) HoldItem(item);
                    return;
                }
            }
            else if (target is ConductorNPC conductor)
            {
                conductor.TalkToConductor();
                return;
            }
            else if (target is ManagementBoard board)
            {
                board.OpenBoard();
                return;
            }
        }

        private void HoldItem(HeldItem item)
        {
            carriedItem = item;
            item.transform.SetParent(handSlot, false);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
        }

        public void DropItem()
        {
            if (carriedItem == null) return;
            carriedItem.transform.SetParent(null);
            carriedItem.transform.position = transform.position + transform.forward * 0.8f;
            carriedItem = null;
            SoundManager.Instance?.PlayDrop();
        }

        public void Stun(float duration)
        {
            isStunned = true;
            stunTimer = duration;
            CancelBusyAction();
            if (stunStarsVisual != null) stunStarsVisual.SetActive(true);
        }

        private void StartBusyAction(System.Action onCancel, System.Func<float, bool> tickFunc)
        {
            isBusyWithAction = true;
            busyCancelAction = onCancel;
            StartCoroutine(BusyActionRoutine(tickFunc));
        }

        private System.Collections.IEnumerator BusyActionRoutine(System.Func<float, bool> tickFunc)
        {
            while (isBusyWithAction)
            {
                bool finished = tickFunc(Time.deltaTime);
                if (finished)
                {
                    isBusyWithAction = false;
                    busyCancelAction = null;
                    yield break;
                }
                yield return null;
            }
        }

        private void CancelBusyAction()
        {
            if (isBusyWithAction)
            {
                isBusyWithAction = false;
                busyCancelAction?.Invoke();
                busyCancelAction = null;
            }
        }
    }
}
