using System.Collections;
using System.Text;
using Santa.Core;
using Santa.Game;
using UnityEngine;

namespace Santa.UI
{
    /// <summary>
    /// `Screen_Result` の仮置き版ロジック。`IGameSessionService.LastResult` を読むだけ
    /// (`SessionEnded` は画面遷移の前に発火するため、購読では取りこぼす。共通仕様 §4.4)。
    /// 「もう一回」「やめる」のボタン自体は `StartSessionAndNavigateButton` / `ScreenNavButton` が持つ。
    /// </summary>
    public class ResultScreenController : ScreenControllerBase<ResultScreenRefs>
    {
        /// <summary>SE-17(11_Screen_Result.md §4.7): `se_rank` の約0.3秒後に鳴らす。</summary>
        private const float NewRecordSeDelaySeconds = 0.3f;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (!ServiceLocator.TryGet<IGameSessionService>(out var session) || session.LastResult == null)
            {
                Refs.SummaryText.text = "(結果がありません)";
                return;
            }

            var r = session.LastResult.Value;
            var sb = new StringBuilder();
            sb.AppendLine($"スコア: {r.Score}");
            sb.AppendLine($"ランク index: {r.RankIndex}");
            sb.AppendLine($"しょり: {r.ClearedUnits} けん");
            sb.AppendLine($"誤答: {r.MissedUnits} けん");
            sb.AppendLine($"最高コンボ: {r.MaxCombo}");
            sb.AppendLine(r.IsNewRecord ? "NEW RECORD!" : "");
            foreach (var kv in r.PerMicroGame)
            {
                sb.AppendLine($"  {kv.Key}: {kv.Value.cleared}/{kv.Value.cleared + kv.Value.missed}");
            }

            Refs.SummaryText.text = sb.ToString();

            // ★ランク章・NEW RECORD帯の見た目(RankBadgeImage等)はこの画面がまだ仮置きのため無い(Q-3)。
            //   音だけは仕様どおりに鳴らしておく(SE-16/SE-17。11_Screen_Result.md §4.7)。
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.Rank);
                if (r.IsNewRecord)
                {
                    StartCoroutine(PlayNewRecordSeDelayed());
                }
            }
        }

        private IEnumerator PlayNewRecordSeDelayed()
        {
            yield return new WaitForSecondsRealtime(NewRecordSeDelaySeconds);
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.NewRecord);
            }
        }
    }
}
