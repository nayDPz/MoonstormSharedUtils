﻿using RoR2;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;

namespace MSU
{
    /// <summary>
    /// Class used to create new Material Overlays for BuffDefs
    /// </summary>
    public static class BuffOverlays
    {
        /// <summary>
        /// A read only dictionary of BuffDef to Material. These materials are later applied as Overlays to CharacterBodies when they have the BuffDef
        /// </summary>
        public static ReadOnlyDictionary<BuffDef, Material> buffOverlayDictionary { get; private set; }
        private static Dictionary<BuffDef, Material> _buffOverlays = new Dictionary<BuffDef, Material>();

        /// <summary>
        /// Wether the BuffOverlayDictionary has been created or not.
        /// </summary>
        public static bool dictionaryCreated { get; private set; } = false;

        [SystemInitializer(typeof(BuffCatalog))]
        private static void SystemInit()
        {
            dictionaryCreated = true;
            MSULog.Info("Initializing Buff Overlays...");
            On.RoR2.CharacterModel.UpdateOverlays += AddBuffOverlay;

            buffOverlayDictionary = new ReadOnlyDictionary<BuffDef, Material>(_buffOverlays);
            _buffOverlays = null;
        }

        /// <summary>
        /// Adds a new Buff Material pair to the Overlays system
        /// </summary>
        /// <param name="def">The BuffDef that will have a new Overlay</param>
        /// <param name="material">The Material for the BuffDef</param>
        public static void AddBuffOverlay(BuffDef def, Material material)
        {
            if (dictionaryCreated)
            {
#if DEBUG
                MSULog.Info("Buff Overlay Dictionary already created.");
#endif
                return;
            }
            
            if(!def)
            {
#if DEBUG
                MSULog.Warning($"BuffDef is null for overlay with material {material}");
#endif
                return;
            }

            if(!material)
            {
#if DEBUG
                MSULog.Warning($"Material is null for buff def {def}");
#endif
                return;
            }

            if(_buffOverlays.ContainsKey(def))
            {
#if DEBUG
                MSULog.Info($"The BuffDef {def} already has an overlay material assigned. (Material={_buffOverlays[def]})");
#endif
                return;
            }
            _buffOverlays.Add(def, material);
        }

        private static void AddBuffOverlay(On.RoR2.CharacterModel.orig_UpdateOverlays orig, CharacterModel self)
        {
            orig(self);
            if (!self.body)
                return;
            foreach(var (buff, material) in buffOverlayDictionary)
            {
                if (self.body.HasBuff(buff))
                    AddOverlay(self, material);
            }
        }

        private static void AddOverlay(CharacterModel model, Material overlayMaterial)
        {
            if (model.activeOverlayCount >= CharacterModel.maxOverlays || !overlayMaterial)
                return;

            Material[] array = model.currentOverlays;
            int num = model.activeOverlayCount;
            model.activeOverlayCount = num + 1;
            array[num] = overlayMaterial;
        }
    }
}