﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R2API.ScriptableObjects;
using System.Collections.ObjectModel;
using System;
using System.Linq;

namespace Moonstorm
{
    public abstract class ContentModule<T> : ModuleBase<T> where T : ContentBase
    {
        public abstract R2APISerializableContentPack SerializableContentPack { get; }

        protected bool AddSafely<TAsset>(ref TAsset[] contentPackArray, TAsset content, string correspondingArrayName = null) where TAsset : UnityEngine.Object
        {
            if (contentPackArray.Contains(content)) //Content already in the contentPack for whatever reason? return true;
            {
                MSULog.Warning($"Content {content} was already in {SerializableContentPack}'s {correspondingArrayName ?? content.GetType().Name} array!\n" +
                    $"MSU automatically adds the content piece to its corresponding array in initialization, do not add it beforehand.");
                return true;
            }

            HG.ArrayUtils.ArrayAppend(ref contentPackArray, content);
            return true;
        }
    }
}