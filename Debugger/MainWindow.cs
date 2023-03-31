namespace ModTools
{
    using System;
    using System.Collections.Generic;
    using ColossalFramework;
    using ColossalFramework.UI;
    using ModTools.Console;
    using ModTools.Explorer;
    using ModTools.GamePanels;
    using ModTools.Scripting;
    using ModTools.UI;
    using UnifiedUI.Helpers;
    using UnityEngine;

    internal sealed class MainWindow : GUIWindow, IGameObject
    {
        private const string ConfigPath = "ModToolsConfig.xml";
        private static readonly object LoggingLock = new object();
        private static readonly object InstanceLock = new object();

        private static MainWindow instance;
        private static bool loggingInitialized;

        private ModalUI modalUI = new ModalUI();

        private CustomConsole console;
        private ScriptEditor scriptEditor;
        private AppearanceConfig appearanceConfig;
        private DebugRenderer debugRenderer;

        public MainWindow()
            : base("Mod Tools " + ModToolsMod.Version, new Rect(128, 128, 356, 340), resizable: false)
        {
            instance = this;
        }

        // TODO: remove the singleton
        public static MainWindow Instance => instance;

        // TODO: refactor the configuration access
        public ModConfiguration Config { get; set; } = new ModConfiguration();

        // TODO: refactor and remove this property
        internal SceneExplorer SceneExplorer { get; private set; }

        // TODO: refactor and remove this property
        internal ColorPicker ColorPicker { get; private set; }

        // TODO: refactor and remove this property
        internal Watches Watches { get; private set; }

        // TODO: refactor and move this functionality to a dedicated service
        public void SaveConfig()
        {
            if (Config == null)
            {
                return;
            }

            if (console != null)
            {
                Config.ConsoleRect = console.WindowRect;
            }

            Config.WatchesRect = Watches.WindowRect;
            Config.SceneExplorerRect = SceneExplorer.WindowRect;
            Config.Serialize(ConfigPath);
        }

        public void Initialize()
        {
            if (!loggingInitialized)
            {
                Application.logMessageReceivedThreaded += OnApplicationLogMessageReceivedThreaded;

                loggingInitialized = true;
            }

            SceneExplorer = gameObject.AddComponent<SceneExplorer>();
            Watches = gameObject.AddComponent<Watches>();
            ColorPicker = gameObject.AddComponent<ColorPicker>();
            scriptEditor = gameObject.AddComponent<ScriptEditor>();
            appearanceConfig = gameObject.AddComponent<AppearanceConfig>();

            LoadConfig();

            if (Config.UseModToolsConsole)
            {
                console = gameObject.AddComponent<CustomConsole>();
                Logger.SetCustomLogger(console);
            }
        }

        public void OnDestroy()
        {
            modalUI?.Release();
            modalUI = null;
        }

        public void Update() {
            var middleButtonState = MouseButtonState.None;
            if (Input.GetMouseButtonDown(2)) {
                middleButtonState = MouseButtonState.Pressed;
            } else if (Input.GetMouseButtonUp(2)) {
                middleButtonState = MouseButtonState.Released;
            } else if (Input.GetMouseButton(2)) {
                middleButtonState = MouseButtonState.Held;
            }

            bool mouseOverAWindow = IsMouseOverAWindow();
            modalUI.Update(mouseOverAWindow, middleButtonState);

            if (!LoadingExtension.Loaded || mouseOverAWindow || UIView.HasModalInput() /* workaround: when mouse is over a window UUI update is not expected. */) {
                HandleHotKeys();
            }
        }

        #region HotKeys
        private void HandleHotKeys() {
            if(FrameVisbleChanged == Time.frameCount) { 
                // prevent double trigger by UUI when mouse is at the place window will be visible.
                return;
            }
            if (SettingsUI.ConsoleKey.KeyActivated()) {
                ToggleConsole();
            } else if (SettingsUI.MainWindowKey.KeyActivated()) {
                ToggleMainWindow();
            } else if (SettingsUI.SceneExplorerKey.KeyActivated()) {
                ToggleSceneExplorer();
            } else if (SettingsUI.DebugRendererKey.KeyActivated()) {
                ToggleDebugRenderer();
            } else if (SettingsUI.WatchesKey.KeyActivated()) {
                ToggleWatches();
            } else if (SettingsUI.ScriptEditorKey.KeyActivated()) {
                ToggleScriptEditor();
            }
        }

        public void RegisterHotkeys() {
            UUIHelpers.RegisterHotkeys(activationKey: SettingsUI.ConsoleKey, onToggle: ToggleConsole);
            UUIHelpers.RegisterHotkeys(activationKey: SettingsUI.MainWindowKey, onToggle: ToggleMainWindow);
            UUIHelpers.RegisterHotkeys(activationKey: SettingsUI.SceneExplorerKey, onToggle: ToggleSceneExplorer);
            UUIHelpers.RegisterHotkeys(activationKey: SettingsUI.WatchesKey, onToggle: ToggleWatches);
            UUIHelpers.RegisterHotkeys(activationKey: SettingsUI.ScriptEditorKey, onToggle: ToggleScriptEditor);

            Dictionary<SavedInputKey, Func<bool>> inDebugRendererKeys = new Dictionary<SavedInputKey, Func<bool>>();
            inDebugRendererKeys[SettingsUI.IterateComponentKey] = DebugRendererIntoolHotkeysActive;
            inDebugRendererKeys[SettingsUI.ShowComponentKey] = DebugRendererIntoolHotkeysActive;
            UUIHelpers.RegisterHotkeys(
                activationKey: SettingsUI.DebugRendererKey,
                onToggle: ToggleDebugRenderer,
                activeKeys: inDebugRendererKeys);
        }

        private void ToggleConsole() {
            if (console) console.Visible = !console.Visible;
        }

        private void ToggleMainWindow() => Visible = !Visible;

        private void ToggleWatches() => Watches.Visible = !Watches.Visible;

        private void ToggleScriptEditor() => scriptEditor.Visible = !scriptEditor.Visible;

        private void ToggleSceneExplorer() {
            SceneExplorer.Visible = !SceneExplorer.Visible;
            if (SceneExplorer.Visible) {
                SceneExplorer.RefreshSceneRoots();
            }
        }

        private void ToggleDebugRenderer() {
            debugRenderer ??= FindObjectOfType<UIView>().gameObject.AddComponent<DebugRenderer>();
            debugRenderer.DrawDebugInfo = !debugRenderer.DrawDebugInfo;
        }

        private bool InDebugRenderer() => debugRenderer?.DrawDebugInfo ?? false;

        private bool DebugRendererIntoolHotkeysActive() => InDebugRenderer() && debugRenderer.IntoolkeysActrive;
        #endregion HotKeys

        protected override void OnWindowDestroyed()
        {
            Destroy(console);
            Destroy(SceneExplorer);
            Destroy(appearanceConfig);
            Destroy(scriptEditor);
            Destroy(Watches);
            Destroy(ColorPicker);
            Destroy(debugRenderer);
        }

        protected override void DrawWindow()
        {
            var newUseConsole = GUILayout.Toggle(Config.UseModToolsConsole, " Use ModTools console");

            if (newUseConsole != Config.UseModToolsConsole)
            {
                Config.UseModToolsConsole = newUseConsole;

                if (Config.UseModToolsConsole)
                {
                    console = gameObject.AddComponent<CustomConsole>();
                    Logger.SetCustomLogger(console);
                }
                else
                {
                    Destroy(console);
                    console = null;
                    Logger.SetCustomLogger(null);
                }

                SaveConfig();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Console log level");
            var newLogLevel = GUILayout.SelectionGrid(Config.LogLevel, new[] { "Log", "Warn", "Err", "None" }, 4);
            GUILayout.EndHorizontal();

            if (newLogLevel != Config.LogLevel)
            {
                Config.LogLevel = newLogLevel;
                SaveConfig();
            }

            var newLogExceptionsToConsole = GUILayout.Toggle(Config.LogExceptionsToConsole, " Log stack traces to console");
            if (newLogExceptionsToConsole != Config.LogExceptionsToConsole)
            {
                Config.LogExceptionsToConsole = newLogExceptionsToConsole;
                SaveConfig();
            }

            var newExtendGamePanels = GUILayout.Toggle(Config.ExtendGamePanels, " Game panel extensions");

            if (newExtendGamePanels != Config.ExtendGamePanels)
            {
                Config.ExtendGamePanels = newExtendGamePanels;
                SaveConfig();

                if (ToolManager.instance.m_properties.m_mode == ItemClass.Availability.Game)
                {
                    var gamePanelExtender = gameObject.GetComponent<GamePanelExtension>();
                    if (Config.ExtendGamePanels)
                    {
                        if (gamePanelExtender == null)
                        {
                            gameObject.AddComponent<GamePanelExtension>();
                        }
                    }
                    else if (gamePanelExtender != null)
                    {
                        Destroy(gamePanelExtender);
                    }
                }
            }

            if (debugRenderer == null)
            {
                debugRenderer = FindObjectOfType<UIView>().gameObject.AddComponent<DebugRenderer>();
            }

            debugRenderer.DrawDebugInfo = GUILayout.Toggle(
                debugRenderer.DrawDebugInfo, $" Debug Renderer ({SettingsUI.DebugRendererKey})");

            var customPrefabsObject = GUILayout.Toggle(Config.CustomPrefabsObject, " ModTools.CustomPrefabs Object");
            if (customPrefabsObject != Config.CustomPrefabsObject)
            {
                Config.CustomPrefabsObject = customPrefabsObject;
                if (Config.CustomPrefabsObject)
                {
                    CustomPrefabs.Bootstrap();
                }
                else
                {
                    CustomPrefabs.Revert();
                }

                SaveConfig();
            }

            var newSelectionTool = GUILayout.Toggle(
                Config.SelectionTool, $" Selection Tool ({SettingsUI.SelectionToolKey})");
            if (newSelectionTool != Config.SelectionTool)
            {
                if (!newSelectionTool)
                {
                    var tool = ToolsModifierControl.GetTool<SelectionTool>();
                    if (tool?.enabled == true)
                    {
                        ToolsModifierControl.SetTool<DefaultTool>();
                    }
                }

                Config.SelectionTool = newSelectionTool;
                SaveConfig();
            }

            GUILayout.Space(Config.TreeIdentSpacing);

            if (GUILayout.Button($"Debug console ({SettingsUI.ConsoleKey})"))
            {
                if (console != null)
                {
                    console.Visible = true;
                }
                else
                {
                    var debugOutputPanel = GameObject.Find("(Library) DebugOutputPanel").GetComponent<DebugOutputPanel>();
                    debugOutputPanel.enabled = true;
                    debugOutputPanel.GetComponent<UIPanel>().isVisible = true;
                }
            }

            if (GUILayout.Button($"Watches ({SettingsUI.WatchesKey})"))
            {
                Watches.Visible = !Watches.Visible;
            }

            if (GUILayout.Button($"Scene explorer ({SettingsUI.SceneExplorerKey})"))
            {
                SceneExplorer.Visible = !SceneExplorer.Visible;
                if (SceneExplorer.Visible)
                {
                    SceneExplorer.RefreshSceneRoots();
                }
            }

            if (GUILayout.Button($"Script editor ({SettingsUI.ScriptEditorKey})"))
            {
                scriptEditor.Visible = !scriptEditor.Visible;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Appearance settings"))
            {
                appearanceConfig.Visible = true;
                var windowRect = appearanceConfig.WindowRect;
                windowRect.position = WindowRect.position + new Vector2(32.0f, 32.0f);
                appearanceConfig.MoveResize(windowRect);
            }
        }

        private void OnApplicationLogMessageReceivedThreaded(string condition, string trace, LogType type)
        {
            lock (LoggingLock)
            {
                if (Config.LogLevel > 2)
                {
                    return;
                }

                if (type == LogType.Exception)
                {
                    var message = condition;
                    if (Config.LogExceptionsToConsole && trace != null)
                    {
                        message = $"{message}\n\n{trace}";
                    }

                    Logger.Error(message);
                }
                else if (type == LogType.Error || type == LogType.Assert)
                {
                    Logger.Error(condition);
                }
                else if (type == LogType.Warning && Config.LogLevel < 2)
                {
                    Logger.Warning(condition);
                }
                else if (Config.LogLevel == 0)
                {
                    Logger.Message(condition);
                }
            }
        }

        private void LoadConfig()
        {
            Config = ModConfiguration.Deserialize(ConfigPath);
            if (Config == null)
            {
                Config = new ModConfiguration();
                SaveConfig();
            }

            console?.MoveResize(Config.ConsoleRect);
            Watches.MoveResize(Config.WatchesRect);
            SceneExplorer.MoveResize(Config.SceneExplorerRect);
            scriptEditor.ReloadProjectWorkspace();
        }
    }
}