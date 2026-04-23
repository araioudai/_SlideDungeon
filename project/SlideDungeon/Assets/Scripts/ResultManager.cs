using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;

public class ResultManager : MonoBehaviour
{
    #region private変数

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
        Init();

        //フェード処理
        StartCoroutine(LiftFade());
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
        scoreText.text = "クリア　: " + clearTime.ToString("F2") + "\n\nスワイプ: " + slideCount;
        if (GameManager.Instance.GetGameClear()) //ゲームクリア時の初期化処理
        {
            //紙吹雪を表示
            confetti.SetActive(true);
            //ランクによったテキストを表示
            RankMeasurement();
            //リザルト状況に応じたパネルを表示
            gameOver.SetActive(false);
            gameClear.SetActive(true);
        }
        else //ゲームオーバー時の初期化処理
        {
            //テキストを表示しない
            rankText.text = ""; 
            wordsText.text = "";
            //紙吹雪を非表示
            confetti.SetActive(false);
            //リザルト状況に応じたパネルを表示
            gameClear.SetActive(false);
            gameOver.SetActive(true);
        }
        int stage = StageIndex.Instance.GetIndex();
        numberText.text = "Stage" + stage;
        if (OffLineRankingManager.Instance.IsHightScore(stage, clearTime) && GameManager.Instance.GetGameClear())
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

    #region 演出ロジック
    /// <summary>
    /// フェード処理
    /// </summary>
    /// <returns></returns>
    private IEnumerator LiftFade()
    {
        //フェード処理
        yield return fade.PlayFadeIn(data.MaskSpeed(MaskData.MaskType.IN));
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
