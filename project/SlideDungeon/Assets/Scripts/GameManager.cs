using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    #region シングルトン（他のスクリプトからInstanceでアクセスできるようにする）
    public static GameManager Instance { get; private set; }
    #endregion

    #region 列挙対

    public enum Modes
    {
        GAME,   //ゲームモード
        CAMERA, //カメラモード

        MAX
    }

    #endregion

    #region SEの定数

    private const int DECISION = 1;
    private const int CANCEL = 2;
    private const int GAMECLEAR = 3;
    private const int GAMEOVER = 4;

    #endregion

    #region private変数

    [Header("スワイプ数表示用テキストセット")]
    [SerializeField] private Text swipeText;                  //スワイプ数表示用
    [Header("時間表示用テキストをセット")]
    [SerializeField] private Text timeText;                   //時間表示用
    [Header("カウント用テキストをセット")]
    [SerializeField] private Text countdownText;              //カウントテキスト
    [Header("ゲームパネルをセット")]
    [SerializeField] private GameObject gamePanel;            //ゲームパネル表示・非表示用
    [Header("ポーズパネルをセット")]
    [SerializeField] private GameObject pausePanel;           //ポーズパネル表示・非表示用
    [Header("ゲームクリア時に使用するものをセット")]
    [SerializeField] private GameObject[] gameClearObj;       //ゲームクリア時、表示・非表示用
    [Header("ゲームオーバーパネルをセット")]
    [SerializeField] private GameObject gameOverPanel;        //ゲームオーバーパネル表示・非表示用
    [Header("モード切り替え用ボタン")]
    [SerializeField] private GameObject modeChangeButton;     //モード切り替えが必要じゃないステージでは非表示
    [Header("カメラモード表示テキスト")]
    [SerializeField] private GameObject cameraModeText;       //カメラモード時、表示用
    [Header("マスクデータ")]
    [SerializeField] private MaskData data;
    [Header("マスクを置くキャンバスをセット")]
    [SerializeField] private GameObject canvasMask;           //マスク用キャンバス

    private UIMaskFader fade;                                 //フェード処理スクリプト

    private Modes currentMode;                                //現在のモード

    private bool isStart;                                     //ゲームがスタートしているかどうか
    private bool hitCheckFirst;                               //ポータル1とプレイヤーの当たり判定
    private bool hitCheckSecond;                              //ポータル2とプレイヤーの当たり判定
    //private bool playerMove;                                  //プレイヤーが動いているか
    private bool playerSwiped;                                //プレイヤーが新しくスワイプしたか
    private bool gameClear;                                   //ゲームクリアしたかどうか
    private bool gameOver;                                    //ゲームオーバーしたかどうか
    private bool isPause;                                     //現在ポーズ中かどうか
    private bool sePlay;                                      //seの再生一度だけ
    private bool isFollow;                                    //プレイヤーを追従する必要があるかどうか
    private int slideCount;                                   //スワイプ数をカウント用
    private float timer;                                      //クリア時間計測用
    private float clearTimeResult;                            //クリア時のタイムを保存

    private Vector2 initialCountdownPos;                      //カウントダウン用テキストの元の位置を保存する変数

    #endregion

    #region Set関数
    /// <summary>
    /// ポータル（どちらか）とプレイヤーが当ったフラグセット関数
    /// </summary>
    /// <param name="hitFirst">最初に当った</param>
    /// <param name="hitSecond">次に当った</param>
    public void SetHitCheck(bool hitFirst, bool hitSecond) { hitCheckFirst = hitFirst; hitCheckSecond = hitSecond; }
    
    /// <summary>
    /// プレイヤーが移動しているかのフラグセット関数
    /// </summary>
    /// <param name="move">移動状態</param>
    //public void SetPlayerMove(bool move) { playerMove = move; }
    
    /// <summary>
    /// プレイヤーがワープした後にスワイプしたかどうかのセット関数
    /// </summary>
    /// <param name="swiped">ワープ後にスワイプした</param>
    public void SetPlayerSwiped(bool swiped) { playerSwiped = swiped; }
    
    /// <summary>
    /// ゲームクリアした時、ゲームオーバーした時フラグセット用
    /// </summary>
    /// <param name="clear">ゲームクリア</param>
    /// <param name="over">ゲームオーバ</param>
    public void SetGameClear(bool clear) { gameClear = clear; }
    public void SetGameOver(bool over) {  gameOver = over; }

    /// <summary>
    /// スワイプ数カウント
    /// </summary>
    /// <param name="value">スワイプ数</param>
    public void SlideCount(int value) { slideCount += value; swipeText.text = "スワイプ:" + slideCount.ToString("D2"); }

    /// <summary>
    /// ゲームモードをセット
    /// </summary>
    /// <param name="value">現在のゲームモード</param>
    public void SetGameMode(Modes value) { currentMode = value; }

    /// <summary>
    /// プレイが開始しているかどうかをセット
    /// </summary>
    /// <param name="start">現在のプレイ開始状態</param>
    public void SetIsStart(bool start) {  isStart = start; }

    #endregion

    #region Get関数
    /// <summary>
    /// ゲームがスタートしているか
    /// </summary>
    /// <returns>スタートしている状態</returns>
    public bool GetGameStart() { return isStart; }

    /// <summary>
    /// ポータルとプレイヤーどっちと当たったかフラグゲット関数
    /// </summary>
    /// <returns>ポータルとプレイヤーどっちと当たったか</returns>
    public bool GetHitCheckFirst() {  return hitCheckFirst; }
    public bool GetHitCheckSecond() {  return hitCheckSecond; }
    
    /// <summary>
    /// プレイヤーがワープした後にスワイプしたかどうかのゲット関数
    /// </summary>
    /// <returns>ワープしたか</returns>
    public bool GetPlayerSwiped() { return playerSwiped; }
    
    /// <summary>
    /// ゲームクリアしているかのフラグゲット関数
    /// </summary>
    /// <returns>ゲームクリア状態</returns>
    public bool GetGameClear() { return gameClear; }

    /// <summary>
    /// ゲームオーバーしているかのフラグゲット関数
    /// </summary>
    /// <returns>ゲームオーバー状態</returns>
    public bool GetGameOver() {  return gameOver; }
    
    /// <summary>
    /// 現在ポーズ中かどうかのフラグゲット関数
    /// </summary>
    /// <returns>現在ポーズ中</returns>
    public bool GetPause() { return isPause; }
    
    /// <summary>
    /// 追従必要があるかどうかのフラグゲット関数
    /// </summary>
    /// <returns>現在追従する必要があるかどうか</returns>
    public bool GetIsFollow() { return isFollow; }

    /// <summary>
    /// クリア時のタイム取得
    /// </summary>
    /// <returns>ゲームクリア時のタイム</returns>
    public float GetClearTimer() { if (gameClear) { return clearTimeResult; } return 0; }

    /// <summary>
    /// クリア時のスライド数取得
    /// </summary>
    /// <returns>ゲームクリア時のスライド数</returns>
    public int GetSlideCount() { if (gameClear) { return slideCount; } return 0; }

    /// <summary>
    /// 現在のゲームモード取得
    /// </summary>
    /// <returns>現在のゲームモード</returns>
    public Modes GetGameMode() { return currentMode; }

    /// <summary>
    /// 現在の開始状態取得
    /// </summary>
    /// <returns>現在の開始状態</returns>
    public bool GetIsStart() { return isStart; }
    #endregion

    #region Unityイベント関数

    void Awake()
    {
        //シングルトン管理
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); //既にInstanceがあれば自分を破棄
            return;
        }
        Instance = this;

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

        MaskFade();
    }

    
    // Update is called once per frame
    void Update()
    {
        ClearTimeCount();
        DrawTimer();
        //FirstTutorial();
        GameClear();
        GameOver();
    }
    #endregion

    #region Start呼び出し関数

    void Init()
    {
        Time.timeScale = 1f;
        isStart = false;
        isPause = false;
        gameClear = false;
        gameOver = false;
        sePlay = false;
        playerSwiped = false;
        currentMode = Modes.GAME;
        slideCount = 0;
        timer = 0f;
        clearTimeResult = 0f;
        gamePanel.SetActive(true);
        gameOverPanel.SetActive(false);
        pausePanel.SetActive(false);
        swipeText.text = "スワイプ:" + slideCount.ToString("D2");

        //最初に現在の位置（インスペクターで設定した中央など）を覚えておく
        if (countdownText != null)
        {
            initialCountdownPos = countdownText.rectTransform.anchoredPosition;
        }

        foreach (GameObject obj in gameClearObj)
        {
            obj.SetActive(false);
        }

        SliderOff();
    }

    #region 視野移動用の表示・非表示用
    void SliderOff()
    {
        int index = StageIndex.Instance.GetIndex();
        switch (index){
            case 1: case 2: case 3:
                isFollow = false;
                modeChangeButton.SetActive(false);
                break;
            default:
                isFollow = true;
                modeChangeButton.SetActive(true);
                break;
        }
    }
    #endregion

    #region Maskのフェード処理とゲームスタートカウントダウン

    void MaskFade()
    {
        StartCoroutine(LiftFade());
    }

    /// <summary>
    /// フェード処理が終わったら、カウントダウン
    /// </summary>
    /// <returns></returns>
    private IEnumerator LiftFade()
    {
        //広がるアニメーション
        yield return fade.PlayFadeIn(data.MaskSpeed(MaskData.MaskType.IN));

        //チュートリアル実行する必要あるなら実行
        if (StageIndex.Instance.GetFirst()) 
        { 
            yield return TutorialManager.Instance.TutorialStart();
            
            //フェードアウト
            yield return fade.PlayFadeOut(data.MaskSpeed(MaskData.MaskType.OUT));

            //広がるアニメーション
            yield return fade.PlayFadeIn(data.MaskSpeed(MaskData.MaskType.IN));
        }

        //カウントダウン開始
        yield return StartCoroutine(StartCountdown());
    }


    /// <summary>
    /// カウントダウン処理
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartCountdown()
    {
        countdownText.gameObject.SetActive(true);

        // 3, 2, 1 のループ
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            PlayCountdownAnimation();
            yield return new WaitForSeconds(1.0f);
        }

        //最後は数字ではなく「GO!」や「START!」
        countdownText.text = "START!";
        countdownText.fontSize = 200;
        PlayCountdownAnimation();

        //ゲーム開始
        isStart = true;
        yield return new WaitForSeconds(0.75f);

        countdownText.gameObject.SetActive(false);
    }

    /// <summary>
    /// カウントダウンのアニメーション
    /// </summary>
    private void PlayCountdownAnimation()
    {
        //位置とスケール、透明度を「最初」の状態にリセット
        countdownText.rectTransform.anchoredPosition = initialCountdownPos;
        countdownText.transform.localScale = Vector3.zero;
        countdownText.color = new Color(countdownText.color.r, countdownText.color.g, countdownText.color.b, 1);

        //ポンッと出るアニメーション
        countdownText.transform.DOScale(1.0f, 0.5f).SetEase(Ease.OutBack);

        //UI専用の移動命令「DOAnchorPosY」を使う！
        // initialCountdownPos.y（元の高さ）から +50 くらい上に浮かせる
        countdownText.rectTransform.DOAnchorPosY(initialCountdownPos.y + 50f, 0.8f);

        //じわじわ消える
        countdownText.DOFade(0, 0.5f).SetDelay(0.5f);
    }
    #endregion

    #endregion

    #region Update呼び出し関数

    #region クリア時間カウント
    /// <summary>
    /// クリア時間カウント
    /// </summary>
    void ClearTimeCount()
    {
        if (!isStart || gameClear || gameOver) { return; }

        switch (currentMode)
        {
            case Modes.GAME:
                timer += Time.deltaTime;
                
                break;
            case Modes.CAMERA:
                timer += Time.deltaTime / 2;

                break;
        }
    }

    #endregion

    #region スコア用の時間表示
    /// <summary>
    /// 時間表示
    /// </summary>
    void DrawTimer()
    {
        timeText.text = "時間: " + timer.ToString("F2");
    }

    #endregion

    #region チュートリアル処理

    void FirstTutorial()
    {
        if (!StageIndex.Instance.GetFirst()) { return; }


    }

    #endregion

    #region ゲームモード切り替え

    public void PushModeChange()
    {
        if(currentMode == Modes.GAME)
        {
            cameraModeText.SetActive(true);
            currentMode = Modes.CAMERA;
        }
        else if (currentMode == Modes.CAMERA)
        {
            cameraModeText.SetActive(false);
            currentMode = Modes.GAME;
        }
    }

    #endregion

    #region ポーズパネルの表示・非表示
    /// <summary>
    /// ポーズ画面を開く
    /// </summary>
    public void DrawPause()
    {
        //動作停止処理
        Time.timeScale = 0f;
        isPause = true;
        pausePanel.SetActive(true);
        SoundManager.Instance.SePlay(DECISION);
    }

    /// <summary>
    /// ポーズ画面を閉じる
    /// </summary>
    public void ClosePause()
    {
        //動作開始処理
        Time.timeScale = 1f;
        isPause = false;
        pausePanel.SetActive(false);
        SoundManager.Instance.SePlay(CANCEL);
    }

    #endregion

    #region ゲームクリア／オーバー時の処理
    /// <summary>
    /// ゲームクリア
    /// </summary>
    void GameClear()
    {
        if (gameClear && !sePlay) //一度だけ実行
        {
            sePlay = true;                   //最初にフラグを立てる
            SoundManager.Instance.BgmStop(); //BGMを止める
            clearTimeResult = timer;

            //UI表示（アニメーション付き）を一回だけ呼ぶ
            StartCoroutine(PlayClearProduction());

            clearTimeResult = timer; //クリア時に一度だけクリアタイム代入
            if (DebugMode.Instance.GetDebugMode())
            {
                RankingManager.Instance.SetTime(clearTimeResult); //ランク集計用にクリアタイムをセット
            }
            else
            {
                //リザルトシーンへ遷移
                DrawGameStatus("GameClear");
            }
            SoundManager.Instance.SePlay(GAMECLEAR); //ゲームクリア用サウンドが再生されてなければ再生
        }
    }

    /// <summary>
    /// クリア時のオブジェクト表示用コルーチン
    /// </summary>
    /// <returns></returns>
    private IEnumerator PlayClearProduction()
    {
        foreach (GameObject obj in gameClearObj)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                //アニメーションが終わったら消す（一回だけ予約）
                float duration = obj.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
                Destroy(obj, duration);
            }
        }
        yield return null;
    }

    /// <summary>
    /// ゲームオーバー
    /// </summary>
    void GameOver()
    {
        //ゲームオーバー処理
        if (gameOver)
        {
            foreach (GameObject obj in gameClearObj)
            {
                obj.SetActive(false);
            }

            SoundManager.Instance.BgmStop(); //BGMを止める

            if (!sePlay)
            {
                //リザルトシーンへ遷移
                DrawGameStatus("GameOver");

                SoundManager.Instance.SePlay(GAMEOVER); //ゲームオーバー用サウンドが再生されてなければ再生
                sePlay = true;                          //一度だけ再生用
            }
        }
    }

    #endregion

    /// <summary>
    /// ゲームステータス表示
    /// </summary>
    private void DrawGameStatus(string status)
    {
        Time.timeScale = 1f;

        //statusの中身を見て、どのパネルを出すか決める
        if (status == "GameClear")
        {
            gameOverPanel.SetActive(false); //ゲームオーバーパネルを非表示
        }
        else
        {
            gameOverPanel.SetActive(true);  //ゲームオーバーパネルを表示
        }

        countdownText.gameObject.SetActive(true);

        //演出開始前に位置・スケール・色をリセット
        countdownText.rectTransform.anchoredPosition = initialCountdownPos;
        countdownText.transform.localScale = Vector3.zero;
        countdownText.color = new Color(countdownText.color.r, countdownText.color.g, countdownText.color.b, 1);

        //はみ出し・改行の設定をコードで強制
        countdownText.horizontalOverflow = HorizontalWrapMode.Overflow; //横にはみ出してもOK
        countdownText.verticalOverflow = VerticalWrapMode.Overflow;     //縦にはみ出してもOK
        countdownText.alignment = TextAnchor.MiddleCenter;              //中央揃え

        countdownText.text = status;
        countdownText.lineSpacing = 0.8f; //行間を少し詰める
        countdownText.fontSize = 175;     //文字の大きさを変える

        countdownText.color = gameClear ? Color.yellow : Color.red;

        //再生中のアニメーションがあれば止める
        countdownText.transform.DOKill();
        countdownText.rectTransform.DOKill();

        Sequence overSeq = DOTween.Sequence();

        //スケールを1.2倍程度に抑える、パンチを効かせる
        overSeq.Append(countdownText.transform.DOScale(1.2f, 0.4f).SetEase(Ease.OutBack))
               //激しい揺れ（DOShakeAnchorPos）
               .Join(countdownText.rectTransform.DOShakeAnchorPos(1.0f, 40f, 40))
               .AppendInterval(1.5f);

        //テキストを表示してからフェードアウト（画面を閉じる）を開始
        overSeq.OnComplete(() =>
        {
            if (fade != null)
            {
                // 1. フェードアウト（画面を閉じる）を開始
                // 2. 第二引数のラムダ式は、アニメーション終了後に実行される
                StartCoroutine(fade.PlayFadeOut(data.MaskSpeed(MaskData.MaskType.OUT), () =>
                {
                    //画面が閉じきったタイミングでシーン遷移を開始
                    StartCoroutine(ResultLoad());
                }));
            }
        });
    }

    #region ゲームリスタート
    /// <summary>
    /// ゲームリスタート
    /// </summary>
    public void GameReStart()
    {
        SoundManager.Instance.SePlay(DECISION);
        if (Time.timeScale == 0f) { Time.timeScale = 1f; }
        StartCoroutine(GameSceneLoad());
    }

    #endregion

    #region 少ししたらリザルトシーンへ
    /// <summary>
    /// 少し時間を空けてからリザルト
    /// </summary>
    IEnumerator ResultLoad()
    {
        if (Time.timeScale == 0f) { Time.timeScale = 1f; }
        yield return new WaitForSeconds(2.5f); //音の分空ける
        SceneManager.LoadScene("ResultScene");
    }

    #endregion

    #region ポーズ画面でゲーム終了
    /// <summary>
    /// ポーズ画面でゲーム終了が押された
    /// </summary>
    public void GameEnd()
    {
        SceneManager.LoadScene("TitleScene");
    }

    #endregion

    #region ゲームシーンロード遅延用
    /// <summary>
    /// ゲームシーン読み込み遅延
    /// </summary>
    IEnumerator GameSceneLoad()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("GameScene");
    }
    #endregion

    #endregion
}
