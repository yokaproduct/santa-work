using Santa.Game;
using Santa.MicroGames;
using UnityEngine;

namespace Santa.Tests
{
    /// <summary>
    /// テスト用に最小構成の SO / GameObject を組み立てるヘルパー。
    /// アセットとしては保存しない(ScriptableObject.CreateInstance のみ)。
    /// </summary>
    public static class TestFactory
    {
        public static GameBalanceSettings CreateBalance(
            float promptDuration = 0.1f,
            float playDuration = 1.0f,
            float judgeEffectDuration = 0.05f,
            float countdownNormal = 0.05f,
            float countdownRetry = 0.05f,
            int requiredUnits = 3,
            int baseScore = 100,
            float minRemainingTimeToStartNewMicroGame = 0.2f,
            float finishSequenceDuration = 0.1f,
            float finishSaveAtTime = 0.02f,
            float finishBgmFadeSeconds = 0.01f)
        {
            var balance = ScriptableObject.CreateInstance<GameBalanceSettings>();
            ReflectionTestUtil.SetPrivateField(balance, "promptDuration", promptDuration);
            ReflectionTestUtil.SetPrivateField(balance, "playDuration", playDuration);
            ReflectionTestUtil.SetPrivateField(balance, "judgeEffectDuration", judgeEffectDuration);
            ReflectionTestUtil.SetPrivateField(balance, "countdownNormal", countdownNormal);
            ReflectionTestUtil.SetPrivateField(balance, "countdownRetry", countdownRetry);
            ReflectionTestUtil.SetPrivateField(balance, "requiredUnits", requiredUnits);
            ReflectionTestUtil.SetPrivateField(balance, "baseScore", baseScore);
            ReflectionTestUtil.SetPrivateField(balance, "minRemainingTimeToStartNewMicroGame", minRemainingTimeToStartNewMicroGame);
            ReflectionTestUtil.SetPrivateField(balance, "finishSequenceDuration", finishSequenceDuration);
            ReflectionTestUtil.SetPrivateField(balance, "finishSaveAtTime", finishSaveAtTime);
            ReflectionTestUtil.SetPrivateField(balance, "finishBgmFadeSeconds", finishBgmFadeSeconds);
            return balance;
        }

        public static GameModeDefinition CreateMode(float sessionDuration, bool recordHighScore = false, bool evaluateRank = false)
        {
            var mode = ScriptableObject.CreateInstance<GameModeDefinition>();
            ReflectionTestUtil.SetPrivateField(mode, "id", "test_mode");
            ReflectionTestUtil.SetPrivateField(mode, "sessionDuration", sessionDuration);
            ReflectionTestUtil.SetPrivateField(mode, "recordHighScore", recordHighScore);
            ReflectionTestUtil.SetPrivateField(mode, "evaluateRank", evaluateRank);
            return mode;
        }

        public static MicroGameDefinition CreateDefinition(string id, MicroGameBase prefab, int questionsPerUnit = 1)
        {
            var def = ScriptableObject.CreateInstance<MicroGameDefinition>();
            ReflectionTestUtil.SetPrivateField(def, "id", id);
            ReflectionTestUtil.SetPrivateField(def, "prefab", prefab);
            ReflectionTestUtil.SetPrivateField(def, "promptTextKey", $"micro.{id}.prompt");
            ReflectionTestUtil.SetPrivateField(def, "titleTextKey", $"micro.{id}.title");
            ReflectionTestUtil.SetPrivateField(def, "introCardTextKey", $"micro.{id}.introCard.body");
            ReflectionTestUtil.SetPrivateField(def, "questionsPerUnit", questionsPerUnit);
            ReflectionTestUtil.SetPrivateField(def, "enabled", true);
            return def;
        }

        public static MicroGameCatalog CreateCatalog(params MicroGameDefinition[] definitions)
        {
            var catalog = ScriptableObject.CreateInstance<MicroGameCatalog>();
            ReflectionTestUtil.SetPrivateField(catalog, "definitions",
                new System.Collections.Generic.List<MicroGameDefinition>(definitions));
            return catalog;
        }

        /// <summary>非アクティブなテンプレートオブジェクトとして DummyMicroGame を作る(Instantiate の元になる)。</summary>
        public static DummyMicroGame CreateDummyTemplate(string name = "~DummyMicroGameTemplate")
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            return go.AddComponent<DummyMicroGame>();
        }
    }
}
