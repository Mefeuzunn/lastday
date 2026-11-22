using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Environment;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorTools
{
    public static class NPCCommunityEcosystemSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("ProjectSurvival/Setup Stage 3 NPC Community & Barter Ecosystem", false, 35)]
        public static void SetupEcosystemMenuItem()
        {
            SetupEcosystem(true);
        }

        public static void SetupEcosystem(bool markDirty = true)
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> RUNNING STAGE 3 LIVING NPC ECOSYSTEM & BARTER SETUP <<<");
            Debug.Log("==================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Setup FAIL] Could not open scene: {ScenePath}");
                return;
            }

            // 1. Setup NPC Ecosystem Manager
            GameObject mgrObj = GameObject.Find("NPC_Ecosystem_Manager");
            if (mgrObj == null)
            {
                mgrObj = new GameObject("NPC_Ecosystem_Manager");
                Undo.RegisterCreatedObjectUndo(mgrObj, "Create NPC_Ecosystem_Manager");
            }

            if (mgrObj.GetComponent<NPCDailyScheduleManager>() == null)
                mgrObj.AddComponent<NPCDailyScheduleManager>();

            if (mgrObj.GetComponent<NPCSocialEcosystem>() == null)
                mgrObj.AddComponent<NPCSocialEcosystem>();

            if (mgrObj.GetComponent<CommunityOutpostManager>() == null)
                mgrObj.AddComponent<CommunityOutpostManager>();

            if (mgrObj.GetComponent<BarterSystem>() == null)
                mgrObj.AddComponent<BarterSystem>();

            Debug.Log("[Setup] NPC_Ecosystem_Manager and core managers configured.");

            // 2. Setup Task Anchors Container
            GameObject anchorRoot = GameObject.Find("--- TASK ANCHORS ---");
            if (anchorRoot == null)
            {
                anchorRoot = new GameObject("--- TASK ANCHORS ---");
                Undo.RegisterCreatedObjectUndo(anchorRoot, "Create Task Anchors Root");
            }

            // Safe House Anchors
            CreateOrUpdateAnchor(anchorRoot.transform, "Anchor_SafeHouse_PerimeterGuard_N", new Vector3(2f, 0f, -8f),
                NPCTaskType.GuardPost, "Sığınak Kuzey Nöbeti", "kerem", 12f, Vector3.forward);

            CreateOrUpdateAnchor(anchorRoot.transform, "Anchor_SafeHouse_PerimeterGuard_W", new Vector3(-10f, 0f, -15f),
                NPCTaskType.GuardPost, "Sığınak Batı Barikat Nöbeti", "kerem", 14f, Vector3.left);

            CreateOrUpdateAnchor(anchorRoot.transform, "Anchor_SafeHouse_Workshop", new Vector3(-4f, 0f, -18f),
                NPCTaskType.WorkshopRepair, "Sığınak Jeneratör & Tamir Masası", "zeynep", 15f, Vector3.back);

            CreateOrUpdateAnchor(anchorRoot.transform, "Anchor_SafeHouse_Campfire", new Vector3(0f, 0f, -22f),
                NPCTaskType.RestAndSocial, "Sığınak Ateş Başı Dinlenme", "any", 16f, Vector3.forward);

            CreateOrUpdateAnchor(anchorRoot.transform, "Anchor_SafeHouse_BunkCot", new Vector3(-8f, 0f, -24f),
                NPCTaskType.RestAndSocial, "Sığınak Yatakhane Ranzası", "any", 20f, Vector3.right);

            CreateOrUpdateAnchor(anchorRoot.transform, "Anchor_SafeHouse_IntelTable", new Vector3(6f, 0f, -20f),
                NPCTaskType.ScavengeRoute, "Sığınak İstihbarat & Harita Masası", "elif", 12f, Vector3.left);

            // Clinic Anchors
            CreateOrUpdateAnchor(anchorRoot.transform, "Anchor_Clinic_MedicalStation", new Vector3(-36f, 0f, 32f),
                NPCTaskType.MedicalDuty, "Klinik Eczane & Triyaj Masası", "aras", 15f, Vector3.forward);

            CreateOrUpdateAnchor(anchorRoot.transform, "Anchor_Clinic_RestBed", new Vector3(-38f, 0f, 30f),
                NPCTaskType.RestAndSocial, "Klinik Hasta Yatağı", "aras", 18f, Vector3.right);

            // Scavenge Route Anchors linked to physical props
            WorldInteractableObject[] allInteractables = UnityEngine.Object.FindObjectsByType<WorldInteractableObject>(FindObjectsSortMode.None);
            int scavengeCount = 0;
            for (int i = 0; i < allInteractables.Length && scavengeCount < 6; i++)
            {
                WorldInteractableObject prop = allInteractables[i];
                if (prop == null) continue;

                Vector3 pos = prop.transform.position;
                if (Vector3.Distance(pos, Vector3.zero) > 15f) // Outside immediate safehouse center
                {
                    NPCTaskAnchor a = CreateOrUpdateAnchor(anchorRoot.transform, $"Anchor_Scavenge_{prop.gameObject.name}_{scavengeCount}",
                        pos + prop.transform.forward * 1.5f, NPCTaskType.ScavengeRoute, $"Keşif Noktası ({prop.ObjectTitle})", "any", 8f, -prop.transform.forward);
                    a.SetTargetInteractable(prop);
                    scavengeCount++;
                }
            }

            // Interactive Door Repair Anchors
            InteractiveDoor[] doors = UnityEngine.Object.FindObjectsByType<InteractiveDoor>(FindObjectsSortMode.None);
            for (int i = 0; i < doors.Length && i < 2; i++)
            {
                InteractiveDoor door = doors[i];
                if (door == null) continue;

                NPCTaskAnchor a = CreateOrUpdateAnchor(anchorRoot.transform, $"Anchor_DoorRepair_{i}",
                    door.transform.position + door.transform.forward * 1.2f, NPCTaskType.WorkshopRepair, "Kapı Tahkimatı & Onarım", "zeynep", 10f, -door.transform.forward);
                a.SetTargetDoor(door);
            }

            Debug.Log($"[Setup] Configured task anchors. Total anchors: {anchorRoot.transform.childCount}");

            // 3. Setup Survivor Barter UI on Canvas
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                SetupBarterUI(canvas.transform);
            }

            // 4. Setup Roaming Scavenger Squads
            SetupScavengerSquads();

            // 5. Mark Scene Dirty and Save
            if (markDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[Setup SUCCESS] Stage 3 Living NPC Ecosystem & Barter saved to Main_Prototype.unity!");
            }
        }

        private static NPCTaskAnchor CreateOrUpdateAnchor(Transform parent, string name, Vector3 pos, NPCTaskType type, string title, string npcId, float duration, Vector3 facing)
        {
            Transform existing = parent.Find(name);
            GameObject obj = existing != null ? existing.gameObject : new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = pos;

            NPCTaskAnchor anchor = obj.GetComponent<NPCTaskAnchor>();
            if (anchor == null) anchor = obj.AddComponent<NPCTaskAnchor>();

            anchor.Configure(type, name, title, npcId, duration);
            return anchor;
        }

        private static void SetupBarterUI(Transform canvasTransform)
        {
            Transform existing = canvasTransform.Find("SurvivorBarter_UI");
            GameObject rootObj;

            if (existing != null)
            {
                rootObj = existing.gameObject;
            }
            else
            {
                rootObj = new GameObject("SurvivorBarter_UI", typeof(RectTransform));
                rootObj.transform.SetParent(canvasTransform, false);
            }

            RectTransform rootRect = rootObj.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Background shade
            Image rootBg = rootObj.GetComponent<Image>() ?? rootObj.AddComponent<Image>();
            rootBg.color = new Color(0.04f, 0.06f, 0.09f, 0.94f);
            rootBg.raycastTarget = true;

            // Content Container
            Transform content = rootObj.transform.Find("Barter_Content");
            GameObject contentObj = content != null ? content.gameObject : new GameObject("Barter_Content", typeof(RectTransform));
            contentObj.transform.SetParent(rootObj.transform, false);
            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.05f, 0.05f);
            contentRect.anchorMax = new Vector2(0.95f, 0.95f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            // Header: Title & Trust Badge
            Transform header = contentObj.transform.Find("Header");
            GameObject headerObj = header != null ? header.gameObject : new GameObject("Header", typeof(RectTransform));
            headerObj.transform.SetParent(contentObj.transform, false);
            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.9f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            Text titleTxt = headerObj.GetComponentInChildren<Text>();
            if (titleTxt == null)
            {
                GameObject tObj = new GameObject("Title_Text", typeof(RectTransform));
                tObj.transform.SetParent(headerObj.transform, false);
                RectTransform tRect = tObj.GetComponent<RectTransform>();
                tRect.anchorMin = Vector2.zero;
                tRect.anchorMax = new Vector2(0.6f, 1f);
                tRect.offsetMin = Vector2.zero;
                tRect.offsetMax = Vector2.zero;
                titleTxt = tObj.AddComponent<Text>();
                titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
                titleTxt.fontSize = 20;
                titleTxt.alignment = TextAnchor.MiddleLeft;
                titleTxt.color = new Color(0.95f, 0.95f, 0.96f);
                titleTxt.text = "[TAKAS] Sığınak Ticareti";
                titleTxt.raycastTarget = false;
            }

            Transform trustObj = headerObj.transform.Find("Trust_Badge");
            Text trustTxt = trustObj != null ? trustObj.GetComponent<Text>() : null;
            if (trustTxt == null)
            {
                GameObject trObj = new GameObject("Trust_Badge", typeof(RectTransform));
                trObj.transform.SetParent(headerObj.transform, false);
                RectTransform trRect = trObj.GetComponent<RectTransform>();
                trRect.anchorMin = new Vector2(0.6f, 0f);
                trRect.anchorMax = Vector2.one;
                trRect.offsetMin = Vector2.zero;
                trRect.offsetMax = Vector2.zero;
                trustTxt = trObj.AddComponent<Text>();
                trustTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
                trustTxt.fontSize = 15;
                trustTxt.alignment = TextAnchor.MiddleRight;
                trustTxt.color = new Color(0.95f, 0.77f, 0.25f);
                trustTxt.text = "GÜVEN: %60  •  İNDİRİM: %18";
                trustTxt.raycastTarget = false;
            }

            // Left Pane (Player Offers)
            Transform leftPane = contentObj.transform.Find("LeftPane_Player");
            GameObject leftObj = leftPane != null ? leftPane.gameObject : new GameObject("LeftPane_Player", typeof(RectTransform));
            leftObj.transform.SetParent(contentObj.transform, false);
            RectTransform leftRect = leftObj.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0f, 0.15f);
            leftRect.anchorMax = new Vector2(0.38f, 0.88f);
            leftRect.offsetMin = Vector2.zero;
            leftRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup leftLayout = leftObj.GetComponent<VerticalLayoutGroup>() ?? leftObj.AddComponent<VerticalLayoutGroup>();
            leftLayout.spacing = 6f;
            leftLayout.childControlHeight = false;
            leftLayout.childControlWidth = true;

            // Right Pane (Merchant Stock)
            Transform rightPane = contentObj.transform.Find("RightPane_Merchant");
            GameObject rightObj = rightPane != null ? rightPane.gameObject : new GameObject("RightPane_Merchant", typeof(RectTransform));
            rightObj.transform.SetParent(contentObj.transform, false);
            RectTransform rightRect = rightObj.GetComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.62f, 0.15f);
            rightRect.anchorMax = new Vector2(1f, 0.88f);
            rightRect.offsetMin = Vector2.zero;
            rightRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup rightLayout = rightObj.GetComponent<VerticalLayoutGroup>() ?? rightObj.AddComponent<VerticalLayoutGroup>();
            rightLayout.spacing = 6f;
            rightLayout.childControlHeight = false;
            rightLayout.childControlWidth = true;

            // Center Action Pane
            Transform centerPane = contentObj.transform.Find("CenterPane_Controls");
            GameObject centerObj = centerPane != null ? centerPane.gameObject : new GameObject("CenterPane_Controls", typeof(RectTransform));
            centerObj.transform.SetParent(contentObj.transform, false);
            RectTransform centerRect = centerObj.GetComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.40f, 0.15f);
            centerRect.anchorMax = new Vector2(0.60f, 0.88f);
            centerRect.offsetMin = Vector2.zero;
            centerRect.offsetMax = Vector2.zero;

            // Center Status Text
            Transform statusObj = centerObj.transform.Find("Balance_Status");
            Text statusTxt = statusObj != null ? statusObj.GetComponent<Text>() : null;
            if (statusTxt == null)
            {
                GameObject stObj = new GameObject("Balance_Status", typeof(RectTransform));
                stObj.transform.SetParent(centerObj.transform, false);
                RectTransform stRect = stObj.GetComponent<RectTransform>();
                stRect.anchorMin = new Vector2(0f, 0.6f);
                stRect.anchorMax = new Vector2(1f, 0.85f);
                stRect.offsetMin = Vector2.zero;
                stRect.offsetMax = Vector2.zero;
                statusTxt = stObj.AddComponent<Text>();
                statusTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
                statusTxt.fontSize = 14;
                statusTxt.alignment = TextAnchor.MiddleCenter;
                statusTxt.color = Color.white;
                statusTxt.text = "Eşyaları seçiniz";
                statusTxt.raycastTarget = false;
            }

            // Buttons: Confirm & Close
            Transform confirmBtnObj = centerObj.transform.Find("Btn_ConfirmTrade");
            Button confirmBtn = confirmBtnObj != null ? confirmBtnObj.GetComponent<Button>() : null;
            if (confirmBtn == null)
            {
                GameObject cbObj = new GameObject("Btn_ConfirmTrade", typeof(RectTransform));
                cbObj.transform.SetParent(centerObj.transform, false);
                RectTransform cbRect = cbObj.GetComponent<RectTransform>();
                cbRect.anchorMin = new Vector2(0.05f, 0.35f);
                cbRect.anchorMax = new Vector2(0.95f, 0.55f);
                cbRect.offsetMin = Vector2.zero;
                cbRect.offsetMax = Vector2.zero;
                Image cbImg = cbObj.AddComponent<Image>();
                cbImg.color = new Color(0.18f, 0.55f, 0.34f, 0.95f);
                confirmBtn = cbObj.AddComponent<Button>();
                confirmBtn.targetGraphic = cbImg;

                GameObject cbt = new GameObject("Text", typeof(RectTransform));
                cbt.transform.SetParent(cbObj.transform, false);
                RectTransform cbtr = cbt.GetComponent<RectTransform>();
                cbtr.anchorMin = Vector2.zero;
                cbtr.anchorMax = Vector2.one;
                cbtr.offsetMin = Vector2.zero;
                cbtr.offsetMax = Vector2.zero;
                Text cbTxt = cbt.AddComponent<Text>();
                cbTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
                cbTxt.fontSize = 15;
                cbTxt.alignment = TextAnchor.MiddleCenter;
                cbTxt.color = Color.white;
                cbTxt.text = "TAKASI ONAYLA";
                cbTxt.raycastTarget = false;
            }

            Transform closeBtnObj = centerObj.transform.Find("Btn_CloseBarter");
            Button closeBtn = closeBtnObj != null ? closeBtnObj.GetComponent<Button>() : null;
            if (closeBtn == null)
            {
                GameObject clObj = new GameObject("Btn_CloseBarter", typeof(RectTransform));
                clObj.transform.SetParent(centerObj.transform, false);
                RectTransform clRect = clObj.GetComponent<RectTransform>();
                clRect.anchorMin = new Vector2(0.05f, 0.12f);
                clRect.anchorMax = new Vector2(0.95f, 0.28f);
                clRect.offsetMin = Vector2.zero;
                clRect.offsetMax = Vector2.zero;
                Image clImg = clObj.AddComponent<Image>();
                clImg.color = new Color(0.35f, 0.15f, 0.18f, 0.95f);
                closeBtn = clObj.AddComponent<Button>();
                closeBtn.targetGraphic = clImg;

                GameObject clt = new GameObject("Text", typeof(RectTransform));
                clt.transform.SetParent(clObj.transform, false);
                RectTransform cltr = clt.GetComponent<RectTransform>();
                cltr.anchorMin = Vector2.zero;
                cltr.anchorMax = Vector2.one;
                cltr.offsetMin = Vector2.zero;
                cltr.offsetMax = Vector2.zero;
                Text clTxt = clt.AddComponent<Text>();
                clTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
                clTxt.fontSize = 14;
                clTxt.alignment = TextAnchor.MiddleCenter;
                clTxt.color = Color.white;
                clTxt.text = "KAPAT";
                clTxt.raycastTarget = false;
            }

            // Attach SurvivorBarterUI component and serialize fields
            SurvivorBarterUI barterUI = rootObj.GetComponent<SurvivorBarterUI>() ?? rootObj.AddComponent<SurvivorBarterUI>();
            SerializedObject so = new SerializedObject(barterUI);
            so.FindProperty("barterRootPanel").objectReferenceValue = rootObj;
            so.FindProperty("titleText").objectReferenceValue = titleTxt;
            so.FindProperty("trustBadgeText").objectReferenceValue = trustTxt;
            so.FindProperty("playerContainer").objectReferenceValue = leftObj.transform;
            so.FindProperty("merchantContainer").objectReferenceValue = rightObj.transform;
            so.FindProperty("balanceStatusText").objectReferenceValue = statusTxt;
            so.FindProperty("confirmTradeButton").objectReferenceValue = confirmBtn;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.ApplyModifiedProperties();

            rootObj.SetActive(false);
            Debug.Log("[Setup] SurvivorBarterUI constructed and bound to Canvas.");
        }

        private static void SetupScavengerSquads()
        {
            GameObject squadRoot = GameObject.Find("--- SCAVENGER SQUADS ---");
            if (squadRoot == null)
            {
                squadRoot = new GameObject("--- SCAVENGER SQUADS ---");
                Undo.RegisterCreatedObjectUndo(squadRoot, "Create Scavenger Squads Root");
            }

            // Squad North
            Transform squadNorth = squadRoot.transform.Find("Scavenger_Squad_North");
            if (squadNorth == null)
            {
                GameObject sqObj = new GameObject("Scavenger_Squad_North");
                sqObj.transform.SetParent(squadRoot.transform, false);
                sqObj.transform.position = new Vector3(25f, 0f, 40f);
                AutonomousScavengerSquad squad = sqObj.AddComponent<AutonomousScavengerSquad>();

                // Create pointman & carrier survivors
                SurvivorNPC_AI point = CreateScavengerSurvivor("Scavenger_Point_Murat", sqObj.transform.position, NPCArchetype.Armed, "murat", "Murat");
                SurvivorNPC_AI carrier = CreateScavengerSurvivor("Scavenger_Carrier_Can", sqObj.transform.position + Vector3.right * 1.5f, NPCArchetype.Civilian, "can", "Can");

                squad.ConfigureSquad(point, carrier);
                Debug.Log("[Setup] Created Scavenger_Squad_North with Murat & Can.");
            }

            // Squad East
            Transform squadEast = squadRoot.transform.Find("Scavenger_Squad_East");
            if (squadEast == null)
            {
                GameObject sqObj = new GameObject("Scavenger_Squad_East");
                sqObj.transform.SetParent(squadRoot.transform, false);
                sqObj.transform.position = new Vector3(45f, 0f, -30f);
                AutonomousScavengerSquad squad = sqObj.AddComponent<AutonomousScavengerSquad>();

                // Create pointman & carrier survivors
                SurvivorNPC_AI point = CreateScavengerSurvivor("Scavenger_Point_Burak", sqObj.transform.position, NPCArchetype.Armed, "burak", "Burak");
                SurvivorNPC_AI carrier = CreateScavengerSurvivor("Scavenger_Carrier_Deniz", sqObj.transform.position + Vector3.right * 1.5f, NPCArchetype.Civilian, "deniz", "Deniz");

                squad.ConfigureSquad(point, carrier);
                Debug.Log("[Setup] Created Scavenger_Squad_East with Burak & Deniz.");
            }
        }

        private static SurvivorNPC_AI CreateScavengerSurvivor(string rootName, Vector3 pos, NPCArchetype archetype, string npcId, string displayName)
        {
            GameObject root = new GameObject(rootName);
            root.transform.position = pos;
            root.tag = "NPC";

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.speed = 3.0f;
            agent.angularSpeed = 240f;
            agent.acceleration = 8f;
            agent.radius = 0.4f;
            agent.height = 1.8f;

            CapsuleCollider cc = root.AddComponent<CapsuleCollider>();
            cc.radius = 0.4f;
            cc.height = 1.8f;
            cc.center = new Vector3(0, 0.9f, 0);

            root.AddComponent<NPCSensor>();
            root.AddComponent<NPCProceduralMotion>();
            SurvivorNPC_AI ai = root.AddComponent<SurvivorNPC_AI>();

            SerializedObject soAi = new SerializedObject(ai);
            soAi.FindProperty("npcId").stringValue = npcId;
            soAi.FindProperty("npcDisplayName").stringValue = displayName;
            soAi.FindProperty("archetype").enumValueIndex = (int)archetype;
            soAi.ApplyModifiedProperties();

            ProceduralHumanoidBuilder.HumanoidConfig config = archetype == NPCArchetype.Armed
                ? ProceduralHumanoidBuilder.KeremConfig()
                : ProceduralHumanoidBuilder.ElifConfig();

            ProceduralHumanoidBuilder.CreateHumanoidModel(root.transform, "Visual_Model", config);

            return ai;
        }
    }
}
