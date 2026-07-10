using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using static UnityEngine.GraphicsBuffer;
using Unity.VisualScripting;

public class ResultManager : MonoBehaviour
{
    #region 列挙対
    enum ScoresText
    {
        SCORE,
        RANK,
        WORD,

        MAX
    }
    #endregion

    #region private変数

    [Header("ランキングUIセット")]
    [SerializeField] private GameObject rankingDisplayObj; //ランキング全体の親
    [SerializeField] private TMP_Text[] top3Texts;         //1位〜3位を表示するテキスト(3個)
    [SerializeField] private TMP_Text myBestRankText;      //「あなたの最高順位: 〇位」用
    [Header("スコア表示などで使うもの")]
    [SerializeField] private GameObject[] scores;          //スコア表示用
    [Header("ランキングランキング表示時に非表示にするもの")]
    [SerializeField] private GameObject[] scoresDelete;    //ランキング表示時に非表示にするもの
    [Header("ロード画面用パネル")]
    [SerializeField] private GameObject loadingPanel;      //ロード中に出すパネル
    [Header("スコアテキストセット")]
    [SerializeField] private Text scoreText;
    [Header("ランクテキストセット")]
    [SerializeField] private Text rankText;
    [Header("ランクワードテキストセット")]
    [SerializeField] private Text wordsText;
    [Header("クリアステージの番号テキストセット")]
    [SerializeField] private Text numberText;
    [Header("スコア表示用パネルセット")]
    [SerializeField] private GameObject scorePanel; //スコア表示用
    [Header("名前入力用パネルセット")]
    [SerializeField] private GameObject inputPanel; //ランキングに登録する名前入力
    [Header("紙吹雪オブジェクトをセット")]
    [SerializeField] private GameObject confetti;   //紙吹雪の表示非表示用
    [Header("ゲームクリア時表示ボタンセット")]
    [SerializeField] private GameObject gameClear;
    [Header("ゲームオーバー時表示ボタンセット")]
    [SerializeField] private GameObject gameOver;
    [Header("SE用オーディオソース／本体をセット")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioClip seDecision;
    [Header("ステージごとのノルマデータセット")]
    [SerializeField] private StageData[] stageDatas;
    [Header("マスクデータ")]
    [SerializeField] private MaskData data;
    [Header("マスクを置くキャンバスをセット")]
    [SerializeField] private GameObject canvasMask;

    private UIMaskFader fade;                       //フェード処理スクリプト

    private float clearTime;
    private int slideCount;
    private int stageNumber;
    private float baseTime;
    private float baseSlide;

    #endregion

    #region Unityイベント関数
    private void Awake()
    {
        //キャンバスの子としてプレハブを生成
        //data.panelMaskは画像の一番上の「Unmasked Panel」プレハブを指すと想定
        GameObject maskRoot = Instantiate(data.panelMask, canvasMask.transform);

        //描画順を一番手前に
        maskRoot.transform.SetAsLastSibling();

        fade = maskRoot.GetComponent<UIMaskFader>();
    }

    // Start is called before the first frame update
    void Start()
    {
        //UIの初期化
        rankingDisplayObj.SetActive(false);
        rankingDisplayObj.transform.localScale = Vector3.zero;
        gameClear.SetActive(false);
        gameOver.SetActive(false);

        //初期化
        Init();

        //演出開始
        if (GameManager.Instance.GetGameClear())
        {
            StartCoroutine(ShowResultSequence());
        }
        else
        {
            StartCoroutine(LiftFade()); //ゲームオーバー時は通常のフェードのみ
            gameOver.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #endregion

    #region Start呼び出し関数
    /// <summary>
    /// 初期化処理
    /// </summary>
    void Init()
    {
        clearTime = 0;
        clearTime = GameManager.Instance.GetClearTimer();
        slideCount = GameManager.Instance.GetSlideCount();
        //UIの初期状態を「隠す」設定にする
        rankingDisplayObj.SetActive(false);
        loadingPanel.SetActive(false);
        for (int i = 0; i < (int)ScoresText.MAX; i++)
        {
            scores[i].SetActive(false);
        }
        rankingDisplayObj.transform.localScale = Vector3.zero;

        //スコアテキストを空に
        scoreText.text = "";

        //スタンプテキストをあらかじめ透明にしておく
        Color rCol = rankText.color;
        rCol.a = 0;
        rankText.color = rCol;

        Color wCol = wordsText.color;
        wCol.a = 0;
        wordsText.color = wCol;

        scoreText.text = "Time : 0.00\n\nSwipe : 0";

        if (GameManager.Instance.GetGameClear()) //ゲームクリア時の初期化処理
        {
            //紙吹雪を表示
            confetti.SetActive(true);
            //ランクによったテキストを表示
            RankMeasurement();
        }
        else //ゲームオーバー時の初期化処理
        {
            //テキストを表示しない
            rankText.text = ""; 
            wordsText.text = "";
            //紙吹雪を非表示
            confetti.SetActive(false);
        }
        int stage = StageIndex.Instance.GetIndex();
        numberText.text = "Stage" + stage;
        if (!DebugMode.Instance.GetDebugMode() && OffLineRankingManager.Instance.IsHightScore(stage, clearTime) && GameManager.Instance.GetGameClear())
        {
            inputPanel.SetActive(true); //名前入力パネル表示
            //Time.timeScale = 0f;        //名前入力中は止める
        }
        else
        {
            scorePanel.SetActive(true);
        }
    }

    /// <summary>
    /// ランク判定と文字反映
    /// </summary>
    void RankMeasurement()
    {
        //現在のステージ番号を取得
        stageNumber = StageIndex.Instance.GetIndex();
        //該当ステージの基準データを取得
        StageData data = stageDatas[stageNumber - 1];
        //Sランク基準タイム
        baseTime = data.baseTime;
        //Sランク基準スライド数
        baseSlide = data.baseSlide;

        //スコア計算
        //各項目を基準値で正規化する：1.0が基準達成、1.0未満なら基準より良い
        float timeNorm = clearTime / baseTime;               //タイムの基準達成度
        float slideNorm = (float)slideCount / baseSlide;     //スライド数の基準達成度
        float score = timeNorm + slideNorm;                  //合計スコア（小さいほど高成績）2.0がピッタリ

        //ランク判定（floatで閾値を調整）
        //各ランクの閾値を超えないかで判定。値が小さいほど良いランクになる
        if (score <= 2.0f)
        {
            //Sランク：タイム・スライドともに基準値ピッタリか、それ以下でクリア
            rankText.color = new Color32(255, 196, 0, 255);
            wordsText.color = new Color32(255, 196, 0, 255);
            rankText.text = "S";  
            wordsText.text = "神話級スライム";
        }
        else if (score <= 2.2f)
        {
            //Aランク：基準値より少し多い場合
            rankText.color = new Color32(255, 57, 67, 255);
            wordsText.color = new Color32(255, 57, 67, 255);
            rankText.text = "A";   
            wordsText.text = "英雄級スライム";
        }
        else if (score <= 2.4f)
        {
            //Bランク : Aより少し多い場合
            rankText.color = new Color32(0, 72, 255, 255);
            wordsText.color = new Color32(0, 72, 255, 255);
            rankText.text = "B";  
            wordsText.text = "熟練スライム";
        }
        else if (score <= 2.6f)
        {
            //Cランク : Bより少し多い場合
            rankText.color = new Color32(0, 255, 40, 255);
            wordsText.color = new Color32(0, 255, 40, 255);
            rankText.text = "C";  
            wordsText.text = "新米スライム";
            //C Dランクは紙吹雪を非表示
            confetti.SetActive(false);
        }
        else
        {
            //Dランク：Cよりさらにスコアが大きい場合
            rankText.color = new Color32(203, 0, 255, 255);
            wordsText.color = new Color32(203, 0, 255, 255);
            rankText.text = "D";  
            wordsText.text = "スライムの卵";
            //C Dランクは紙吹雪を非表示
            confetti.SetActive(false);
        }
    }

    /// <summary>
    /// 演出ありの結果表示
    /// </summary>
    /// <returns></returns>
    private IEnumerator ShowResultSequence()
    {
        //フェードイン
        yield return fade.PlayFadeIn(data.MaskSpeed(MaskData.MaskType.IN));

        //裏でランキングデータの取得を開始
        bool isDataLoaded = false;
        List<ScoreEntry> rankingList = new List<ScoreEntry>();
        int onlineMyRank = -1;
        int stage = StageIndex.Instance.GetIndex();

        if (DebugMode.Instance.GetDebugMode())
        {
            //PlayerPrefsから保存されているオンライン用IDを取得
            string myId = PlayerPrefs.GetString("OnlineUserID", "");

            //【オンライン】通信開始
            OnLineRanking.Instance.GetResultRanking(stage, myId, (list, myRank) => {
                rankingList = list;
                onlineMyRank = myRank;
                isDataLoaded = true;
            });
        }
        else
        {
            //【オフライン】ローカルから取得
            rankingList = OffLineRankingManager.Instance.GetRanking(stage);
            isDataLoaded = true;
        }

        //スコアカウントアップ（0から始まる）
        yield return StartCoroutine(ScoreCountUpRoutine());
        yield return new WaitForSeconds(0.3f);

        //ランクと肩書きスタンプ
        yield return StartCoroutine(StampRoutine(rankText.transform, wordsText.transform));
        yield return new WaitForSeconds(0.2f);

        //演出が終わった時点でまだデータが届いていなければ、ここでロードパネルを表示
        if (!isDataLoaded)
        {
            loadingPanel.SetActive(true);
        }

        //ランキング表示
        yield return new WaitUntil(() => isDataLoaded);

        //ロードが完了したら非表示
        loadingPanel.SetActive(false);

        SetRankingUI(rankingList, onlineMyRank);

        rankingDisplayObj.SetActive(true);
        rankingDisplayObj.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
    }

    #region 各演出ルーチン

    /// <summary>
    /// フェード処理
    /// </summary>
    /// <returns></returns>
    private IEnumerator LiftFade()
    {
        //フェード処理
        yield return fade.PlayFadeIn(data.MaskSpeed(MaskData.MaskType.IN));
    }

    /// <summary>
    /// スコア表示処理
    /// </summary>
    /// <returns></returns>
    private IEnumerator ScoreCountUpRoutine()
    {
        float dTime = 0;
        int dSwipe = 0;
        bool isDone = false;

        scores[(int)ScoresText.SCORE].SetActive(true);

        //開始前にリセット
        scoreText.text = "Time : 0.00\n\nSwipe : 0";

        DOTween.To(() => 0f, x => dTime = x, clearTime, 1.0f).SetEase(Ease.OutQuad);
        DOTween.To(() => 0, x => dSwipe = x, slideCount, 1.0f).SetEase(Ease.OutQuad)
            .OnUpdate(() => {
                scoreText.text = $"Time : {dTime:F2}\n\nSwipe : {dSwipe}";
            })
            .OnComplete(() => isDone = true);

        yield return new WaitUntil(() => isDone);
        
        //完了時に少し強調
        scoreText.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f);
    }

    /// <summary>
    /// スタンプでランクや肩書の表示
    /// </summary>
    /// <param name="rank">ランクテキスト</param>
    /// <param name="title">肩書テキスト</param>
    /// <returns></returns>
    private IEnumerator StampRoutine(Transform rank, Transform title)
    {
        Transform[] target = { rank, title };
        for (int i = 0; i < 2; i++)
        {
            Text t = target[i].GetComponent<Text>();

            scores[i + 1].SetActive(true);

            //スタンプ前の準備：大きくして透明度は維持
            target[i].localScale = Vector3.one * 3f;

            //振り下ろす瞬間に透明度を1にする
            t.DOFade(1f, 0.1f);

            yield return target[i].DOScale(Vector3.one, 0.2f).SetEase(Ease.InBack).WaitForCompletion();

            //着地衝撃
            target[i].DOPunchScale(Vector3.one * 0.2f, 0.3f);
        }
        //seSource.PlayOneShot(seStamp);
    }

    private void SetRankingUI(List<ScoreEntry> list, int onlineMyRank)
    {
        foreach (GameObject sc in scoresDelete)
        {
            sc.SetActive(false);
        }

        if (GameManager.Instance.GetGameClear())
        {
            gameClear.SetActive(true);
        }
        else
        {
            gameOver.SetActive(true);
        }

        //Top3の表示
        for (int i = 0; i < top3Texts.Length; i++)
        {
            if (i < list.Count)
                top3Texts[i].text = $"{i + 1}位: {list[i].playerName} ({list[i].clearTime:F2}s)";
            else
                top3Texts[i].text = $"{i + 1}位: ---";
        }

        int myRank;

        if (DebugMode.Instance.GetDebugMode())
        {
            //オンライン時は、GASが全データから割り出してくれた順位をそのまま適用
            myRank = onlineMyRank;
        }
        else
        {
            //ローカルのリストから名前が一致する要素を探す
            myRank = list.FindIndex(x => x.playerName == InputManager.playerName) + 1;
        }

        //順位の表示
        if (myRank > 0)
        { 
            myBestRankText.text = $"YourRank: {myRank}"; 
        }
        else
        {
            myBestRankText.text = $"YourRank: -";
        }
    }

    #endregion

    #endregion

    #region 次のステージへ
    public void PushNext()
    {
        seSource.PlayOneShot(seDecision);
        StageIndex.Instance.SetNextIndex(1);
        // 1. フェードアウト（画面を閉じる）を開始
        // 2. 第二引数のラムダ式は、アニメーション終了後に実行される
        StartCoroutine(fade.PlayFadeOut(data.MaskSpeed(MaskData.MaskType.OUT), () =>
        {
            //画面が閉じきったタイミングでシーン遷移を開始
            StartCoroutine(GameSceneLoad());
        }));
    }
    #endregion

    #region ゲームリスタート

    public void GameReStart()
    {
        seSource.PlayOneShot(seDecision);

        StartCoroutine(fade.PlayFadeOut(data.MaskSpeed(MaskData.MaskType.OUT), () =>
        {
            //画面が閉じきったタイミングでシーン遷移を開始
            StartCoroutine(GameSceneLoad());
        }));
    }

    #endregion

    #region タイトルへが押された

    public void PushTitle()
    {
        seSource.PlayOneShot(seDecision);
        StageIndex.Instance.SetIndex(0);

        StartCoroutine(fade.PlayFadeOut(data.MaskSpeed(MaskData.MaskType.OUT), () =>
        {
            //画面が閉じきったタイミングでシーン遷移を開始
            StartCoroutine(TitleSceneLoad());
        }));
    }

    #endregion

    #region 名前が決定されたら

    public void NameEnter()
    {
        //ランキング登録処理
        int stage = StageIndex.Instance.GetIndex();
        OffLineRankingManager.Instance.AddScore(stage, InputManager.playerName, clearTime);

        //入力パネルを閉じて時間を再開
        inputPanel.SetActive(false);
        Time.timeScale = 1f;

        seSource.PlayOneShot(seDecision);
        scorePanel.SetActive(true);
    }

    #endregion

    #region ゲームシーンロード遅延用
    IEnumerator GameSceneLoad()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("GameScene");
    }
    #endregion

    #region タイトルシーンロード遅延用
    IEnumerator TitleSceneLoad()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("TitleScene");
    }
    #endregion
}
