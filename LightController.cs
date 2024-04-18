using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static CharacterAnimEvent;
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

        private GameObject m_visual;

        private bool m_indoors;

        private bool m_forceUpdate = true;

        private float m_updateVisualTimer = 0f;

        private static readonly List<LanternLightController> Instances = new List<LanternLightController>();
        
        const int c_characterLayer = 9;

        void Awake()
        {
            Transform mainLight = transform.Find(LanternItem.c_pointLightName);

            m_mainLight = mainLight.GetComponent<Light>();
            m_mainLightFlicker = mainLight.GetComponent<LightFlicker>();
            m_mainLightLod = mainLight.GetComponent<LightLod>();

            m_spotLight = transform.Find(LanternItem.c_spotLightName).GetComponent<Light>();

            m_material = GetComponent<MeshRenderer>().sharedMaterial;
        }

        void Start()
        {
            m_character = transform.root.GetComponent<Character>();
            m_visual = m_character?.GetVisual();
            UpdateVisualLayers();
        }

        void Update()
        {
            if (m_mainLight.color != lightColor.Value || m_forceUpdate)
            {
                m_mainLight.color = lightColor.Value;
                m_spotLight.color = lightColor.Value;
                m_material.SetColor("_EmissionColor", lightColor.Value);
            }

            if (m_character == null)
                return;

            if (m_character.InInterior() && (!m_indoors || m_forceUpdate))
            {
                m_indoors = true;

                m_mainLight.intensity = lightIntensityIndoors.Value;
                m_mainLightFlicker.m_baseIntensity = lightIntensityIndoors.Value;
                m_mainLight.range = lightRangeIndoors.Value;
                m_mainLight.shadowStrength = lightShadowsIndoors.Value;

                SetLightLodState(m_mainLightLod);

                m_mainLight.shadows = m_mainLight.shadowStrength > 0 ? LightShadows.Soft : LightShadows.None;
            }
            else if (!m_character.InInterior() && (m_indoors || m_forceUpdate))
            {
                m_indoors = true;

                m_mainLight.intensity = lightIntensityOutdoors.Value;
                m_mainLightFlicker.m_baseIntensity = lightIntensityOutdoors.Value;
                m_mainLight.range = lightRangeOutdoors.Value;
                m_mainLight.shadowStrength = lightShadowsOutdoors.Value;

                SetLightLodState(m_mainLightLod);

                m_mainLight.shadows = m_mainLight.shadowStrength > 0 ? LightShadows.Soft : LightShadows.None;
            }

            m_forceUpdate = false;
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

        private void SetLightLodState(LightLod lightLod)
        {
            lightLod.m_lightDistance = lightLod.m_light.range;
            lightLod.m_baseRange = lightLod.m_light.range;
            lightLod.m_baseShadowStrength = lightLod.m_light.shadowStrength;
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

        internal static void UpdateLightState()
        {
            foreach (LanternLightController instance in Instances)
                instance.m_forceUpdate = true;
        }

        internal static void UpdateVisualsLayers(GameObject visual)
        {
            foreach (LanternLightController instance in Instances)
                if (instance.m_visual == visual)
                    instance.StartUpdateVisualLayers();
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
