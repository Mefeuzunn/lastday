using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.UI;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;

namespace ProjectSurvival.AI
{
    public class NPCSocialEcosystem : MonoBehaviour
    {
        public static NPCSocialEcosystem Instance { get; private set; }

        [Header("Social Chatter Parameters")]
        [SerializeField] private float chatterDistanceThreshold = 6.5f;
        [SerializeField] private float chatterCooldownPerPair = 25f;
        [SerializeField] private float proximityCheckInterval = 0.35f;

        [Header("Tactical Coordination Parameters")]
        [SerializeField] private float tacticalCalloutRadius = 18f;
        [SerializeField] private float tacticalCalloutCooldown = 8f;
        [SerializeField] private float medicAssistRadius = 12f;
        [SerializeField] private float medicAssistCooldown = 15f;
        [SerializeField] private float ammoShareRadius = 8f;
        [SerializeField] private float ammoShareCooldown = 20f;

        private float nextProximityCheckTime = 0f;
        private float lastSquadCalloutTime = -99f;
        private float lastMedicAssistTime = -99f;
        private float lastAmmoShareTime = -99f;

        private readonly Dictionary<string, float> pairChatterCooldowns = new Dictionary<string, float>();
        private readonly Dictionary<SurvivorNPC_AI, WorldSpeechBubbleUI> bubblePool = new Dictionary<SurvivorNPC_AI, WorldSpeechBubbleUI>();

        // Turkish Survival Dialogue Lines
        private static readonly string[] DayChatterLines = new string[]
        {
            "Telsizden ses var mı? Merkezden hala haber yok.",
            "Sığınağın batı kapısındaki barikat gevşemiş, Zeynep bakmalı.",
            "Gece yaklaşıyor, fenerlerin pillerini kontrol ettin mi?",
            "Dr. Aras dün gece iki yaralı getirdi, ilaç stoklarımız tükeniyor.",
            "Kerem ile devriyeye çıktığımızda kuzey caddede koşanları gördük.",
            "Erzak deposundaki konserveler bu haftayı anca çıkarır.",
            "Dün gece bir patlama sesi duydum, baraj tarafında bir şeyler oluyor.",
            "Filtreler tıkanmış, temiz su bulmak gittikçe zorlaşıyor."
        };

        private static readonly string[] NightChatterLines = new string[]
        {
            "Gece devriyesi başladı, projektörleri açık tutun.",
            "Dışarıdaki çığlık sesleri gittikçe yaklaşıyor...",
            "Karanlıkta gözlerinin parladığını görebiliyorum...",
            "Barikatların yanından ayrılmayın, gece avlanıyorlar."
        };

        private static readonly string[] CombatCalloutLines = new string[]
        {
            "Ateş altında kaldım, destek verin!",
            "Koşucu yaklaşıyor, sol kanada dikkat!",
            "Çığlıkçı var! Horde çekmeden indirin!",
            "Tank var, zayıf noktasına vurun ve dağılın!",
            "Siper alın, etrafımızı sarıyorlar!"
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (Time.time >= nextProximityCheckTime)
            {
                nextProximityCheckTime = Time.time + proximityCheckInterval;
                UpdateSocialProximitySlices();
                UpdateMedicAndAmmoSharingSlices();
            }
        }

        private void UpdateSocialProximitySlices()
        {
            var survivors = SurvivorNPC_AI.ActiveSurvivors;
            if (survivors == null || survivors.Count < 2) return;

            float now = Time.time;
            bool isNight = DayNightManager.Instance != null && DayNightManager.Instance.IsNight;

            for (int i = 0; i < survivors.Count; i++)
            {
                SurvivorNPC_AI a = survivors[i];
                if (a == null || a.IsDead || a.CurrentState == NPCState.Combat || a.CurrentState == NPCState.Dialogue) continue;

                for (int j = i + 1; j < survivors.Count; j++)
                {
                    SurvivorNPC_AI b = survivors[j];
                    if (b == null || b.IsDead || b.CurrentState == NPCState.Combat || b.CurrentState == NPCState.Dialogue) continue;

                    float dist = Vector3.Distance(a.transform.position, b.transform.position);
                    if (dist <= chatterDistanceThreshold)
                    {
                        string pairKey = GetPairKey(a.NpcId, b.NpcId);
                        if (!pairChatterCooldowns.TryGetValue(pairKey, out float cooldownUntil) || now >= cooldownUntil)
                        {
                            pairChatterCooldowns[pairKey] = now + chatterCooldownPerPair;
                            TriggerDiegeticChatter(a, b, isNight);
                            return; // Only trigger one chatter per interval to keep CPU minimal
                        }
                    }
                }
            }
        }

        private string GetPairKey(string idA, string idB)
        {
            return string.Compare(idA, idB, StringComparison.Ordinal) < 0
                ? $"{idA}_{idB}"
                : $"{idB}_{idA}";
        }

        private void TriggerDiegeticChatter(SurvivorNPC_AI speaker, SurvivorNPC_AI listener, bool isNight)
        {
            string[] pool = isNight ? NightChatterLines : DayChatterLines;
            string line = pool[UnityEngine.Random.Range(0, pool.Length)];

            // Gentle mutual orientation
            Vector3 toListener = (listener.transform.position - speaker.transform.position);
            toListener.y = 0f;
            if (toListener.sqrMagnitude > 0.01f)
            {
                speaker.transform.rotation = Quaternion.LookRotation(toListener);
            }

            ShowSpeechBubble(speaker, line, 3.2f);
            Debug.Log($"[NPCSocialEcosystem] {speaker.NpcDisplayName} -> {listener.NpcDisplayName}: \"{line}\"");
        }

        public void TriggerCombatCallout(SurvivorNPC_AI caller, ZombieAI targetZombie = null)
        {
            if (caller == null || caller.IsDead) return;
            if (Time.time < lastSquadCalloutTime + tacticalCalloutCooldown) return;
            lastSquadCalloutTime = Time.time;

            string line = CombatCalloutLines[UnityEngine.Random.Range(0, CombatCalloutLines.Length)];
            if (targetZombie != null)
            {
                if (targetZombie.Type == ZombieType.Screamer)
                    line = "Çığlıkçı tespit edildi! Ses bombası atmadan indirin!";
                else if (targetZombie.Type == ZombieType.Tank)
                    line = "Tank yaklaşıyor! Dağılın, arkasındaki zayıf noktaya odaklanın!";
                else if (targetZombie.Type == ZombieType.Runner)
                    line = "Koşucu hızla yaklaşıyor, kanatları koruyun!";
            }

            ShowSpeechBubble(caller, line, 2.8f);

            // Notify allied armed NPCs within 18m to provide crossfire
            var survivors = SurvivorNPC_AI.ActiveSurvivors;
            for (int i = 0; i < survivors.Count; i++)
            {
                SurvivorNPC_AI ally = survivors[i];
                if (ally == null || ally == caller || ally.IsDead) continue;
                if (ally.Archetype != NPCArchetype.Armed) continue;

                float dist = Vector3.Distance(caller.transform.position, ally.transform.position);
                if (dist <= tacticalCalloutRadius && (ally.CurrentState == NPCState.Idle || ally.CurrentState == NPCState.RoutineTask))
                {
                    ally.TransitionToState(NPCState.Combat);
                }
            }
        }

        private void UpdateMedicAndAmmoSharingSlices()
        {
            var survivors = SurvivorNPC_AI.ActiveSurvivors;
            if (survivors == null || survivors.Count == 0) return;

            GameObject playerObj = GameObject.FindWithTag("Player");
            SurvivalStats playerStats = playerObj != null ? playerObj.GetComponent<SurvivalStats>() : null;

            // 1. Field Medic Evaluation
            if (Time.time >= lastMedicAssistTime + medicAssistCooldown)
            {
                for (int i = 0; i < survivors.Count; i++)
                {
                    SurvivorNPC_AI s = survivors[i];
                    if (s == null || s.IsDead || s.Archetype != NPCArchetype.Medic) continue;

                    // Check player first
                    if (playerStats != null && playerStats.CurrentHealth < playerStats.MaxHealth * 0.4f)
                    {
                        float dist = Vector3.Distance(s.transform.position, playerObj.transform.position);
                        if (dist <= medicAssistRadius)
                        {
                            lastMedicAssistTime = Time.time;
                            playerStats.Heal(25f);
                            ShowSpeechBubble(s, "Yaranı sarıyorum, dayan!", 2.5f);
                            ProceduralAudioManager.Instance?.PlayFirstAid();
                            GameEvents.TriggerObjectiveToastRequested($"[SAHRA SIHHİYESİ] {s.NpcDisplayName} sizi tedavi etti! (+25 Can)");
                            return;
                        }
                    }

                    // Check other wounded allies
                    for (int j = 0; j < survivors.Count; j++)
                    {
                        SurvivorNPC_AI wounded = survivors[j];
                        if (wounded == null || wounded == s || wounded.IsDead) continue;

                        if (wounded.CurrentHealth < wounded.MaxHealth * 0.4f)
                        {
                            float dist = Vector3.Distance(s.transform.position, wounded.transform.position);
                            if (dist <= medicAssistRadius)
                            {
                                lastMedicAssistTime = Time.time;
                                wounded.Heal(25f);
                                ShowSpeechBubble(s, $"{wounded.NpcDisplayName}, yaranı tedavi ettim!", 2.5f);
                                ProceduralAudioManager.Instance?.PlayFirstAid();
                                return;
                            }
                        }
                    }
                }
            }

            // 2. Ammo Sharing Evaluation
            if (Time.time >= lastAmmoShareTime + ammoShareCooldown && playerObj != null)
            {
                InventorySystem inv = InventorySystem.Instance;
                if (inv != null && !inv.HasItem("ammo_9mm", 5))
                {
                    for (int i = 0; i < survivors.Count; i++)
                    {
                        SurvivorNPC_AI armedNpc = survivors[i];
                        if (armedNpc == null || armedNpc.IsDead || armedNpc.Archetype != NPCArchetype.Armed) continue;

                        float dist = Vector3.Distance(armedNpc.transform.position, playerObj.transform.position);
                        if (dist <= ammoShareRadius)
                        {
                            lastAmmoShareTime = Time.time;
                            inv.AddItemById("ammo_9mm", 15);
                            ShowSpeechBubble(armedNpc, "Al şu şarjörü, mermisiz kalma!", 2.5f);
                            ProceduralAudioManager.Instance?.PlayItemPickup();
                            GameEvents.TriggerObjectiveToastRequested($"[MÜHİMMAT PAYLAŞIMI] {armedNpc.NpcDisplayName} sana 15 mermi verdi!");
                            return;
                        }
                    }
                }
            }
        }

        public void ShowSpeechBubble(SurvivorNPC_AI npc, string text, float duration = 3.2f)
        {
            if (npc == null || npc.gameObject == null) return;

            if (!bubblePool.TryGetValue(npc, out WorldSpeechBubbleUI bubble) || bubble == null)
            {
                GameObject bObj = new GameObject($"SpeechBubble_{npc.NpcId}");
                bObj.transform.position = npc.transform.position + Vector3.up * 2.15f;
                bubble = bObj.AddComponent<WorldSpeechBubbleUI>();
                bubble.BindTarget(npc.transform);
                bubblePool[npc] = bubble;
            }

            bubble.ShowSpeech(text, duration);
        }
    }
}
