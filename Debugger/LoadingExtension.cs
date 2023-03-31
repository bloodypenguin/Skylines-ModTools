using ModTools.MainObject;

namespace ModTools {
    using ColossalFramework;
    using ICities;
    using ModTools.Explorer;
    using ModTools.GamePanels;
    using ModTools.Utils;
    using System;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class LoadingExtension : LoadingExtensionBase
    {
        public static bool Loaded { get; private set; }

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            TypeUtil.ClearTypeCache();
            ShaderUtil.ClearShaderCache();
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            Load();
        }

        public static void Load()
        {
            try {
                CustomPrefabs.Bootstrap();
                var appMode = Singleton<ToolManager>.instance.m_properties.m_mode;
                var modTools = MainWindow.Instance;
                if(modTools == null) {
                    Debug.LogError("ModTools instance wasn't present");
                    return;
                }

                if(modTools.Config.ExtendGamePanels && appMode == ItemClass.Availability.Game) {
                    modTools.gameObject.AddComponent<GamePanelExtension>();
                }

                SelectionTool.Create();

                Loaded = true;

                MainWindow.Instance.RegisterHotkeys();
            } catch(Exception ex) {
                Logger.Exception(ex);
            }
        }

        public override void OnLevelUnloading()
        {
            Loaded = false;
            var sceneExplorer = Object.FindObjectOfType<SceneExplorer>();

            sceneExplorer?.ClearExpanded();
            sceneExplorer?.ClearHistory();

            SelectionTool.Release();
        }

        public override void OnReleased()
        {
            base.OnReleased();
            TypeUtil.ClearTypeCache();
            ShaderUtil.ClearShaderCache();
            CustomPrefabs.Revert();
            TransferSearchResults.Revert();
        }
    }
}