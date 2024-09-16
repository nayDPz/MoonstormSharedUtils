﻿using EntityStates;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MSU
{
    //TODO: Make it so family events are displayed with this too.
    /// <summary>
    /// The <see cref="GameplayEventTextController"/> is a Singleton component that manages the EventText system of <see cref="GameplayEvent"/>s.
    /// <br>The EventText are large, bold texts that appear when an event starts and when an event ends.</br>
    /// <pr>This text is automatically enqueued when spawning an Event via the <see cref="GameplayEventManager"/>, otherwise no text is enqueued when spawning <see cref="GameplayEvent"/>s manually.</pr>
    /// </summary>
    public sealed class GameplayEventTextController : MonoBehaviour
    {
        /// <summary>
        /// Returns the current instance of the GameplayEventTextController.
        /// </summary>
        public static GameplayEventTextController instance { get; private set; }
        private static GameObject _prefab;

        /// <summary>
        /// The EntityStateMachine that's running the required fade in/out states of the text.
        /// </summary>
        public EntityStateMachine textStateMachine { get; private set; }

        /// <summary>
        /// The <see cref="HGTextMeshProUGUI"/> component that's displaying the event text.
        /// </summary>
        public HGTextMeshProUGUI textMeshProUGUI { get; private set; }

        /// <summary>
        /// The HUD instance that this GameplayEventTextController is a child of.
        /// </summary>
        public HUD hudInstance { get; private set; }
        private Queue<EventTextRequest> _textRequests = new Queue<EventTextRequest>();

        /// <summary>
        /// Returns the current event text request that's being processed.
        /// </summary>
        public EventTextRequest? currentTextRequest { get; private set; }

        private TMPro.TMP_FontAsset _bombadierFontAsset;

        [SystemInitializer]
        private static void SystemInit()
        {
            _prefab = MSUMain.msuAssetBundle.LoadAsset<GameObject>("GameplayEventText");
            On.RoR2.UI.HUD.Awake += SpawnAndGetInstance;
        }

        private static void SpawnAndGetInstance(On.RoR2.UI.HUD.orig_Awake orig, RoR2.UI.HUD self)
        {
            orig(self);
            GameObject.Instantiate(_prefab, self.mainContainer.transform);
            instance.hudInstance = self;
        }

        [ConCommand(commandName = "test_event_text", flags = ConVarFlags.None, helpText = "Tests the GameplayEventTextController with a generic EventTextRequest")]
        private static void CC_TestEventText(ConCommandArgs args)
        {
            if (!instance)
                return;

            instance.EnqueueNewTextRequest(new EventTextRequest
            {
                eventToken = "Event Text Test",
                eventColor = Color.cyan,
                textDuration = 15
            });
        }

        /// <summary>
        /// Enqueues a new <see cref="EventTextRequest"/> to be displayed
        /// </summary>
        /// <param name="request">The EventText to display</param>
        public void EnqueueNewTextRequest(EventTextRequest request)
        {
            _textRequests.Enqueue(request);
        }

        private void Update()
        {
            //no request being processed, and there's a pending request, dequeue and initialize.
            if(!currentTextRequest.HasValue && _textRequests.Count > 0)
            {
                DequeueAndInitializeRequest();
                return;
            }

            //If there is a request, and its not being proceessed, process it.
            if(currentTextRequest.HasValue && textStateMachine.state is Idle)
            {
                var value = currentTextRequest.Value;

                var hasOverrideState = value.customTextState.HasValue;

                EventTextState state = null;
                if(hasOverrideState)
                {
                    var val = value.customTextState.Value;
                    state = (EventTextState)EntityStateCatalog.InstantiateState(ref val);
                
                }
                else
                {
                    state = new FadeInState();
                }
                state.duration = hasOverrideState ? value.textDuration : value.textDuration / 3;
                textStateMachine.SetNextState(state);
            }
        }

        internal void NullCurrentRequest()
        {
            currentTextRequest = null;
        }

        private void DequeueAndInitializeRequest()
        {
            currentTextRequest = _textRequests.Dequeue();
            string tokenValue = currentTextRequest.Value.tokenValue;
            Color messageColor = currentTextRequest.Value.eventColor;
            Color outlineColor = currentTextRequest.Value.GetBestOutlineColor();

            textMeshProUGUI.text = tokenValue;
            textMeshProUGUI.color = messageColor;
            textMeshProUGUI.outlineColor = outlineColor;
            textMeshProUGUI.font = currentTextRequest.Value.customFontAsset.hasValue ? currentTextRequest.Value.customFontAsset.value : _bombadierFontAsset;
        }

        private void Start()
        {
            _bombadierFontAsset = textMeshProUGUI.font;
        }

        private void Awake()
        {
            textStateMachine = GetComponent<EntityStateMachine>();
            textMeshProUGUI = GetComponent<HGTextMeshProUGUI>();
        }

        private void OnEnable()
        {
            instance = this;
            textMeshProUGUI.text = string.Empty;
        }
        private void OnDisable()
        {
            if (instance == this)
                instance = null;
        }

        /// <summary>
        /// Struct that represents a request to display an EventText.
        /// </summary>
        public struct EventTextRequest
        {
            /// <summary>
            /// The token to use for the text, the <see cref="GameplayEventTextController"/> uses <see cref="tokenValue"/> to obtain the correct string in the current language.
            /// </summary>
            public string eventToken;
            /// <summary>
            /// The color for this event text, the outline for this event text is calculated using <see cref="GetBestOutlineColor"/>
            /// </summary>
            public Color eventColor;

            /// <summary>
            /// The total duration that this event text should last. If <see cref="customTextState"/>'s value is null, this value will be passed to <see cref="FadeInState"/> with the duration value of <see cref="textDuration"/> / 3 (IE: A text duration of 6 will cause the FadeIn state to last 2 seconds, The wait state 2 seconds, and the FadeOut state to last 2 seconds)
            /// <para>In case <see cref="customTextState"/> is not null, this value will be passed raw to the state specified in <see cref="customTextState"/></para>
            /// </summary>
            public float textDuration;

            /// <summary>
            /// If supplied, the <see cref="GameplayEventTextController.textStateMachine"/>'s state will be set to this state, The state needs to inherit from <see cref="EventTextState"/>.
            /// <br>remember to eventually set the state machine's state back to main, otherwise no more text requests will be processed.</br>
            /// <br>The final state (the one that sets the state machine's state back to main) should also call <see cref="EventTextState.NullRequest"/> for proper disposal of the request.</br>
            /// </summary>
            public SerializableEntityStateType? customTextState;

            /// <summary>
            /// If supplied, this font asset is used for the lifetime of this Request, if no FontAsset is supplied, the base game's "Bombardier" font is used.
            /// </summary>
            public NullableRef<TMPro.TMP_FontAsset> customFontAsset;

            /// <summary>
            /// The value of <see cref="eventToken"/> using the currently loaded language
            /// </summary>
            public string tokenValue => Language.GetString(eventToken);

            /// <summary>
            /// Obtains the best outline color to use with <see cref="eventColor"/>
            /// <br>This color is calculated depending on the <see cref="eventColor"/>'s light value</br>.
            /// </summary>
            /// <returns>The best outline color</returns>
            public Color GetBestOutlineColor()
            {
                Color.RGBToHSV(eventColor, out float hue, out float saturation, out float light);

                float modifier = light > 0.5f ? (-0.5f) : 0.5f;
                float newSaturation = Mathf.Clamp01(saturation + modifier);
                float newLight = Mathf.Clamp01(light + modifier);
                return Color.HSVToRGB(hue, newSaturation, newLight);
            }
        }

        /// <summary>
        /// The base class for all EventText related entity states
        /// </summary>
        public abstract class EventTextState : EntityState
        {
            /// <summary>
            /// Returns the GameplayEventTextcontroller that created this state.
            /// </summary>
            public GameplayEventTextController textController { get; private set; }

            /// <summary>
            /// The UIJuice component attached to the <see cref="GameplayEventTextController"/>
            /// </summary>
            public UIJuice uiJuice { get; private set; }

            /// <summary>
            /// The total duration this state should last.
            /// </summary>
            public float duration;

            public override void OnEnter()
            {
                base.OnEnter();
                textController = GetComponent<GameplayEventTextController>();
                uiJuice = GetComponent<UIJuice>();
            }

            /// <summary>
            /// Method used for nulling the current request in the TExtController, usually the request that instantiated this state.
            /// </summary>
            protected virtual void NullRequest()
            {
                textController.NullCurrentRequest();
            }
        }

        /// <summary>
        /// State used for EventTexts where the Text fades in
        /// </summary>
        public class FadeInState : EventTextState
        {
            public override void OnEnter()
            {
                base.OnEnter();
                uiJuice.destroyOnEndOfTransition = false;
                uiJuice.transitionDuration = duration;
                uiJuice.TransitionAlphaFadeIn();
                uiJuice.originalAlpha = 1;
                uiJuice.transitionEndAlpha = 1;
            }

            public override void Update()
            {
                base.Update();
                if(age > duration)
                {
                    outer.SetNextState(new WaitState
                    {
                        duration = duration
                    });
                }
            }
        }

        /// <summary>
        /// State used for EventTexts where the text has faded in completely and is now waiting
        /// </summary>
        public class WaitState : EventTextState
        {
            public override void Update()
            {
                base.Update();
                if(age > duration)
                {
                    outer.SetNextState(new FadeOutState
                    {
                        duration = duration
                    });
                }
            }
        }

        /// <summary>
        /// State used for EventTexts where the text has finished waiting and is now fading out
        /// </summary>
        public class FadeOutState : EventTextState
        {
            public override void OnEnter()
            {
                base.OnEnter();
                uiJuice.TransitionAlphaFadeOut();
            }

            public override void Update()
            {
                base.Update();
                if(age > duration)
                {
                    textController.NullCurrentRequest();
                    outer.SetNextStateToMain();
                }
            }
        }
    }
}