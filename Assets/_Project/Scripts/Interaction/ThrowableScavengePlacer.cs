using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Active Phase 4: Scatters 18+ pickable bricks (Tuğla) and distraction bottles
    /// along curbs, alleyways, collapsed ramps, and construction rubble across the city.
    /// Uses URP instanced materials and non-allocating triggers.
    /// </summary>
    public static class ThrowableScavengePlacer
    {
        private static bool hasSpawned = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureThrowablePropsPlaced()
        {
            if (hasSpawned) return;
            hasSpawned = true;

            GameObject parentRoot = GameObject.Find("--- THROWABLE_SCAVENGE_PROPS ---");
            if (parentRoot == null)
            {
                parentRoot = new GameObject("--- THROWABLE_SCAVENGE_PROPS ---");
            }

            ItemData brickItem = Resources.Load<ItemData>("Items/item_brick");
            ItemData bottleItem = Resources.Load<ItemData>("Items/item_distraction_bottle");

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material brickMat = new Material(litShader);
            brickMat.name = "Mat_Brick_Pickup_Instanced";
            brickMat.color = new Color(0.72f, 0.28f, 0.18f); // Red terracotta
            brickMat.enableInstancing = true;

            Material bottleMat = new Material(litShader);
            bottleMat.name = "Mat_Bottle_Pickup_Instanced";
            bottleMat.color = new Color(0.22f, 0.65f, 0.38f, 0.85f); // Green glass
            bottleMat.enableInstancing = true;

            // 10 Construction Bricks (Tuğla) along curbs & rubble
            Vector3[] brickPositions = new Vector3[]
            {
                new Vector3(4.5f, 0.12f, -12.5f),   // SafeHouse courtyard curb
                new Vector3(-8.2f, 0.12f, -14.0f),  // SafeHouse alleyway
                new Vector3(18.5f, 0.12f, -6.5f),   // Near SafeHouse pickup truck
                new Vector3(12.0f, 0.12f, 15.0f),   // Main quarantine boulevard curb
                new Vector3(-15.5f, 0.12f, 22.0f),  // Collapsed brick wall corner
                new Vector3(28.0f, 0.12f, -35.0f),  // South alley rubble
                new Vector3(-32.0f, 0.12f, -18.0f), // West warehouse curb
                new Vector3(6.5f, 0.12f, 48.0f),    // Construction barrier rubble
                new Vector3(-45.0f, 0.12f, 5.0f),   // Clinic alleyway curb
                new Vector3(35.0f, 0.12f, 25.0f)    // East street curb
            };

            for (int i = 0; i < brickPositions.Length; i++)
            {
                string propName = $"Prop_Brick_Curb_{i + 1}";
                if (parentRoot.transform.Find(propName) != null) continue;

                GameObject brickObj = new GameObject(propName);
                brickObj.transform.SetParent(parentRoot.transform);
                brickObj.transform.position = brickPositions[i];
                brickObj.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vis.name = "Visual_Model";
                vis.transform.SetParent(brickObj.transform, false);
                vis.transform.localScale = new Vector3(0.28f, 0.14f, 0.14f);
                Collider c = vis.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.Destroy(c);
                vis.GetComponent<Renderer>().sharedMaterial = brickMat;

                SphereCollider trigger = brickObj.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 1.35f;

                LootContainer lc = brickObj.AddComponent<LootContainer>();
                if (brickItem != null) lc.AddItem(brickItem, 2, 1.0f);
            }

            // 8 Glass Bottles (Dikkat Dağıtıcı Şişe)
            Vector3[] bottlePositions = new Vector3[]
            {
                new Vector3(2.2f, 0.12f, -15.5f),   // Courtyard gate curb
                new Vector3(-5.5f, 0.12f, -8.0f),   // SafeHouse porch steps
                new Vector3(15.0f, 0.12f, -18.0f),  // Street trash can side
                new Vector3(-22.0f, 0.12f, 10.0f),  // Sidewalk dumpster side
                new Vector3(22.0f, 0.12f, 8.0f),    // Bus stop curb
                new Vector3(-12.0f, 0.12f, 38.0f),  // North avenue corner
                new Vector3(40.0f, 0.12f, -10.0f),  // East shop alley
                new Vector3(-38.0f, 0.12f, -40.0f)  // Metro station entrance curb
            };

            for (int i = 0; i < bottlePositions.Length; i++)
            {
                string propName = $"Prop_Bottle_Curb_{i + 1}";
                if (parentRoot.transform.Find(propName) != null) continue;

                GameObject bottleObj = new GameObject(propName);
                bottleObj.transform.SetParent(parentRoot.transform);
                bottleObj.transform.position = bottlePositions[i];
                bottleObj.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                vis.name = "Visual_Model";
                vis.transform.SetParent(bottleObj.transform, false);
                vis.transform.localScale = new Vector3(0.12f, 0.28f, 0.12f);
                Collider c = vis.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.Destroy(c);
                vis.GetComponent<Renderer>().sharedMaterial = bottleMat;

                SphereCollider trigger = bottleObj.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 1.35f;

                LootContainer lc = bottleObj.AddComponent<LootContainer>();
                if (bottleItem != null) lc.AddItem(bottleItem, 1, 1.0f);
            }

            Debug.Log("[ThrowableScavengePlacer] Placed 18 pickable bricks and glass bottles across city curbs and alleys.");
        }
    }
}
