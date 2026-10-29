using UnityEngine;

namespace SoulsALaCarte.Environment
{
    public class SoulShelfDoorway : MonoBehaviour
    {
        public static SoulShelfDoorway Instance { get; private set; }

        [Header("Elementos de la Estantería y Puerta")]
        public GameObject soulShelfRoot;
        public GameObject doorwayBlocker;
        public GameObject lockedSign;

        private void Awake()
        {
            Instance = this;
        }

        public void SetDoorwayState(bool isLevelActive)
        {
            // Durante el nivel activo:
            // - La estantería de almas está visible para poder coger almas.
            // - El paso a la cabina del maquinista está bloqueado.
            // - El cartel "Maquinista cerrado en nivel activo" está visible.
            // Entre niveles (fuera de ronda):
            // - La estantería desaparece / se retrae.
            // - El paso queda 100% libre para entrar a la cabina y hablar con Migafees.
            // - El cartel desaparece.
            if (soulShelfRoot != null)
            {
                soulShelfRoot.SetActive(isLevelActive);
            }

            if (doorwayBlocker != null)
            {
                doorwayBlocker.SetActive(isLevelActive);
            }

            if (lockedSign != null)
            {
                lockedSign.SetActive(isLevelActive);
            }
        }
    }
}
