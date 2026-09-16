using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Santa.MicroGames;
using Santa.MicroGames.Address;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.Tests.EditMode
{
    /// <summary>
    /// M2(住所)の本実装(`AddressMicroGame`)が `MicroGameBase` の契約(共通仕様 §4.3)を
    /// M1(`LetterMicroGame`)と同じ形で満たしていることを検証する。
    ///
    /// ★2026-09-10: 実素材(色分けされた `WorldMap.png`)の投入に伴い、当たり判定が
    /// 矩形の `Button` × 6 から `WorldMapHitTester` による色キー判定へ置き換わった。
    /// 契約テスト(§2)では `WorldMapHitTester.RegionTapped` を直接発火してタップを模擬する
    /// (実際のポインタ入力〜スクリーン座標変換は `RectTransformUtility` の標準実装に委ねており、
    /// ここでは検証していない。取りまとめ役への報告事項)。
    ///
    /// 色キー判定そのもの(`WorldMapRegionSampler` / `RegionLookupTable`)の検証は §3 を参照。
    ///
    /// ★このファイルのために `Santa.Tests.EditMode.asmdef` に `UnityEngine.UI` /
    /// `Unity.TextMeshPro` の参照を追加した(取りまとめ役への報告事項)。実際の
    /// Image / TMP_Text を組み立てて `MicroGameBase` を駆動する契約テストには、
    /// これらの型への直接参照が必要なため。
    /// </summary>
    public class AddressMicroGameTests
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MicroGames/MicroGame_Address.prefab";
        private const string WorldMapTexturePath = "Assets/Art/MicroGames/Address/WorldMap.png";

        // ★実際に WorldMap.png をサンプリングして確定した値(取りまとめ役への報告に記載)。
        //  プレハブ側(WorldMapHitTester)の設定と一致させてある。
        private static readonly (string regionId, Color32 fill, Color32 outline)[] RealRegionColors =
        {
            ("asia", new Color32(246, 202, 200, 255), new Color32(214, 86, 82, 255)),
            ("europe", new Color32(177, 196, 241, 255), new Color32(82, 120, 214, 255)),
            ("africa", new Color32(243, 214, 191, 255), new Color32(214, 141, 82, 255)),
            ("north_america", new Color32(188, 239, 209, 255), new Color32(82, 214, 136, 255)),
            ("south_america", new Color32(193, 246, 246, 255), new Color32(82, 214, 212, 255)),
            ("oceania", new Color32(236, 211, 246, 255), new Color32(177, 82, 214, 255)),
        };

        private const float RealMatchThreshold = 16f;
        private const int RealLookupWidth = 480;
        private const int RealLookupHeight = 270;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<Texture2D> _texturesToCleanup = new List<Texture2D>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
            _spawned.Clear();

            foreach (var tex in _texturesToCleanup)
            {
                if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
            }
            _texturesToCleanup.Clear();
        }

        // ------------------------------------------------------------------
        // 1. 出題データの制約(§5.3。実データに対する軽量な再確認。
        //    詳細な全項目検証は CountryDataTests.cs が担う。ここでは
        //    「AddressMicroGameが動作するための前提」に絞って確認する)。
        // ------------------------------------------------------------------

        [Test]
        public void RealCountryTable_HasNoDuplicateIds_AndCoversAllSixRegions()
        {
            var table = AssetDatabase.LoadAssetAtPath<CountryTable>("Assets/Settings/Questions/CountryTable.asset");
            if (table == null || table.Countries.Count == 0)
            {
                Assert.Ignore("CountryTable.asset が空です。先にインポートを実行してください。");
                return;
            }

            var ids = table.Countries.Select(c => c.id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "id が重複している国がある(重複した問題は出題プールの前提を壊す)");

            var regions = new HashSet<string>(table.Countries.Select(c => c.regionId));
            foreach (var region in Country.ValidRegionIds)
            {
                Assert.IsTrue(regions.Contains(region), $"'{region}' に該当する国が1つもない");
            }
        }

        // ------------------------------------------------------------------
        // 2. MicroGameBase の契約
        // ------------------------------------------------------------------

        [Test]
        public void Prepare_ShowsFirstCountry_AndDisablesInput_AndReflectsCarriedOverCounters()
        {
            var table = CreateTable(("jp", "日本", "Japan", "asia"), ("fr", "フランス", "France", "europe"));
            var (game, refs, regions) = CreateAddressMicroGame();

            var sessionStore = new Dictionary<string, IMicroGameSessionState>();
            var existingState = new AddressSessionState
            {
                Pool = new ShuffledPoolCursor<Country>(table.Countries, new System.Random(0)),
            };
            existingState.DeliveredPerRegion["asia"] = 5;
            sessionStore["address"] = existingState;

            var context = BuildContext(table, sessionStore);
            game.Prepare(context);

            Assert.IsFalse(string.IsNullOrEmpty(refs.CountryNameText.text), "Prepare 直後に国名が表示されているべき(業務提示中に読ませるため。共通仕様 §4.3)");
            Assert.IsFalse(refs.WorldMapHitTester.InputEnabled, "Begin() 前は当たり判定が無効なはず");

            var asiaRegion = regions.First(r => r.RegionId == "asia");
            Assert.AreEqual("5", asiaRegion.RegionCounterText.text,
                "セッション状態から引き継いだ累積数がRegionCounterTextに反映されているべき(§4.7)");

            var europeRegion = regions.First(r => r.RegionId == "europe");
            Assert.AreEqual("0", europeRegion.RegionCounterText.text, "未登場の地域は0のはず");
        }

        [Test]
        public void Begin_EnablesMapInput()
        {
            var table = CreateTable(("jp", "日本", "Japan", "asia"));
            var (game, refs, _) = CreateAddressMicroGame();
            game.Prepare(BuildContext(table, new Dictionary<string, IMicroGameSessionState>()));

            game.Begin();

            Assert.IsTrue(refs.WorldMapHitTester.InputEnabled, "Begin() 後は当たり判定が有効になるべき");
        }

        [Test]
        public void CorrectRegionTap_RaisesUnitClearedOnly_AndIncrementsCounter_AndAdvancesToNextCountry()
        {
            var table = CreateTable(("jp", "日本", "Japan", "asia"), ("fr", "フランス", "France", "europe"));
            var (game, refs, regions) = CreateAddressMicroGame();
            game.Prepare(BuildContext(table, new Dictionary<string, IMicroGameSessionState>()));
            game.Begin();

            int unitCleared = 0, missed = 0, allCleared = 0;
            game.OnUnitCleared += () => unitCleared++;
            game.OnMissed += () => missed++;
            game.OnAllUnitsCleared += () => allCleared++;

            string firstCountryText = refs.CountryNameText.text;
            string correctRegionId = ResolveRegionIdForCountryName(firstCountryText);
            var correctRegion = regions.First(r => r.RegionId == correctRegionId);

            TapRegion(refs, correctRegionId);

            Assert.AreEqual(1, unitCleared, "正解タップは OnUnitCleared を1回だけ発火するべき");
            Assert.AreEqual(0, missed, "正解タップで OnMissed が発火してはならない");
            Assert.AreEqual(0, allCleared, "1件目では OnAllUnitsCleared は発火しないはず");
            Assert.AreEqual("1", correctRegion.RegionCounterText.text, "正解した地域のカウンタが+1されるべき");
            Assert.AreNotEqual(firstCountryText, refs.CountryNameText.text, "正解後は次の荷物へ即座に切り替わるべき(待ち時間ゼロ)");
            Assert.IsTrue(refs.WorldMapHitTester.InputEnabled, "3件に達していないので、次の問へ進んだ後は再び押せるようになるべき");
        }

        [Test]
        public void WrongRegionTap_RaisesMissedOnly_AndDoesNotIncrementAnyCounter()
        {
            var table = CreateTable(("jp", "日本", "Japan", "asia"));
            var (game, refs, regions) = CreateAddressMicroGame();
            game.Prepare(BuildContext(table, new Dictionary<string, IMicroGameSessionState>()));
            game.Begin();

            int unitCleared = 0, missed = 0;
            game.OnUnitCleared += () => unitCleared++;
            game.OnMissed += () => missed++;

            TapRegion(refs, "europe"); // 正解は asia("jp")なので europe は誤答

            Assert.AreEqual(0, unitCleared, "誤答は OnUnitCleared を発火してはならない");
            Assert.AreEqual(1, missed, "誤答は OnMissed を1回発火するべき");
            foreach (var region in regions)
            {
                Assert.AreEqual("0", region.RegionCounterText.text, "誤答では累積数を増やしてはならない(§4.3)");
            }
        }

        [Test]
        public void ThreeCorrectTaps_RaiseUnitClearedThreeTimes_ThenAllUnitsCleared_InCorrectOrder()
        {
            // 6地域すべてから1か国ずつ用意し、常に「正解の地域」を選べるようにする。
            var table = CreateTable(
                ("jp", "日本", "Japan", "asia"),
                ("fr", "フランス", "France", "europe"),
                ("ke", "ケニア", "Kenya", "africa"));
            var (game, refs, _) = CreateAddressMicroGame();
            game.Prepare(BuildContext(table, new Dictionary<string, IMicroGameSessionState>(), new System.Random(7)));
            game.Begin();

            var eventOrder = new List<string>();
            game.OnUnitCleared += () => eventOrder.Add("cleared");
            game.OnAllUnitsCleared += () => eventOrder.Add("allCleared");

            for (int i = 0; i < 3; i++)
            {
                string currentCountryName = refs.CountryNameText.text;
                var regionId = ResolveRegionIdForCountryName(currentCountryName);
                TapRegion(refs, regionId);
            }

            Assert.AreEqual(new[] { "cleared", "cleared", "cleared", "allCleared" }, eventOrder,
                "OnAllUnitsCleared は3件目の OnUnitCleared の直後に発火するべき(共通仕様 §4.4)");

            var summary = game.GetSummary();
            Assert.AreEqual(3, summary.UnitsCleared);
            Assert.AreEqual(0, summary.UnitsMissed);
            Assert.AreEqual(3, summary.QuestionsAnswered);
        }

        [Test]
        public void Finish_PreventsFurtherEvents_AndCanBeCalledWithoutStateAlreadySet()
        {
            var table = CreateTable(("jp", "日本", "Japan", "asia"));
            var (game, refs, _) = CreateAddressMicroGame();
            game.Prepare(BuildContext(table, new Dictionary<string, IMicroGameSessionState>()));
            game.Begin();

            game.Finish(MicroGameFinishReason.TimeUp);

            Assert.IsFalse(refs.WorldMapHitTester.InputEnabled, "Finish() 後は当たり判定を無効化するべき");

            int unitCleared = 0, missed = 0, allCleared = 0;
            game.OnUnitCleared += () => unitCleared++;
            game.OnMissed += () => missed++;
            game.OnAllUnitsCleared += () => allCleared++;

            // ★Finish後に地図から直接タップされても(理論上ありえないが)イベントが漏れないことを確認する。
            TapRegion(refs, "asia");

            Assert.AreEqual(0, unitCleared + missed + allCleared, "Finish() 後はいかなるイベントも発火してはならない(共通仕様 §4.3)");

            // Finish の多重呼び出しでも例外を起こさない(共通側が誤って2回呼んでも安全)。
            Assert.DoesNotThrow(() => game.Finish(MicroGameFinishReason.TimeUp));

            var summary = game.GetSummary();
            Assert.AreEqual(0, summary.UnitsCleared);
            Assert.AreEqual(0, summary.UnitsMissed);
        }

        [Test]
        public void SetPaused_DisablesInput_AndResumeReenablesIt()
        {
            var table = CreateTable(("jp", "日本", "Japan", "asia"));
            var (game, refs, _) = CreateAddressMicroGame();
            game.Prepare(BuildContext(table, new Dictionary<string, IMicroGameSessionState>()));
            game.Begin();

            game.SetPaused(true);
            Assert.IsFalse(refs.WorldMapHitTester.InputEnabled, "ポーズ中は当たり判定を無効化するべき");

            game.SetPaused(false);
            Assert.IsTrue(refs.WorldMapHitTester.InputEnabled, "ポーズ解除後(Begin済み・未終了)は当たり判定を再度有効化するべき");
        }

        // ------------------------------------------------------------------
        // 3. セッションをまたぐ永続状態(§4.7)とプールの wrap-around(§8.7)
        // ------------------------------------------------------------------

        [Test]
        public void GetOrCreateState_ReturnsSameInstance_AcrossTwoAppearancesOfM2()
        {
            var table = CreateTable(("jp", "日本", "Japan", "asia"), ("fr", "フランス", "France", "europe"));
            var sessionStore = new Dictionary<string, IMicroGameSessionState>();

            var (gameA, refsA, _) = CreateAddressMicroGame();
            gameA.Prepare(BuildContext(table, sessionStore, new System.Random(1)));
            gameA.Begin();

            string clearedRegionId = ResolveRegionIdForCountryName(refsA.CountryNameText.text);
            TapRegion(refsA, clearedRegionId); // 1件正解

            // ★実際のゲームでは1回目のPrefabは破棄され、2回目のM2出現で新しいインスタンスが生成される。
            var (gameB, refsB, regionsB) = CreateAddressMicroGame();
            gameB.Prepare(BuildContext(table, sessionStore, new System.Random(2)));

            var clearedRegionB = regionsB.First(r => r.RegionId == clearedRegionId);
            Assert.AreEqual("1", clearedRegionB.RegionCounterText.text,
                "2回目のM2出現でも、1回目で貯まった累積数(GetOrCreateState経由)が引き継がれるべき(共通仕様 §4.7)");
        }

        [Test]
        public void Pool_WrapsAround_WhenExhausted_WithoutThrowing()
        {
            // プールが2件しかない状態で、正解に達しないよう誤答を繰り返してプールを何周も消費させる。
            var table = CreateTable(("jp", "日本", "Japan", "asia"), ("fr", "フランス", "France", "europe"));
            var (game, refs, _) = CreateAddressMicroGame();
            game.Prepare(BuildContext(table, new Dictionary<string, IMicroGameSessionState>(), new System.Random(5)));
            game.Begin();

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 20; i++) // プール(2件)を10周ぶん消費する
                {
                    TapRegion(refs, "oceania"); // asia/europe以外なので常に誤答になる
                }
            }, "プールが枯渇しても先頭に戻って出題が続くべき(共通仕様 §8.7)。例外で落ちてはならない");

            Assert.IsFalse(string.IsNullOrEmpty(refs.CountryNameText.text), "枯渇後も国名の表示が継続しているべき");
        }

        // ------------------------------------------------------------------
        // 4. 色キー判定そのもの(WorldMapRegionSampler / RegionLookupTable)。
        //    実際の WorldMap.png と、プレハブに設定した実際の色・閾値を使って検証する。
        // ------------------------------------------------------------------

        [Test]
        public void WorldMapRegionSampler_BuildFromRealAsset_FindsAllSixRegions_WithMeaningfulPixelCounts()
        {
            var table = BuildRealLookupTableOrIgnore();

            foreach (var (regionId, _, _) in RealRegionColors)
            {
                int cellCount = table.GetCellCount(regionId);
                Assert.Greater(cellCount, 300,
                    $"'{regionId}' の対応表セル数が少なすぎる(閾値がずれてこの地域が消えている可能性がある)。実測={cellCount}");
            }
        }

        [Test]
        public void WorldMapRegionSampler_OceanPoint_IsNoRegion()
        {
            var table = BuildRealLookupTableOrIgnore();

            // ★実際にサンプリングして確認済みの海(白)の座標(画像左上の余白部分)。
            bool hit = table.TryGetRegionId(new Vector2(0.0026f, 0.9954f), out var regionId);
            Assert.IsFalse(hit, $"海(白)の座標は「どこでもない」と判定されるべきだが、'{regionId}' と判定された");
        }

        [Test]
        public void WorldMapRegionSampler_OutlineColors_AreNoRegion_ForAllRegions()
        {
            // ★輪郭は塗りより濃い別の色(実際にWorldMap.pngからサンプリングした値)。
            //  実画像の1点を探す代わりに、実際に使う色・閾値の組み合わせで
            //  「輪郭の色そのもの」を判定させて無反応になることを確認する
            //  (巨大な実画像から輪郭ピクセル1点をピクセル単位で特定するテストは、
            //  間引き解像度とのエイリアシングで脆くなるため避けた。取りまとめ役への報告事項)。
            var entries = RealRegionColors.Select(r => new RegionColorEntry { regionId = r.regionId, fillColor = r.fill }).ToArray();

            foreach (var (regionId, _, outline) in RealRegionColors)
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, outline);
                texture.Apply();
                _texturesToCleanup.Add(texture);

                var table = WorldMapRegionSampler.Build(texture, entries, 1, 1, RealMatchThreshold);
                bool hit = table.TryGetRegionId(new Vector2(0.5f, 0.5f), out var hitRegionId);
                Assert.IsFalse(hit, $"'{regionId}' の輪郭色は「どこでもない」と判定されるべきだが、'{hitRegionId}' と判定された");
            }
        }

        [Test]
        public void WorldMapRegionSampler_Centroid_IsClassifiedAsItsOwnRegion()
        {
            var table = BuildRealLookupTableOrIgnore();

            foreach (var (regionId, _, _) in RealRegionColors)
            {
                Vector2 centroid = table.GetCentroidUV(regionId);
                bool hit = table.TryGetRegionId(centroid, out var hitRegionId);
                Assert.IsTrue(hit && hitRegionId == regionId,
                    $"'{regionId}' の重心({centroid})はその地域自身と判定されるべき(実測: hit={hit}, region={hitRegionId})。" +
                    "三日月形などでは重心が地域外に出ることがある(仕様書に明記の既知の懸念)が、" +
                    "実際の6地域はいずれも重心が内側に収まることを確認済み。");
            }
        }

        [Test]
        public void Prefab_HasWorldMapHitTester_WithSixRegionColorsAndSaneThreshold()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Assert.Ignore($"{PrefabPath} が見つかりません。");
                return;
            }

            var hitTester = prefab.GetComponentInChildren<WorldMapHitTester>(true);
            Assert.IsNotNull(hitTester, "MicroGame_Address.prefab に WorldMapHitTester が付いているべき");
            Assert.IsNotNull(hitTester.SourceTexture, "sourceTexture が未設定");
            Assert.AreEqual(6, hitTester.RegionColors?.Count ?? 0, "regionColors は常に6件のはず(設計原則2)");
            Assert.Greater(hitTester.MatchThreshold, 0f, "matchThreshold は正の値のはず");
            Assert.Greater(hitTester.LookupWidth, 0);
            Assert.Greater(hitTester.LookupHeight, 0);

            var refs = prefab.GetComponent<AddressMicroGameRefs>();
            Assert.IsNotNull(refs, "AddressMicroGameRefs が付いているべき");
            Assert.AreEqual(6, refs.Regions?.Length ?? 0, "regions は常に6件のはず(設計原則2)");
            foreach (var regionId in Country.ValidRegionIds)
            {
                Assert.IsTrue(refs.Regions.Any(r => r.RegionId == regionId), $"'{regionId}' に対応する RegionRefs が無い");
            }
        }

        private RegionLookupTable BuildRealLookupTableOrIgnore()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(WorldMapTexturePath);
            if (texture == null)
            {
                Assert.Ignore($"{WorldMapTexturePath} が見つかりません。");
                return null;
            }

            var entries = RealRegionColors.Select(r => new RegionColorEntry { regionId = r.regionId, fillColor = r.fill }).ToArray();
            return WorldMapRegionSampler.Build(texture, entries, RealLookupWidth, RealLookupHeight, RealMatchThreshold);
        }

        // ------------------------------------------------------------------
        // ヘルパー
        // ------------------------------------------------------------------

        private static void TapRegion(AddressMicroGameRefs refs, string regionId)
        {
            refs.WorldMapHitTester.RegionTapped?.Invoke(regionId, Vector2.zero);
        }

        private static string ResolveRegionIdForCountryName(string countryDisplayName)
        {
            // CreateTable で使う3か国分の name_ja → region_id の対応。テスト専用の小さな辞書。
            switch (countryDisplayName)
            {
                case "日本": return "asia";
                case "フランス": return "europe";
                case "ケニア": return "africa";
                default: throw new InvalidOperationException($"想定外の国名: {countryDisplayName}");
            }
        }

        private CountryTable CreateTable(params (string id, string ja, string en, string region)[] rows)
        {
            var table = ScriptableObject.CreateInstance<CountryTable>();
            var countries = rows.Select(r => new Country
            {
                id = r.id,
                nameJa = r.ja,
                nameEn = r.en,
                regionId = r.region,
                weight = 1f,
                reviewStatus = "approved",
                addedVersion = "1.0",
            }).ToList();
            SetPrivateField(table, "countries", countries);
            return table;
        }

        private MicroGameDefinition CreateDefinition(CountryTable table)
        {
            var def = ScriptableObject.CreateInstance<MicroGameDefinition>();
            SetPrivateField(def, "id", "address");
            SetPrivateField(def, "promptTextKey", "micro.address.prompt");
            SetPrivateField(def, "titleTextKey", "micro.address.title");
            SetPrivateField(def, "questionSource", table);
            SetPrivateField(def, "questionsPerUnit", 1);
            SetPrivateField(def, "enabled", true);
            return def;
        }

        private MicroGameContext BuildContext(
            CountryTable table,
            Dictionary<string, IMicroGameSessionState> sessionStore,
            System.Random rng = null)
        {
            var def = CreateDefinition(table);
            return new MicroGameContext(
                definition: def,
                requiredUnits: 3,
                playDuration: 12f,
                rng: rng ?? new System.Random(0),
                sessionPlayIndex: 0,
                sessionStateStore: sessionStore);
        }

        private (AddressMicroGame game, AddressMicroGameRefs refs, RegionRefs[] regions) CreateAddressMicroGame()
        {
            var root = new GameObject("AddressMicroGameUnderTest", typeof(RectTransform));
            _spawned.Add(root);

            var flagImage = CreateImageChild(root.transform, "FlagImage");
            var countryNameText = CreateTextChild(root.transform, "CountryNameText");

            var worldMapGo = new GameObject("WorldMap", typeof(RectTransform));
            worldMapGo.transform.SetParent(root.transform, false);
            var worldMapRect = worldMapGo.GetComponent<RectTransform>();
            worldMapRect.sizeDelta = new Vector2(1080f, 608f);

            var hitTester = worldMapGo.AddComponent<WorldMapHitTester>();
            SetPrivateField(hitTester, "mapRect", worldMapRect);
            // ★契約テスト(§2)では実テクスチャを使わず `RegionTapped` を直接発火するため、
            //  sourceTexture / regionColors は未設定のままでよい(Prepare側もnullガード済み)。

            var regionIds = Country.ValidRegionIds;
            var regions = new RegionRefs[regionIds.Length];
            for (int i = 0; i < regionIds.Length; i++)
            {
                regions[i] = CreateRegion(worldMapGo.transform, regionIds[i]);
            }

            var addressRefs = root.AddComponent<AddressMicroGameRefs>();
            SetPrivateField(addressRefs, "flagImage", flagImage);
            SetPrivateField(addressRefs, "countryNameText", countryNameText);
            SetPrivateField(addressRefs, "regions", regions);
            SetPrivateField(addressRefs, "worldMapHitTester", hitTester);

            var game = root.AddComponent<AddressMicroGame>();
            SetPrivateField(game, "refs", addressRefs);

            return (game, addressRefs, regions);
        }

        private RegionRefs CreateRegion(Transform parent, string regionId)
        {
            var go = new GameObject($"RegionMarker_{regionId}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var nameText = CreateTextChild(go.transform, "RegionNameText");
            var counterText = CreateTextChild(go.transform, "RegionCounterText");
            counterText.text = "0";

            var regionRefs = go.AddComponent<RegionRefs>();
            SetPrivateField(regionRefs, "regionId", regionId);
            SetPrivateField(regionRefs, "regionNameText", nameText);
            SetPrivateField(regionRefs, "regionCounterText", counterText);

            return regionRefs;
        }

        private static Image CreateImageChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<Image>();
        }

        private static TMP_Text CreateTextChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<TextMeshProUGUI>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }

            if (field == null)
            {
                throw new MissingFieldException($"フィールドが見つかりません: {target.GetType().Name}.{fieldName}");
            }

            field.SetValue(target, value);
        }
    }
}
