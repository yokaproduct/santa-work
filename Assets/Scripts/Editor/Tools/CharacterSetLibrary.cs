using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Santa.EditorTools
{
    /// <summary>
    /// フォントの文字カバレッジ検証・再検証に使う文字集合を提供する。継続的に使うツールなので
    /// Editor/Tools/ に置く(フォント差し替え時に何度でも再実行できるようにするため)。
    /// </summary>
    public static class CharacterSetLibrary
    {
        /// <summary>
        /// letters.csv の全文面 + 主要UI文言 + ひらがな/カタカナ/半角英数記号 を1つの文字列にまとめる。
        /// 実際にゲーム内で表示される文字なので、この結果がそのまま
        /// 「実データが表示できるか」の検証結果になる。
        /// </summary>
        public static string BuildRequiredCharacterSet()
        {
            var sb = new StringBuilder();

            for (char c = 'ぁ'; c <= 'ゖ'; c++) sb.Append(c);
            for (char c = 'ァ'; c <= 'ヺ'; c++) sb.Append(c);
            sb.Append("ー、。「」・！？…‥（）〜※★☆〇×✕✓　");

            for (char c = ' '; c <= '~'; c++) sb.Append(c);

            sb.Append("サンタのお仕事仮タップしてスタート置き画面モード選択びょうやめるもういちど");
            sb.Append("結果スコアランクしょりけん誤答最高コンボプレゼントつづける正誤どちらでも");
            sb.Append("てがみしわけじゅうしょとどけろおもさはかりつつみわけよめはかれ");
            sb.Append("仮実装タップで件成立完了");

            string csvPath = Path.Combine(Application.dataPath, "..", "docs", "data", "letters.csv");
            string csvText = File.Exists(csvPath) ? File.ReadAllText(csvPath, Encoding.UTF8) : "";
            sb.Append(csvText);

            return new string(sb.ToString().Where(c => !char.IsControl(c) || c == '\n').Distinct().ToArray());
        }

        /// <summary>
        /// ★正直な注記: これは学年別漢字配当表(教育漢字1026字)の非公式・記憶に基づく再構成であり、
        /// 文部科学省の公式リストと完全一致することは保証できない。正式な検証には、
        /// 権威あるソースからテキスト化した1026字リストを別途用意し、このクラスに読み込ませて
        /// 再検証することを推奨する。なお本フォントはDynamic SDFのため、このリストに無い教育漢字でも
        /// ZenKurenaido-Regular.ttf 自体にグリフがあれば実行時に自動で追加され、豆腐にはならない。
        /// </summary>
        public static string BuildBestEffortKyoikuKanjiSet()
        {
            const string grade1 = "一二三四五六七八九十百千万円年月日時分曜週火水木金土本人子女男" +
                                   "上下左右中大小学校先生名前山川林森田畑村町村花草木竹犬猫牛馬魚鳥虫" +
                                   "空雨風雪天気石土音力手足目耳口見出入立休思考";
            const string grade2 = "何雲園遠家歌画回会海絵外角楽活間丸岩顔汽記帰弓牛魚京強教近兄形計元言" +
                                   "原戸古午後語工公広交光考行高黄合谷国黒今才細作算止市矢姉思紙寺自時";
            const string grade3 = "遊予様洋葉陽羊薬役由油有旅両緑礼列練路和意育員飲院運泳駅央横屋温化荷界開階寒感漢館岸期起客宮急球去橋業曲局銀区苦具君係軽血決研県庫湖向幸港号根祭皿仕死使始指歯詩次事持式実写者主守取酒受州拾終習集住重宿所暑助昭消商章勝乗植深申真神身進世整昔全相送想息速族他打対待代第題炭短談";
            const string grade4 = "愛案以衣位囲胃印英栄塩億加果貨課芽賀改械害街各覚完官管観関願喜季紀旗器機議求泣救給挙漁共協鏡競極訓軍郡型径景芸欠結健験建憲固功好候航康告差菜最材昨札刷殺察参産散残士氏史司試児治辞失借種周祝順初松笑唱焼象照賞臣信成省清静席積折節説浅戦選然争倉巣束側続卒孫帯隊達単置仲貯兆腸低底停伝典徒努灯堂働特得毒熱念敗梅博飯飛費必票標不夫付府副粉兵別辺変便包法望牧末満未脈民無約勇要養浴利陸良料量輪類令冷例歴連老労録";
            const string grade5 = "圧易移因永営衛易益演応往桜恩仮価河過快解格確額刊幹慣眼基寄規技義逆久旧居許境均禁句群経潔件券険検限現減故個護効厚耕鉱構興講混査再財罪雑酸賛支志枝師資飼示似識質舎謝授修述術準序招証常情条状織職制性政勢精製税責績接設絶祖素総造像増則測属損態築貯張提程適統堂銅導徳独任燃能破犯判版比肥非備俵評貧布婦富武復複仏編弁保墓報豊防貿暴務夢迷綿輸余預容略留領";
            const string grade6 = "異遺域宇映延沿我灰拡革閣割株干巻看簡危机貴揮疑吸供胸郷勤筋系敬警劇激穴憲厳己呼誤后孝皇紅降鋼刻穀骨困砂座済裁策冊蚕至私姿視詞誌磁射捨尺若樹収宗就衆従縦縮熟純処署諸除将傷障城蒸針仁垂推寸盛聖誠宣専泉洗染銭善奏窓創装層操蔵臓存尊宅担探誕段暖値宙忠著庁頂潮賃痛敵展討党糖届乳認納脳派拝背肺俳班晩否批秘腹奮並閉陛片補暮宝訪亡忘棒枚幕密盟模訳郵優幼欲翌乱卵覧裏律臨朗論";
            return grade1 + grade2 + grade3 + grade4 + grade5 + grade6;
        }
    }
}
