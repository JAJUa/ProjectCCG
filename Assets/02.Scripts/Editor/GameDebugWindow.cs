#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SpiritAge.Core;
using SpiritAge.Core.Enums;

namespace SpiritAge.Editor
{
    /// <summary>
    /// 게임 디버그 윈도우
    /// </summary>
    public class GameDebugWindow : EditorWindow
    {
        private Vector2 scrollPos;

        [MenuItem("SpiritAge/Game Debug Window")]
        public static void ShowWindow()
        {
            GetWindow<GameDebugWindow>("Spirit Age Debug");
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Game must be running to use debug tools", MessageType.Info);
                return;
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawGameControls();
            DrawPlayerStats();
            DrawUnitInfo();
            DrawDebugActions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawGameControls()
        {
            EditorGUILayout.LabelField("Game Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Start Game"))
            {
                GameFlowController.Instance.StartNewGame();
            }

            if (GUILayout.Button("End Game"))
            {
                GameFlowController.Instance.EndGame();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Pause"))
            {
                GameFlowController.Instance.PauseGame();
            }

            if (GUILayout.Button("Resume"))
            {
                GameFlowController.Instance.ResumeGame();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void DrawPlayerStats()
        {
            var backend = BackendGameManager.Instance;
            if (backend == null || backend.CurrentPlayerDeck == null) return;

            EditorGUILayout.LabelField("Player Stats", EditorStyles.boldLabel);

            var deck = backend.CurrentPlayerDeck;

            EditorGUILayout.LabelField($"Round: {backend.CurrentRound}");
            EditorGUILayout.LabelField($"Phase: {backend.CurrentPhase}");
            EditorGUILayout.LabelField($"Gold: {deck.gold}");
            EditorGUILayout.LabelField($"Health: {deck.health}");
            EditorGUILayout.LabelField($"Units Owned: {deck.ownedUnits.Count}");
            EditorGUILayout.LabelField($"Formation Size: {deck.formation.Count}");

            EditorGUILayout.Space();
        }

        private void DrawUnitInfo()
        {
            var backend = BackendGameManager.Instance;
            if (backend == null || backend.CurrentPlayerDeck == null) return;

            EditorGUILayout.LabelField("Formation", EditorStyles.boldLabel);

            foreach (var unit in backend.CurrentPlayerDeck.formation)
            {
                if (unit == null) continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{unit.Name} [{unit.EvolutionType}]");
                EditorGUILayout.LabelField($"ATK: {unit.Stats.Attack} | HP: {unit.Stats.Health}/{unit.Stats.MaxHealth} | SPD: {unit.Stats.Speed}");
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();
        }

        private void DrawDebugActions()
        {
            EditorGUILayout.LabelField("Debug Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Add 100 Gold"))
            {
                BackendGameManager.Instance.AddGold(100);
            }

            if (GUILayout.Button("Damage Player (10)"))
            {
                BackendGameManager.Instance.CurrentPlayerDeck.health -= 10;
            }

            if (GUILayout.Button("Force Win Battle"))
            {
                BattleManager.Instance.OnBattleEnd?.Invoke(BattleResult.Victory);
            }

            if (GUILayout.Button("Skip to Next Round"))
            {
                BackendGameManager.Instance.CurrentRound++;
                BackendGameManager.Instance.StartShopPhase();
            }

            if (GUILayout.Button("Refresh Shop"))
            {
                ShopManager.Instance.RefreshShop(true);
            }

            EditorGUILayout.Space();
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}
#endif