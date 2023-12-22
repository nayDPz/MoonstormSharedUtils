﻿using BepInEx;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MSU
{
    public static class EquipmentModule
    {
        public static ReadOnlyDictionary<EquipmentDef, IEquipmentContentPiece> AllMoonstormEquipments { get; private set; }
        public static ReadOnlyDictionary<EquipmentDef, IEliteContentPiece> MoonstormEliteEquipments { get; private set; }
        public static ReadOnlyDictionary<EquipmentDef, IEquipmentContentPiece> NonEliteMoonstormEquipments { get; private set; }
        public static ReadOnlyCollection<EliteDef> MoonstormEliteDefs { get; private set; }

        private static Dictionary<EquipmentDef, IEquipmentContentPiece> _moonstormEquipments = new Dictionary<EquipmentDef, IEquipmentContentPiece>();

        public static ResourceAvailability moduleAvailability;


        private static Dictionary<BaseUnityPlugin, IEquipmentContentPiece[]> _pluginToEquipments = new Dictionary<BaseUnityPlugin, IEquipmentContentPiece[]>();
        private static Dictionary<BaseUnityPlugin, IContentPieceProvider<EquipmentDef>> _pluginToContentProvider = new Dictionary<BaseUnityPlugin, IContentPieceProvider<EquipmentDef>>();
        public static void AddProvider(BaseUnityPlugin plugin, IContentPieceProvider<EquipmentDef> provider)
        {
            _pluginToContentProvider.Add(plugin, provider);
        }

        public static IEquipmentContentPiece[] GetEquipments(BaseUnityPlugin plugin)
        {
            if (_pluginToEquipments.TryGetValue(plugin, out var items))
            {
                return items;
            }
            return null;
        }

        public static IEnumerator InitialzeEquipments(BaseUnityPlugin plugin)
        {
            if (_pluginToContentProvider.TryGetValue(plugin, out IContentPieceProvider<EquipmentDef> provider))
            {
                yield return InitializeEquipmentsFromProvider(plugin, provider);
            }
            yield break;
        }

        [SystemInitializer(typeof(EquipmentCatalog))]
        private static void SystemInit()
        {
            On.RoR2.EquipmentSlot.PerformEquipmentAction += PerformAction;

            var allEquips = new Dictionary<EquipmentDef, IEquipmentContentPiece>();
            var nonEliteEquips = new Dictionary<EquipmentDef, IEquipmentContentPiece>();
            var eliteEquips = new Dictionary<EquipmentDef, IEliteContentPiece>();

            foreach(var(eqpDef, eqp) in _moonstormEquipments)
            {
                allEquips.Add(eqpDef, eqp);
                if(eqp is IEliteContentPiece eliteContent)
                {
                    eliteEquips.Add(eqpDef, eliteContent);
                }
                else
                {
                    nonEliteEquips.Add(eqpDef, eqp);
                }
            }

            AllMoonstormEquipments = new ReadOnlyDictionary<EquipmentDef, IEquipmentContentPiece>(allEquips);
            MoonstormEliteEquipments = new ReadOnlyDictionary<EquipmentDef, IEliteContentPiece>(eliteEquips);
            NonEliteMoonstormEquipments = new ReadOnlyDictionary<EquipmentDef, IEquipmentContentPiece>(nonEliteEquips);

            moduleAvailability.MakeAvailable();
        }

        private static bool PerformAction(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, EquipmentSlot self, EquipmentDef equipmentDef)
        {
            if(AllMoonstormEquipments.TryGetValue(equipmentDef, out var equipment))
            {
                return equipment.Execute(self);
            }
            return orig(self, equipmentDef);
        }

        private static IEnumerator InitializeEquipmentsFromProvider(BaseUnityPlugin plugin, IContentPieceProvider<EquipmentDef> provider)
        {
            IContentPiece<EquipmentDef>[] content = provider.GetContents();

            foreach (var equipment in content)
            {
                yield return InitializeEquipment(equipment, plugin, provider);
            }
        }

        private static IEnumerator InitializeEquipment(IContentPiece<EquipmentDef> equipment, BaseUnityPlugin plugin, IContentPieceProvider<EquipmentDef> provider)
        {
            if (!equipment.IsAvailable())
            {
                yield break;
            }

            yield return equipment.LoadContentAsync();

            equipment.Initialize();

            var asset = equipment.Asset;
            provider.ContentPack.AddToArraySafe(ref provider.ContentPack.equipmentDefs, asset);

            if (equipment is IContentPackModifier packModifier)
            {
                packModifier.ModifyContentPack(provider.ContentPack);
            }

            if (equipment is IEquipmentContentPiece equipmentContentPiece)
            {
                if (!_pluginToEquipments.ContainsKey(plugin))
                {
                    _pluginToEquipments.Add(plugin, Array.Empty<IEquipmentContentPiece>());
                }
                var array = _pluginToEquipments[plugin];
                HG.ArrayUtils.ArrayAppend(ref array, equipmentContentPiece);
                _moonstormEquipments.Add(asset, equipmentContentPiece);
            }

            if(equipment is IEliteContentPiece eliteContentPiece)
            {
                foreach(var eliteDef in eliteContentPiece.EliteDefs)
                {
                    provider.ContentPack.AddToArraySafe(ref provider.ContentPack.eliteDefs, eliteDef);
                }
                provider.ContentPack.AddToArraySafe(ref provider.ContentPack.buffDefs, asset.passiveBuffDef);
            }
        }
    }
}