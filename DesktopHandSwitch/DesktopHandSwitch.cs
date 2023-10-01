using HarmonyLib;
using FrooxEngine;
using System;
using ResoniteModLoader;

// ReSharper disable InconsistentNaming

namespace DesktopHandSwitch
{
    public class DesktopHandSwitch : ResoniteMod
    {
        public override string Name => "DesktopHandSwitch";
        public override string Author => "TheJebForge";
        public override string Version => "1.1.0";

        [AutoRegisterConfigKey]
        readonly ModConfigurationKey<Key> ACTIVATION_KEY = new ModConfigurationKey<Key>("activation_key","Activation Key", () => Key.LeftAlt);
        [AutoRegisterConfigKey]
        readonly ModConfigurationKey<bool> ACTIVATION_MODE = new ModConfigurationKey<bool>("activation_mode","Switch on hold if enabled, toggle switch if disabled", () => true);

        static DesktopHandSwitch modInstance;
        static Chirality initialChirality = Chirality.Right;
        static bool initialChiralityWasSet;
        static DateTime lastSwitch = DateTime.UtcNow;

        ModConfiguration config;
        
        public override void OnEngineInit() {
            modInstance = this;
            config = GetConfiguration();
            
            Harmony harmony = new Harmony($"net.{Author}.{Name}");
            harmony.PatchAll();
        }

        Key GetActivationKey() {
            config.TryGetValue(ACTIVATION_KEY, out var key);
            return key;
        }

        bool GetActivationMode() {
            config.TryGetValue(ACTIVATION_MODE, out var mode);
            return mode;
        }

        static Chirality OppositeChirality(Chirality chirality) {
            return chirality == Chirality.Left ? Chirality.Right : Chirality.Left;
        }

        [HarmonyPatch(typeof(InteractionHandler), "OnStart")]
        class CommonTool_OnStart_Patch
        {
            static void Postfix(InteractionHandler __instance) {
                if (initialChiralityWasSet) return;
                
                initialChirality = __instance.InputInterface.PrimaryHand;
                initialChiralityWasSet = true;
            }
        }
        
        [HarmonyPatch(typeof(InteractionHandler), "OnInputUpdate")]
        class CommonTool_OnInputUpdate_Patch
        {
            static void Prefix(InteractionHandler __instance) {
                if (modInstance.GetActivationMode()) {
                    if (__instance.InputInterface.GetKeyDown(modInstance.GetActivationKey())) {
                        __instance.InputInterface.PrimaryHand = OppositeChirality(initialChirality);
                    }

                    if (__instance.InputInterface.GetKeyUp(modInstance.GetActivationKey())) {
                        __instance.InputInterface.PrimaryHand = initialChirality;
                    }
                }
                else {
                    if (__instance.InputInterface.GetKeyDown(modInstance.GetActivationKey())) {
                        TimeSpan timePassed = DateTime.UtcNow - lastSwitch;

                        if (timePassed.TotalSeconds > 0.1) {
                            __instance.InputInterface.PrimaryHand = OppositeChirality(__instance.InputInterface.PrimaryHand);
                            lastSwitch = DateTime.UtcNow;
                        }
                    }
                }
            }
        }
    }
}