using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Missions;

namespace ProjectSurvival.Survival
{
    [RequireComponent(typeof(Collider))]
    public class RadioStation : MonoBehaviour
    {
        [Header("Radio Logs")]
        [SerializeField] private List<string> radioMessages = new List<string>
        {
            "Cızırtı... 'Buradan tüm hayatta kalanlara sesleniyoruz. Şehir merkezindeki klinik karantinaya alındı...'",
            "'Dikkat! Gece hava karardığında zombiler sese 2 kat daha duyarlı hale geliyor. Işıklarınızı kapatın!'",
            "'Acil Çağrı: Doğu caddesinde sıkışmış siviller var. Sığınağa ulaşabilen herkes erzak toplasın!'"
        };

        private int currentMessageIndex = 0;
        private bool playerInRange = false;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnEnable()
        {
            GameEvents.OnInteractPressed += HandleInteract;
        }

        private void OnDisable()
        {
            GameEvents.OnInteractPressed -= HandleInteract;
            if (playerInRange)
            {
                GameEvents.TriggerInteractionPromptChanged(false, string.Empty);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = true;
                GameEvents.TriggerInteractionPromptChanged(true, "TELSİZİ DİNLE (Frekans Tara)");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = false;
                GameEvents.TriggerInteractionPromptChanged(false, string.Empty);
            }
        }

        private void HandleInteract()
        {
            if (!playerInRange) return;

            string msg = radioMessages[currentMessageIndex];
            currentMessageIndex = (currentMessageIndex + 1) % radioMessages.Count;

            GameEvents.TriggerNoteCollected($"Telsiz Yayını #{currentMessageIndex + 1}", msg);
            GameEvents.TriggerInteractionPromptChanged(true, $"Telsiz: \"{msg}\"");
            Debug.Log($"[RadioStation] {msg}");
        }
    }
}
