using UnityEngine;

namespace Agentes.Bases.Cerebro
{
    public class AgentBlackboard : MonoBehaviour
    {
        [Header("Percepción del Ladrón")]
        public bool veAlLadron; // Datos del SensorVision[cite: 2, 6]
        public Vector3? ultimaPosicionLadron; // Último rastro detectado[cite: 6, 7]
        public bool ladronTieneFuego; // ¿Lleva la antorcha encendida?[cite: 2, 6]
        public bool ladronPerdidoConFuego; // Si se escapó mientras brillaba[cite: 6, 7]
        
        [Header("Historial de Percepción")]
        public bool ladronVisibleFrameAnterior; // Para detectar transiciones[cite: 6, 7]
        public bool ladronVisibleFrameAnteriorTeniaFuego; // Historial táctico[cite: 6, 7]
        public bool acabaDeVerAlLadron; // Trigger de "¡Ahí está!"
        public bool acabaDePerderAlLadron; // Trigger de "Se me ha escapado"

        [Header("Percepción de Entorno")]
        public bool oyoAlgo; // Datos del SensorOido[cite: 1, 6]
        public Vector3? posicionRuido; // Punto de origen del sonido[cite: 6, 7]
        public Vector3? posicionPuerta; // Puerta física detectada por SensorObjetos[cite: 4, 6]
        public bool hogueraDetectada; // ¿La hoguera está visible en este frame?[cite: 3, 6]
        public bool hogueraEnCampoDeVision; // ¿Estamos mirando hacia ella?[cite: 3, 6]
        public Vector3? posicionHogueraConocida; // Ubicación en el mapa[cite: 3, 6]

        [Header("Estados Lógicos y de Alerta")]
        public bool enAlerta; // Estado de sospecha activa[cite: 6, 7]
        public bool alarmaHogueraActiva; // Estado global de robo confirmado[cite: 6, 7]
        public bool busquedaAgotada; // Indica si el tiempo de patrulla local terminó[cite: 6, 7]
        public bool busquedaEsPorFuego; // ¿Buscamos porque la hoguera se apagó?[cite: 6, 7]
        public bool busquedaForzada; // Anti-bucle: ir a hoguera tras N fallos[cite: 6, 7]

        [Header("Contadores de Comportamiento")]
        public int framesSinVerHoguera = 0; // Filtro anti-oclusión[cite: 6, 7]
        public int ciclosBusquedaCompletados = 0; // Para la búsqueda rotatoria (cada 3)[cite: 6, 7]
        public int contadorResetsCiclo = 0; // Para el anti-bucle de interrupciones[cite: 6, 7]

        /// <summary>
        /// Limpia datos específicos del ladrón si se pierde el rastro o el TTL expira.
        /// </summary>
        public void ResetearDatosLadron()
        {
            veAlLadron = false;
            ultimaPosicionLadron = null;
            ladronTieneFuego = false;
            ladronPerdidoConFuego = false;
            enAlerta = false;
        }
    }
}