using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ProjectSurvival.Player;

namespace ProjectSurvival.EditorTools
{
    public static class ProceduralHumanoidBuilder
    {
        public struct HumanoidConfig
        {
            public float height;
            public Color skinColor;
            public Color shirtColor;
            public Color pantsColor;
            public Color shoesColor;
            public Color hairColor;
            public bool hasHair;
            public bool hasHat;
            public Color hatColor;
            public bool hasGlasses;
            public bool hasCoat;
            public Color coatColor;
            public bool hasVest;
            public Color vestColor;
            public bool hasBackpack;
            public Color backpackColor;
            public bool hasCrossbag;
            public Color crossbagColor;
            public bool isZombie;
            public bool isHunched;
            public float hunchAngle;
            public bool hasArmorPlates;
            public bool hasGlowingVeins;
            public bool isTank;
            public bool isRunner;
            public bool hasStethoscope;
        }

        public static HumanoidConfig PlayerConfig()
        {
            return new HumanoidConfig
            {
                height = 1.85f,
                skinColor = new Color(0.92f, 0.78f, 0.69f),
                shirtColor = new Color(46f / 255f, 111f / 255f, 149f / 255f), // Blue tactical #2E6F95
                pantsColor = new Color(0.18f, 0.22f, 0.28f), // Charcoal cargo
                shoesColor = new Color(0.14f, 0.14f, 0.16f),
                hairColor = new Color(0.2f, 0.15f, 0.1f),
                hasHair = true,
                hasBackpack = true,
                backpackColor = new Color(0.22f, 0.32f, 0.24f), // Tactical olive
                hasVest = true,
                vestColor = new Color(0.16f, 0.18f, 0.22f)
            };
        }

        public static HumanoidConfig DrArasConfig()
        {
            return new HumanoidConfig
            {
                height = 1.82f,
                skinColor = new Color(0.90f, 0.76f, 0.67f),
                shirtColor = new Color(0.15f, 0.55f, 0.52f), // Teal shirt
                pantsColor = new Color(0.25f, 0.28f, 0.32f),
                shoesColor = new Color(0.15f, 0.15f, 0.16f),
                hairColor = new Color(0.35f, 0.35f, 0.38f),
                hasHair = true,
                hasGlasses = true,
                hasCoat = true,
                coatColor = new Color(0.95f, 0.96f, 0.98f), // Lab coat
                hasStethoscope = true
            };
        }

        public static HumanoidConfig ZeynepConfig()
        {
            return new HumanoidConfig
            {
                height = 1.68f,
                skinColor = new Color(0.94f, 0.80f, 0.72f),
                shirtColor = new Color(0.48f, 0.32f, 0.70f), // Purple jacket #7B52B3
                pantsColor = new Color(0.20f, 0.22f, 0.25f),
                shoesColor = new Color(0.28f, 0.20f, 0.16f),
                hairColor = new Color(0.12f, 0.10f, 0.08f),
                hasHair = true,
                hasVest = true,
                vestColor = new Color(0.35f, 0.25f, 0.50f)
            };
        }

        public static HumanoidConfig KeremConfig()
        {
            return new HumanoidConfig
            {
                height = 1.90f,
                skinColor = new Color(0.88f, 0.74f, 0.63f),
                shirtColor = new Color(0.30f, 0.35f, 0.40f),
                pantsColor = new Color(0.55f, 0.42f, 0.26f),
                shoesColor = new Color(0.22f, 0.16f, 0.10f),
                hairColor = new Color(0.25f, 0.20f, 0.15f),
                hasHat = true,
                hatColor = new Color(0.22f, 0.24f, 0.28f),
                hasVest = true,
                vestColor = new Color(0.25f, 0.28f, 0.30f)
            };
        }

        public static HumanoidConfig ElifConfig()
        {
            return new HumanoidConfig
            {
                height = 1.70f,
                skinColor = new Color(0.92f, 0.77f, 0.68f),
                shirtColor = new Color(0.82f, 0.76f, 0.45f), // Beige field jacket
                pantsColor = new Color(0.28f, 0.30f, 0.25f),
                shoesColor = new Color(0.20f, 0.15f, 0.12f),
                hairColor = new Color(0.18f, 0.12f, 0.08f),
                hasHair = true,
                hasCrossbag = true,
                crossbagColor = new Color(0.35f, 0.28f, 0.18f)
            };
        }

        public static HumanoidConfig ScavengerConfig()
        {
            return new HumanoidConfig
            {
                height = 1.78f,
                skinColor = new Color(0.86f, 0.72f, 0.62f),
                shirtColor = new Color(0.32f, 0.36f, 0.28f), // Worn olive drab
                pantsColor = new Color(0.20f, 0.22f, 0.24f), // Dark utility cargo
                shoesColor = new Color(0.14f, 0.12f, 0.10f),
                hairColor = new Color(0.18f, 0.15f, 0.12f),
                hasHair = true,
                hasVest = true,
                vestColor = new Color(0.24f, 0.26f, 0.22f), // Tactical vest
                hasBackpack = true,
                backpackColor = new Color(0.28f, 0.24f, 0.18f)
            };
        }

        public static HumanoidConfig ZombieStandardConfig()
        {
            return new HumanoidConfig
            {
                height = 1.80f,
                skinColor = new Color(0.48f, 0.58f, 0.46f), // Rotting pale green
                shirtColor = new Color(0.28f, 0.24f, 0.22f), // Tattered brown
                pantsColor = new Color(0.22f, 0.20f, 0.20f),
                shoesColor = new Color(0.14f, 0.12f, 0.12f),
                isZombie = true,
                isHunched = true,
                hunchAngle = 18f
            };
        }

        public static HumanoidConfig ZombieRunnerConfig()
        {
            return new HumanoidConfig
            {
                height = 1.76f,
                skinColor = new Color(0.52f, 0.48f, 0.42f), // Desiccated gray-tan
                shirtColor = new Color(0.32f, 0.18f, 0.18f), // Dried blood tint
                pantsColor = new Color(0.18f, 0.18f, 0.20f),
                shoesColor = new Color(0.12f, 0.10f, 0.10f),
                isZombie = true,
                isHunched = true,
                hunchAngle = 24f,
                isRunner = true
            };
        }

        public static HumanoidConfig ZombieScreamerConfig()
        {
            return new HumanoidConfig
            {
                height = 1.84f,
                skinColor = new Color(0.42f, 0.28f, 0.55f), // Sickly purple
                shirtColor = new Color(0.18f, 0.14f, 0.24f),
                pantsColor = new Color(0.15f, 0.12f, 0.18f),
                shoesColor = new Color(0.10f, 0.08f, 0.12f),
                isZombie = true,
                isHunched = true,
                hunchAngle = 12f,
                hasGlowingVeins = true
            };
        }

        public static HumanoidConfig ZombieTankConfig()
        {
            return new HumanoidConfig
            {
                height = 2.18f,
                skinColor = new Color(0.30f, 0.32f, 0.30f), // Ash gray scarred
                shirtColor = new Color(0.14f, 0.15f, 0.16f),
                pantsColor = new Color(0.12f, 0.12f, 0.14f),
                shoesColor = new Color(0.10f, 0.10f, 0.10f),
                isZombie = true,
                isTank = true,
                hasArmorPlates = true,
                isHunched = true,
                hunchAngle = 10f
            };
        }

        public static GameObject CreateHumanoidModel(Transform rootParent, string modelName, HumanoidConfig config)
        {
            GameObject visual = new GameObject(modelName);
            visual.transform.SetParent(rootParent);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            float hScale = config.height / 1.85f;
            float widthMult = config.isTank ? 1.45f : (config.isRunner ? 0.88f : 1.0f);

            // Container pivot for posture
            GameObject bodyHips = new GameObject("Body_Hips");
            bodyHips.transform.SetParent(visual.transform);
            bodyHips.transform.localPosition = new Vector3(0f, 0.88f * hScale, 0f);

            if (config.isHunched)
            {
                bodyHips.transform.localRotation = Quaternion.Euler(config.hunchAngle, 0f, 0f);
            }

            // Material Caches (strictly URP Lit)
            Material skinMat = SceneBuilder.GetOrCreateMaterial(config.isZombie ? "Mat_Skin_Zombie" : "Mat_Skin_Human", config.skinColor);
            Material shirtMat = SceneBuilder.GetOrCreateMaterial($"Mat_Shirt_{config.shirtColor.GetHashCode()}", config.shirtColor);
            Material pantsMat = SceneBuilder.GetOrCreateMaterial($"Mat_Pants_{config.pantsColor.GetHashCode()}", config.pantsColor);
            Material shoesMat = SceneBuilder.GetOrCreateMaterial($"Mat_Shoes_{config.shoesColor.GetHashCode()}", config.shoesColor);

            // 1. Pelvis (Smooth rounded horizontal capsule)
            GameObject pelvis = CreatePart(PrimitiveType.Capsule, "Pelvis", bodyHips.transform,
                new Vector3(0f, 0f, 0f),
                new Vector3(0.28f * widthMult, 0.18f * hScale, 0.22f),
                pantsMat);
            pelvis.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            // 2. Torso (Smooth Athletic Capsule with organic taper)
            GameObject torso = CreatePart(PrimitiveType.Capsule, "Torso", bodyHips.transform,
                new Vector3(0f, 0.32f * hScale, 0f),
                new Vector3(0.34f * widthMult, 0.24f * hScale, 0.24f * widthMult),
                shirtMat);

            // Vest or Coat overlay
            if (config.hasVest)
            {
                Material vestMat = SceneBuilder.GetOrCreateMaterial($"Mat_Vest_{config.vestColor.GetHashCode()}", config.vestColor);
                CreatePart(PrimitiveType.Capsule, "Vest_Overlay", torso.transform,
                    new Vector3(0f, 0f, 0f),
                    new Vector3(1.08f, 1.02f, 1.15f),
                    vestMat);
            }
            if (config.hasCoat)
            {
                Material coatMat = SceneBuilder.GetOrCreateMaterial($"Mat_Coat_{config.coatColor.GetHashCode()}", config.coatColor);
                CreatePart(PrimitiveType.Capsule, "Coat_Upper", torso.transform,
                    new Vector3(0f, 0f, 0f),
                    new Vector3(1.10f, 1.04f, 1.18f),
                    coatMat);
                // Lower coat flap down to knees
                CreatePart(PrimitiveType.Capsule, "Coat_LowerFlap", bodyHips.transform,
                    new Vector3(0f, -0.22f * hScale, 0.02f),
                    new Vector3(0.38f * widthMult, 0.26f * hScale, 0.28f * widthMult),
                    coatMat);
            }

            // Tank Armor Plates
            if (config.hasArmorPlates)
            {
                Material steelMat = SceneBuilder.GetOrCreateMaterial("Mat_Armor_ScrapMetal", new Color(0.22f, 0.24f, 0.26f));
                CreatePart(PrimitiveType.Cube, "Armor_ChestPlate", torso.transform,
                    new Vector3(0f, 0.05f, 0.58f),
                    new Vector3(0.85f, 0.65f, 0.25f),
                    steelMat);
                CreatePart(PrimitiveType.Cube, "Armor_BackPlate", torso.transform,
                    new Vector3(0f, 0.05f, -0.58f),
                    new Vector3(0.85f, 0.65f, 0.25f),
                    steelMat);
            }

            // Backpack
            if (config.hasBackpack)
            {
                Material bpMat = SceneBuilder.GetOrCreateMaterial($"Mat_BP_{config.backpackColor.GetHashCode()}", config.backpackColor);
                GameObject bp = CreatePart(PrimitiveType.Cube, "Backpack", torso.transform,
                    new Vector3(0f, 0.02f, -0.65f),
                    new Vector3(0.55f, 0.60f, 0.35f),
                    bpMat);
            }

            // Crossbody Messenger Bag
            if (config.hasCrossbag)
            {
                Material cbMat = SceneBuilder.GetOrCreateMaterial($"Mat_CB_{config.crossbagColor.GetHashCode()}", config.crossbagColor);
                CreatePart(PrimitiveType.Cube, "MessengerBag", bodyHips.transform,
                    new Vector3(0.24f * widthMult, -0.05f, -0.05f),
                    new Vector3(0.18f, 0.26f, 0.26f),
                    cbMat);
            }

            // Stethoscope for Dr. Aras
            if (config.hasStethoscope)
            {
                Material stethMat = SceneBuilder.GetOrCreateMaterial("Mat_Stethoscope", new Color(0.12f, 0.14f, 0.16f));
                CreatePart(PrimitiveType.Cylinder, "Steth_Loop", torso.transform,
                    new Vector3(0f, 0.35f, 0.45f),
                    new Vector3(0.55f, 0.05f, 0.45f),
                    stethMat);
            }

            // 3. Neck & Head (Proportioned to classical ~1:7.5 Canon)
            GameObject neck = CreatePart(PrimitiveType.Cylinder, "Neck", torso.transform,
                new Vector3(0f, 0.54f, 0.02f),
                new Vector3(0.18f, 0.12f, 0.18f),
                skinMat);

            GameObject head = CreatePart(PrimitiveType.Sphere, "Head", neck.transform,
                new Vector3(0f, 1.15f, 0f),
                new Vector3(1.25f, 1.35f, 1.25f),
                skinMat);

            // Stylized jaw / chin taper
            CreatePart(PrimitiveType.Sphere, "Jaw_Chin", head.transform,
                new Vector3(0f, -0.32f, 0.18f),
                new Vector3(0.68f, 0.52f, 0.65f),
                skinMat);

            // Eyes
            Material eyeMat = SceneBuilder.GetOrCreateMaterial(config.isZombie ? "Mat_Zombie_Eyes" : "Mat_Human_Eyes",
                config.isZombie ? new Color(1.0f, 0.15f, 0.05f) : new Color(0.12f, 0.15f, 0.20f));

            CreatePart(PrimitiveType.Sphere, "Eye_L", head.transform,
                new Vector3(-0.25f, 0.08f, 0.45f),
                new Vector3(0.14f, 0.14f, 0.14f),
                eyeMat);

            CreatePart(PrimitiveType.Sphere, "Eye_R", head.transform,
                new Vector3(0.25f, 0.08f, 0.45f),
                new Vector3(0.14f, 0.14f, 0.14f),
                eyeMat);

            // Eyewear / Glasses
            if (config.hasGlasses)
            {
                Material frameMat = SceneBuilder.GetOrCreateMaterial("Mat_Glasses_Frame", new Color(0.15f, 0.18f, 0.22f));
                CreatePart(PrimitiveType.Cube, "Glasses_Bridge", head.transform,
                    new Vector3(0f, 0.08f, 0.52f),
                    new Vector3(0.68f, 0.10f, 0.08f),
                    frameMat);
            }

            // Hair or Hat
            if (config.hasHat)
            {
                Material hatMat = SceneBuilder.GetOrCreateMaterial($"Mat_Hat_{config.hatColor.GetHashCode()}", config.hatColor);
                GameObject cap = CreatePart(PrimitiveType.Cylinder, "Cap_Dome", head.transform,
                    new Vector3(0f, 0.38f, 0.05f),
                    new Vector3(1.05f, 0.32f, 1.05f),
                    hatMat);
                CreatePart(PrimitiveType.Cube, "Cap_Visor", cap.transform,
                    new Vector3(0f, -0.3f, 0.55f),
                    new Vector3(0.95f, 0.12f, 0.60f),
                    hatMat);
            }
            else if (config.hasHair)
            {
                Material hairMat = SceneBuilder.GetOrCreateMaterial($"Mat_Hair_{config.hairColor.GetHashCode()}", config.hairColor);
                CreatePart(PrimitiveType.Sphere, "Hair_Volume", head.transform,
                    new Vector3(0f, 0.22f, -0.08f),
                    new Vector3(1.06f, 0.95f, 1.08f),
                    hairMat);
            }

            // Screamer glowing veins
            if (config.hasGlowingVeins)
            {
                Material veinMat = SceneBuilder.GetOrCreateMaterial("Mat_Glowing_Veins", new Color(0.75f, 0.15f, 0.95f));
                CreatePart(PrimitiveType.Cylinder, "Vein_Neck_L", neck.transform,
                    new Vector3(-0.35f, 0f, 0.25f),
                    new Vector3(0.12f, 0.5f, 0.12f),
                    veinMat);
                CreatePart(PrimitiveType.Cylinder, "Vein_Neck_R", neck.transform,
                    new Vector3(0.35f, 0f, 0.25f),
                    new Vector3(0.12f, 0.5f, 0.12f),
                    veinMat);
            }

            // 4. Arms & Hands (Smooth athletic shoulder spheres & capsule limbs)
            float armThickness = 0.09f * (config.isTank ? 1.4f : (config.isRunner ? 0.88f : 1.0f));
            float armLen = 0.32f * hScale;

            // Left Arm
            GameObject shoulderL = new GameObject("Shoulder_L");
            shoulderL.transform.SetParent(torso.transform);
            shoulderL.transform.localPosition = new Vector3(-0.35f, 0.24f, 0f);

            // Shoulder deltoid sphere
            CreatePart(PrimitiveType.Sphere, "Deltoid_L", shoulderL.transform, Vector3.zero, Vector3.one * 0.14f, shirtMat);

            GameObject upperArmL = CreatePart(PrimitiveType.Capsule, "UpperArm_L", shoulderL.transform,
                new Vector3(0f, -armLen * 0.45f, 0f),
                new Vector3(armThickness * 1.6f, armLen * 0.45f, armThickness * 1.6f),
                shirtMat);

            // Elbow joint
            CreatePart(PrimitiveType.Sphere, "Elbow_L", upperArmL.transform, new Vector3(0f, -0.95f, 0f), Vector3.one * 0.70f, skinMat);

            GameObject forearmL = CreatePart(PrimitiveType.Capsule, "Forearm_L", upperArmL.transform,
                new Vector3(0f, -1.02f, 0f),
                new Vector3(0.85f, 0.85f, 0.85f),
                skinMat);

            CreatePart(PrimitiveType.Sphere, "Hand_L", forearmL.transform,
                new Vector3(0f, -1.05f, 0f),
                new Vector3(0.80f, 0.60f, 0.70f),
                skinMat);

            // Right Arm
            GameObject shoulderR = new GameObject("Shoulder_R");
            shoulderR.transform.SetParent(torso.transform);
            shoulderR.transform.localPosition = new Vector3(0.35f, 0.24f, 0f);

            CreatePart(PrimitiveType.Sphere, "Deltoid_R", shoulderR.transform, Vector3.zero, Vector3.one * 0.14f, shirtMat);

            GameObject upperArmR = CreatePart(PrimitiveType.Capsule, "UpperArm_R", shoulderR.transform,
                new Vector3(0f, -armLen * 0.45f, 0f),
                new Vector3(armThickness * 1.6f, armLen * 0.45f, armThickness * 1.6f),
                shirtMat);

            CreatePart(PrimitiveType.Sphere, "Elbow_R", upperArmR.transform, new Vector3(0f, -0.95f, 0f), Vector3.one * 0.70f, skinMat);

            GameObject forearmR = CreatePart(PrimitiveType.Capsule, "Forearm_R", upperArmR.transform,
                new Vector3(0f, -1.02f, 0f),
                new Vector3(0.85f, 0.85f, 0.85f),
                skinMat);

            GameObject handR = CreatePart(PrimitiveType.Sphere, "Hand_R", forearmR.transform,
                new Vector3(0f, -1.05f, 0f),
                new Vector3(0.85f, 0.65f, 0.75f),
                skinMat);

            // Attach Weapons to Player Right Hand
            if (!config.isZombie && rootParent.CompareTag("Player"))
            {
                CreateWeaponModels(handR.transform, rootParent);
            }

            // 5. Legs & Feet (Sleek aerodynamic shoes sitting flat on ground Y = 0)
            float legThickness = 0.12f * (config.isTank ? 1.45f : 1.0f);
            float legSpread = 0.10f * widthMult;

            // Left Leg
            GameObject hipJointL = new GameObject("Hip_L");
            hipJointL.transform.SetParent(bodyHips.transform);
            hipJointL.transform.localPosition = new Vector3(-legSpread, -0.06f * hScale, 0f);

            GameObject thighL = CreatePart(PrimitiveType.Capsule, "Thigh_L", hipJointL.transform,
                new Vector3(0f, -0.20f * hScale, 0f),
                new Vector3(legThickness * 1.9f, 0.20f * hScale, legThickness * 1.9f),
                pantsMat);

            CreatePart(PrimitiveType.Sphere, "Knee_L", thighL.transform, new Vector3(0f, -0.95f, 0f), Vector3.one * 0.80f, pantsMat);

            GameObject calfL = CreatePart(PrimitiveType.Capsule, "Calf_L", thighL.transform,
                new Vector3(0f, -1.0f, 0f),
                new Vector3(0.88f, 0.85f, 0.88f),
                pantsMat);

            // Sleek aerodynamic shoe (smooth tapered capsule, zero black platform block)
            GameObject footL = CreatePart(PrimitiveType.Capsule, "Foot_L", calfL.transform,
                new Vector3(0f, -0.96f, 0.14f),
                new Vector3(0.65f, 0.16f, 0.85f),
                shoesMat);
            footL.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

            // Right Leg
            GameObject hipJointR = new GameObject("Hip_R");
            hipJointR.transform.SetParent(bodyHips.transform);
            hipJointR.transform.localPosition = new Vector3(legSpread, -0.06f * hScale, 0f);

            GameObject thighR = CreatePart(PrimitiveType.Capsule, "Thigh_R", hipJointR.transform,
                new Vector3(0f, -0.20f * hScale, 0f),
                new Vector3(legThickness * 1.9f, 0.20f * hScale, legThickness * 1.9f),
                pantsMat);

            CreatePart(PrimitiveType.Sphere, "Knee_R", thighR.transform, new Vector3(0f, -0.95f, 0f), Vector3.one * 0.80f, pantsMat);

            GameObject calfR = CreatePart(PrimitiveType.Capsule, "Calf_R", thighR.transform,
                new Vector3(0f, -1.0f, 0f),
                new Vector3(0.88f, 0.85f, 0.88f),
                pantsMat);

            GameObject footR = CreatePart(PrimitiveType.Capsule, "Foot_R", calfR.transform,
                new Vector3(0f, -0.96f, 0.14f),
                new Vector3(0.65f, 0.16f, 0.85f),
                shoesMat);
            footR.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

            // Posture arm offsets
            if (config.isZombie)
            {
                shoulderL.transform.localRotation = Quaternion.Euler(-35f, -15f, 12f);
                shoulderR.transform.localRotation = Quaternion.Euler(-42f, 12f, -10f);
            }
            else
            {
                shoulderL.transform.localRotation = Quaternion.Euler(6f, 0f, 8f);
                shoulderR.transform.localRotation = Quaternion.Euler(6f, 0f, -8f);
            }

            return visual;
        }

        private static void CreateWeaponModels(Transform handSocket, Transform playerRoot)
        {
            Material gunMetalMat = SceneBuilder.GetOrCreateMaterial("Mat_GunMetal", new Color(0.12f, 0.13f, 0.15f), smoothness: 0.7f);
            Material woodStockMat = SceneBuilder.GetOrCreateMaterial("Mat_AxeWood", new Color(0.40f, 0.26f, 0.16f), smoothness: 0.3f);
            Material axeSteelMat = SceneBuilder.GetOrCreateMaterial("Mat_AxeSteel", new Color(0.75f, 0.78f, 0.82f), smoothness: 0.85f);
            Material axeRedMat = SceneBuilder.GetOrCreateMaterial("Mat_AxeRed", new Color(0.85f, 0.18f, 0.15f));

            // 1. Fire Axe
            GameObject visualAxe = new GameObject("Visual_Weapon_Axe");
            visualAxe.transform.SetParent(handSocket);
            visualAxe.transform.localPosition = new Vector3(0f, 0.05f, 0.15f);
            visualAxe.transform.localRotation = Quaternion.Euler(15f, 0f, -20f);

            // Handle
            CreatePart(PrimitiveType.Cylinder, "Handle", visualAxe.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.045f, 0.38f, 0.045f), woodStockMat);
            // Steel Blade
            CreatePart(PrimitiveType.Cube, "Blade_Steel", visualAxe.transform, new Vector3(0f, 0.48f, 0.08f), new Vector3(0.05f, 0.14f, 0.20f), axeSteelMat);
            CreatePart(PrimitiveType.Cube, "Blade_RedBack", visualAxe.transform, new Vector3(0f, 0.48f, -0.06f), new Vector3(0.055f, 0.12f, 0.08f), axeRedMat);

            // 2. 9mm Pistol
            GameObject visualPistol = new GameObject("Visual_Weapon_Pistol");
            visualPistol.transform.SetParent(handSocket);
            visualPistol.transform.localPosition = new Vector3(0f, -0.02f, 0.18f);
            visualPistol.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            // Grip
            CreatePart(PrimitiveType.Cube, "Grip", visualPistol.transform, new Vector3(0f, -0.06f, -0.03f), new Vector3(0.038f, 0.12f, 0.055f), gunMetalMat);
            // Slide & Barrel
            CreatePart(PrimitiveType.Cube, "Slide", visualPistol.transform, new Vector3(0f, 0.02f, 0.05f), new Vector3(0.042f, 0.055f, 0.22f), gunMetalMat);

            // Muzzle Point
            GameObject muzzlePistol = new GameObject("Muzzle_Point");
            muzzlePistol.transform.SetParent(visualPistol.transform);
            muzzlePistol.transform.localPosition = new Vector3(0f, 0.02f, 0.17f);

            // Muzzle Flash Light
            GameObject lightObj = new GameObject("Muzzle_Light");
            lightObj.transform.SetParent(muzzlePistol.transform);
            lightObj.transform.localPosition = Vector3.zero;
            Light mLight = lightObj.AddComponent<Light>();
            mLight.type = LightType.Point;
            mLight.color = new Color(1.0f, 0.85f, 0.45f);
            mLight.range = 7f;
            mLight.intensity = 2.8f;
            mLight.enabled = false;

            // 3. Shotgun
            GameObject visualShotgun = new GameObject("Visual_Weapon_Shotgun");
            visualShotgun.transform.SetParent(handSocket);
            visualShotgun.transform.localPosition = new Vector3(0f, 0.02f, 0.22f);
            visualShotgun.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            // Stock
            CreatePart(PrimitiveType.Cube, "Stock", visualShotgun.transform, new Vector3(0f, -0.05f, -0.16f), new Vector3(0.045f, 0.09f, 0.24f), woodStockMat);
            // Receiver
            CreatePart(PrimitiveType.Cube, "Receiver", visualShotgun.transform, new Vector3(0f, 0.01f, 0.04f), new Vector3(0.05f, 0.07f, 0.18f), gunMetalMat);
            // Long Barrel
            GameObject barrel = CreatePart(PrimitiveType.Cylinder, "Barrel", visualShotgun.transform, new Vector3(0f, 0.03f, 0.32f), new Vector3(0.04f, 0.22f, 0.04f), gunMetalMat);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // 4. Tactical Crossbow
            GameObject visualCrossbow = new GameObject("Visual_Weapon_Crossbow");
            visualCrossbow.transform.SetParent(handSocket);
            visualCrossbow.transform.localPosition = new Vector3(0f, 0.02f, 0.20f);
            visualCrossbow.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            CreatePart(PrimitiveType.Cube, "Stock_Rail", visualCrossbow.transform, new Vector3(0f, 0f, 0f), new Vector3(0.045f, 0.055f, 0.42f), gunMetalMat);
            CreatePart(PrimitiveType.Cube, "Prod_Limbs", visualCrossbow.transform, new Vector3(0f, 0.02f, 0.18f), new Vector3(0.36f, 0.035f, 0.025f), axeSteelMat);
            CreatePart(PrimitiveType.Cube, "Stirrup", visualCrossbow.transform, new Vector3(0f, 0f, 0.23f), new Vector3(0.08f, 0.02f, 0.05f), axeSteelMat);
            GameObject optic = CreatePart(PrimitiveType.Cylinder, "Reflex_Optic", visualCrossbow.transform, new Vector3(0f, 0.05f, -0.05f), new Vector3(0.028f, 0.06f, 0.028f), gunMetalMat);
            optic.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Connect to WeaponSystem
            WeaponSystem ws = playerRoot.GetComponent<WeaponSystem>();
            if (ws != null)
            {
                ws.SetVisualReferences(visualAxe, visualPistol, visualShotgun, muzzlePistol.transform, mLight, visualCrossbow);
            }
        }

        private static GameObject CreatePart(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.localPosition = localPos;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;

            Collider col = part.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            if (mat != null)
            {
                Renderer r = part.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }

            return part;
        }
    }
}
