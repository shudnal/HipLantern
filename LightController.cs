using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using static HipLantern.HipLantern;

namespace HipLantern
{
    public class LanternLightController : MonoBehaviour
    {
        private Light m_mainLight;
        private LightFlicker m_mainLightFlicker;
        private LightLod m_mainLightLod;

        private Light m_spotLight;

        private Character m_character;
        private Material m_material;
        private ItemDrop m_itemDrop;

        private GameObject m_visual;
        private GameObject m_insects;
        private GameObject m_flare;

        private float m_updateVisualTimer = 0f;

        private static readonly List<LanternLightController> Instances = new List<LanternLightController>();
        
        const int c_characterLayer = 9;

        void Awake()
        {
            Transform mainLight = transform.Find(LanternItem.c_pointLightName);

            m_mainLight = mainLight.GetComponent<Light>();
            m_mainLightFlicker = mainLight.GetComponent<LightFlicker>();
            m_mainLightLod = mainLight.GetComponent<LightLod>();

            m_spotLight = transform.Find(LanternItem.c_spotLightName)?.GetComponent<Light>();

            m_material = GetComponent<MeshRenderer>().sharedMaterial;

            m_insects = transform.Find("insects")?.gameObject;
            m_flare = transform.Find("flare")?.gameObject;

            m_itemDrop = GetComponentInParent<ItemDrop>();
        }

        void Start()
        {
            m_character = transform.root.GetComponent<Character>();
            m_visual = m_character?.GetVisual();
            UpdateVisualLayers();
        }

        void Update()
        {
            m_mainLight.color = lightColor.Value;

            if (m_spotLight)
                m_spotLight.color = lightColor.Value;

            if (m_itemDrop != null)
            {
                m_mainLight.gameObject.SetActive(false);
                m_flare?.SetActive(IsTimeToLight());
                m_material.SetColor("_EmissionColor", lightColor.Value);
            }
            else if (m_character == null)
            {
                m_mainLight.intensity = lightIntensityOutdoors.Value;
                m_mainLightFlicker.m_baseIntensity = lightIntensityOutdoors.Value;
                m_mainLight.range = lightRangeOutdoors.Value;

                m_mainLightLod.m_lightDistance = LanternItem.c_lightLodDistance * 2;
                m_mainLightLod.m_baseRange = lightRangeOutdoors.Value;

                m_mainLight.shadows = LightShadows.None;
                m_mainLightLod.m_shadowLod = false;

                m_insects?.SetActive(IsNightTime());
                m_flare?.SetActive(IsTimeToLight());
                m_material.SetColor("_EmissionColor", new Color(lightColor.Value.r + (m_flare.activeSelf ? 0.25f : 0.1f), 
                                                                lightColor.Value.g + (m_flare.activeSelf ? 0.25f : 0.1f), 
                                                                lightColor.Value.b + (m_flare.activeSelf ? 0.25f : 0.1f), 
                                                                lightColor.Value.a));
            }
            else if (m_character.InInterior())
            {
                m_mainLight.intensity = lightIntensityIndoors.Value;
                m_mainLightFlicker.m_baseIntensity = lightIntensityIndoors.Value;
                m_mainLight.range = lightRangeIndoors.Value;
                m_mainLight.shadowStrength = lightShadowsIndoors.Value;

                m_mainLightLod.m_lightDistance = Mathf.Max(lightRangeIndoors.Value + 10f, LanternItem.c_lightLodDistance);
                m_mainLightLod.m_baseRange = lightRangeIndoors.Value;
                m_mainLightLod.m_baseShadowStrength = lightShadowsIndoors.Value;

                m_mainLight.shadows = m_mainLight.shadowStrength > 0 ? LightShadows.Soft : LightShadows.None;
                m_material.SetColor("_EmissionColor", new Color(lightColor.Value.r + 0.25f, lightColor.Value.g + 0.25f, lightColor.Value.b + 0.25f, lightColor.Value.a));
            }
            else
            {
                m_mainLight.intensity = lightIntensityOutdoors.Value;
                m_mainLightFlicker.m_baseIntensity = lightIntensityOutdoors.Value;
                m_mainLight.range = lightRangeOutdoors.Value;
                m_mainLight.shadowStrength = lightShadowsOutdoors.Value;

                m_mainLightLod.m_lightDistance = Mathf.Max(lightRangeOutdoors.Value + 10f, LanternItem.c_lightLodDistance);
                m_mainLightLod.m_baseRange = lightRangeOutdoors.Value;
                m_mainLightLod.m_baseShadowStrength = lightShadowsOutdoors.Value;

                m_mainLight.shadows = m_character != null && m_mainLight.shadowStrength > 0 ? LightShadows.Soft : LightShadows.None;
                m_material.SetColor("_EmissionColor", new Color(lightColor.Value.r + 0.25f, lightColor.Value.g + 0.25f, lightColor.Value.b + 0.25f, lightColor.Value.a));
            }
        }

        void FixedUpdate()
        {
            if (m_updateVisualTimer > 0)
            {
                m_updateVisualTimer = Mathf.Max(0f, m_updateVisualTimer - Time.fixedDeltaTime);

                if (m_updateVisualTimer == 0f)
                    UpdateVisualLayers();
            }
        }

        void OnEnable()
        {
            Instances.Add(this);
        }

        void OnDisable()
        {
            Instances.Remove(this);
        }

        private void UpdateVisualLayers()
        {
            if (m_visual == null)
                return;

            for (int i = 0; i < m_visual.transform.childCount; i++)
            {
                Transform child = m_visual.transform.GetChild(i);
                if (child.gameObject.layer == c_characterLayer)
                    continue;

                child.gameObject.layer = c_characterLayer;

                Transform[] children = child.GetComponentsInChildren<Transform>(includeInactive: true);
                foreach (Transform chld in children)
                    chld.gameObject.layer = c_characterLayer;
            }
        }

        private void StartUpdateVisualLayers()
        {
            m_updateVisualTimer = 0.5f;
        }

        internal static void UpdateVisualsLayers(GameObject visual)
        {
            foreach (LanternLightController instance in Instances)
                if (instance.m_visual == visual)
                    instance.StartUpdateVisualLayers();
        }

        private bool IsNightTime()
        {
            return transform.position.y > 3000f || EnvMan.IsNight();
        }

        private bool IsTimeToLight()
        {
            if (IsNightTime())
                return true;

            if (!EnvMan.IsDaylight() || !EnvMan.instance)
                return true;

            float dayFraction = EnvMan.instance.GetDayFraction();

            if (!(dayFraction <= 0.3f))
                return dayFraction >= 0.69f;

            return true;
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupEquipment))]
        public static class Humanoid_SetupVisEquipment_AttachLayersFix
        {
            private static void Postfix(Humanoid __instance)
            {
                UpdateVisualsLayers(__instance.m_visual);
            }
        }
    }
}
