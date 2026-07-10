using DG.Tweening;
using GoogleMobileAds.Api;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    #region シングルトン（他のスクリプトからInstanceでアクセスできるようにする）
    public static TitleManager Instance { get; private set; }
    #endregion

    #region 定数

    #region SEの定数

    private const int DECISION = 0;
    private const int CANCEL = 1;

    #endregion

    #region VibrationButton用の定数

    private const int ON = 0;
    private const int OFF = 1;

    #endregion

    #endregion

    #region private変数

    #region タイトルパネルで使うもの
    [Header("タイトルパネル関連")]
    [Header("タイトルパネルセット")]
    [SerializeField] private GameObject titlePanel;    //タイトルパネル表示・非表示用

    [Header("マスクデータ")]
    [SerializeField] private MaskData data;
    [Header("マスクを置くキャンバスをセット")]
    [SerializeField] private GameObject canvasMask;

    private UIMaskFader fade;

    [Space(25)]
    #endregion

    #region ステージ選択パネルで使うもの
    [Header("ステージ選択パネル関連")]
    [Header("ステージセレクトパネルセット")]
    [SerializeField] private GameObject selectPanel;      //ステージセレクトパネル表示・非表示用
    [Header("ステージセレクトの名前入力オブジェセット")]
    [SerializeField] private GameObject inputObject;      //ランキングに登録する名前入力
    [Header("ステージセレクトのオブジェ(ボタンなど)セット")]
    [SerializeField] private GameObject selectObject;     //ステージ選択ボタンなど
    [Header("ステージセレクト画面を前へボタンセット")]
    [SerializeField] private GameObject beforeButton;     //前へボタンを最初の1～9レベルでは描画しない用
    [Header("ステージセレクト画面を次へボタンセット")]
    [SerializeField] private Button nextButton;
    [Header("レベルセレクトを配列にセット")]
    [SerializeField] private GameObject[] levelSelect;    //レベル選択のパネル（ゲームオブジェクト）どこを表示するか

    [Space(25)]
    #endregion

    #region ランキング関連で使うもの
    [Header("ランキング関連")]
    [Header("ログインパネル")]
    [SerializeField] private GameObject loginPanel;       //ログイン画面のパネル
    [Header("入力関連")]
    [SerializeField] private TMP_InputField nameInput;    //名前入力欄
    [SerializeField] private TMP_InputField passInput;    //パスワード入力欄
    [Header("状態テキスト")]
    [SerializeField] private TMP_Text statusText;         //「ログイン中...」などの状態表示
    [Header("ロード画面用パネル")]
    [SerializeField] private GameObject loadingPanel;     //ロード中に出すパネル
    [Header("ランキングセレクトパネルセット")]
    [SerializeField] private GameObject rankSelect;       //ランキングセレクトパネル表示・非表示用
    [Header("ランキングパネルセット")]
    [SerializeField] private GameObject rankPanel;        //ランキングパネル表示・非表示用
    [Header("現在のランキングを表示用テキストセット")]
    [SerializeField] private Text ranking;                //現在のランキング表示
    [Header("ランキング表示用テキストセット")]
    [SerializeField] private Text[] textRanking;          //ランキング表示用

    [Space(25)]
    #endregion

    #region 設定関連で使うもの
    [Header("設定関連")]
    [Header("設定パネルセット")]
    [SerializeField] private GameObject settingPanel;     //設定パネル表示・非表示用
    [Header("0:VibrationOn／1:VibrationOffをセット")]
    [SerializeField] private Image[] vibration;

    [Space(25)]
    #endregion

    #region サウンド関連で使うもの
    [Header("サウンド関連")]
    [Header("SE用オーディオソース／本体をセット")]
    [SerializeField] private AudioSource button;          //ボタン鳴らす用
    [SerializeField] private AudioClip[] buttonClip;      //ボタンSE配列

    #endregion

    private GameObject objctName;                         //オブジェクト名
    private GameObject rankName;                          //オブジェクト名

    private int level;                                    //レベルのどこを表示するか
    private bool isButton;                                //次へ or 前へボタンが押されたフラグ
    private bool isSetting;                               //設定ボタンが押されている状態かどうかのフラグ

    #endregion

    #region Set関数

    private void SetRank(int rank) { ranking.text = "Level" + rank; }

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
        //初期化
        Init();

        //ログイン画面分け
        DrawLogin();

        //フェード処理
        MaskFade();

        //起動時の設定に合わせてUIの見た目を初期化
        RefreshVibrationUI();
    }

    // Update is called once per frame
    void Update()
    {
        //描画するステージを更新
        UpdateSelect();
    }

    #endregion

    #region Start呼び出し関数

    void Init()
    {
        //バナーを表示
        if (!StageIndex.Instance.GetIsFirst() && GoogleAdMobBanner.Instance != null) { GoogleAdMobBanner.Instance.BannerShow(); }

        //バイブレーションプラグインの初期化
#if UNITY_ANDROID || UNITY_IOS
        Vibration.Init();
#endif
        Application.targetFrameRate = 120; //タブレットだと遅い場合があるからフレームレートを120まで上げておく
        level = 0;
        isButton = false;
        isSetting = false;
        if (Time.timeScale == 0f) { Time.timeScale = 1f; }
        //タイトルパネル以外一旦すべて非表示
        settingPanel.SetActive(false);
        rankSelect.SetActive(false);
        rankPanel.SetActive(false);
        selectPanel.SetActive(false);
    }

    /// <summary>
    /// ログイン画面表示分け
    /// </summary>
    void DrawLogin()
    {
        //ログイン済みかどうかで表示を分ける
        if (OnLineRanking.Instance.IsLoggedIn)
        {
            //すでにログインIDがあれば、タイトルを表示
            loginPanel.SetActive(false);
            titlePanel.SetActive(true);
        }
        else
        {
            //未ログインならログインパネルを表示
            loginPanel.SetActive(true);
            titlePanel.SetActive(false);
        }
    }

    #region マスク処理
    void MaskFade()
    {
        //if (StageIndex.Instance.GetIsFirst()) { return; }

        LiftFade();
    }

    /// <summary>
    /// フェードイン処理
    /// </summary>
    /// <returns></returns>
    private void LiftFade()
    {
        //広がるアニメーション
        StartCoroutine(fade.PlayFadeIn(data.MaskSpeed(MaskData.MaskType.IN)));
    }
    #endregion

#endregion

    #region Update呼び出し関数

    #region 描画するステージを更新

    void UpdateSelect()
    {
        //ボタンが押されてないときはreturn
        if (!isButton) { return; }

        //押されたボタンに対応した次のパネルを表示
        levelSelect[level].SetActive(true);
        
        //ボタンが押されたフラグリセット
        isButton = false;
    }

    #endregion

    #endregion

    #region ボタン呼び出し関数

    #region タイトルでステージセレクトが押された時

    public void GameSelect()
    {
        button.PlayOneShot(buttonClip[DECISION]);
        titlePanel.SetActive(false);
        rankPanel.SetActive(false);
        selectPanel.SetActive(true);
        inputObject.SetActive(false);
        selectObject.SetActive(true);
        levelSelect[0].SetActive(true);
    }

    #endregion

    #region タイトルでランキングが押された時

    public void RankingSelect()
    {
        button.PlayOneShot(buttonClip[DECISION]);
        StageIndex.Instance.SetIndex(1);
        if (DebugMode.Instance.GetDebugMode()) 
        {
            //RankingManager.Instance.SetStage(StageIndex.Instance.GetIndex()); //初期はレベル1のランキングセット
            //Debug.Log($"RankingManager.Instance: {RankingManager.Instance}");
            //RankingManager.Instance.SetRequest(true);
        }
        else
        {
            //オフラインランキング処理
        }
        titlePanel.SetActive(false);
        selectPanel.SetActive(false);
        rankSelect.SetActive(true);
        rankPanel.SetActive(false);
    }

    #endregion

    #region 見たいランキングレベルの何かが押された時
    /// <summary>
    /// ランキングレベルの何かが押された時
    /// </summary>
    public void RankingLevelSelect()
    {
        //押されたボタンからステージ番号を取得
        objctName = EventSystem.current.currentSelectedGameObject;
        string name = objctName.name;
        button.PlayOneShot(buttonClip[DECISION]);

        int stageIndex = 1;
        if (name.StartsWith("Stage"))
        {
            string numberPart = name.Replace("Stage", "");
            int.TryParse(numberPart, out stageIndex);
        }
        StageIndex.Instance.SetIndex(stageIndex);

        //DebugMode（オンライン/オフライン）によって処理を分岐
        if (DebugMode.Instance.GetDebugMode())
        {
            //【オンラインモード】
            //通信中は「ロード中...」のパネルを表示（二重クリック防止）
            loadingPanel.SetActive(true);

            //OnLineRankingにデータ取得を依頼
            //通信が終わったら、自動的に実行
            OnLineRanking.Instance.GetRanking(stageIndex, (rankingList) =>
            {
                //通信が終わったのでロード画面を隠す
                loadingPanel.SetActive(false);

                //届いたリストを表示用関数に渡す
                DisplayRankingData(rankingList, stageIndex);
            });
        }
        else
        {
            //【オフライン】
            //ローカル保存されているJSONからリストを取得（一瞬なのでロード画面は不要）
            var rankingList = OffLineRankingManager.Instance.GetRanking(stageIndex);

            //表示用関数に渡す
            DisplayRankingData(rankingList, stageIndex);
        }
    }

    /// <summary>
    /// 取得したデータ（List<ScoreEntry>）を実際のUIテキストに反映する
    /// オンライン・オフライン共通で使う表示の最終出口。
    /// </summary>
    private void DisplayRankingData(List<ScoreEntry> rankingList, int stageIndex)
    {
        //まずはテキストを全クリア
        for (int i = 0; i < textRanking.Length; i++)
        {
            textRanking[i].text = "";
        }

        //届いたランキングデータを1つずつテキストに入れていく
        for (int i = 0; i < rankingList.Count && i < textRanking.Length; i++)
        {
            var entry = rankingList[i];
            //オンラインの場合は、サーバー側でIDから「最新の名前」に変換されたものが届いている
            textRanking[i].text = $"{entry.playerName}　{entry.clearTime:F2}秒";
        }

        //表示するパネルの切り替え
        SetRank(stageIndex);
        rankSelect.SetActive(false); //レベル選択画面を隠す
        rankPanel.SetActive(true);   //ランキング表示パネルを出す
    }

    #endregion

    #region ランキングパネルから選択パネルへ戻る時

    public void ExitRanking()
    {
        button.PlayOneShot(buttonClip[CANCEL]);
        rankPanel.SetActive(false);
        rankSelect.SetActive(true);
    }

    #endregion

    #region ランキング関連

    /// <summary>
    /// ログインボタンが押された時
    /// </summary>
    public void OnLoginClick()
    {
        button.PlayOneShot(buttonClip[DECISION]);

        //名前とパスワード
        string userName = nameInput.text;
        string password = passInput.text;

        //名前とパスワードの入力チェック
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)) return;

        statusText.fontSize = 150;
        StartLoadingAnim(statusText, "通信中");
        OnLineRanking.Instance.Login(userName, password, (success, message) =>
        {
            if (success)
            {
                HandleAuthSuccess("ログインしました！");
            }
            else
            {
                DOTween.Kill("LoadingDots"); //エラー時もアニメを止める
                statusText.fontSize = 100;
                statusText.text = message;   //「パスワードが違います」を表示
            }
        });
    }

    /// <summary>
    /// 新規登録ボタンが押された時
    /// </summary>
    public void OnRegisterClick()
    {
        button.PlayOneShot(buttonClip[DECISION]);

        //名前とパスワード
        string userName = nameInput.text;
        string password = passInput.text;

        //名前とパスワードの入力チェック
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)) return;

        statusText.fontSize = 150;
        StartLoadingAnim(statusText, "登録中");
        OnLineRanking.Instance.Register(userName, password, (success, message) =>
        {
            if (success)
            {
                HandleAuthSuccess("登録が完了しました！");
            }
            else
            {
                DOTween.Kill("LoadingDots"); //エラー時もアニメを止める
                statusText.fontSize = 100;
                statusText.text = message;   //「この名前は使われています」を表示
            }
        });
    }

    /// <summary>
    /// 通信開始時に呼ぶアニメーション
    /// </summary>
    /// <param name="targetText">アニメーションさせたいテキスト</param>
    /// <param name="baseMessage">表示したい固定文字</param>
    void StartLoadingAnim(TMP_Text targetText, string baseMessage)
    {
        int dotCount = 0;

        //DOVirtual.DelayedCall を使って 0.5秒おきに呼び出し
        //最後の引数(false)をtrueにすると無限ループ
        Sequence seq = DOTween.Sequence().SetId("LoadingDots"); //IDをセット

        //0.5秒待ってからドットを更新する処理をループさせる
        seq.AppendCallback(() => {
            dotCount = (dotCount + 1) % 4; // 0, 1, 2, 3 の繰り返し

            string visibleDots = new string('.', dotCount);
            string invisibleDots = new string('.', 3 - dotCount);

            //透明なドットを混ぜて全体の幅を維持
            targetText.text = $"{baseMessage}{visibleDots}<color=#00000000>{invisibleDots}</color>";
        });
        seq.AppendInterval(0.5f);
        seq.SetLoops(-1); //無限ループ
    }


    /// <summary>
    /// ログイン・登録成功時の演出
    /// </summary>
    /// <param name="successMessage">表示テキスト</param>
    private void HandleAuthSuccess(string successMessage)
    {
        //通信中のドットアニメを停止
        DOTween.Kill("LoadingDots");
        statusText.fontSize = 80;
        statusText.text = successMessage;

        //メッセージを読ませるために0.5秒待ってからフェード開始
        DOVirtual.DelayedCall(0.5f, () =>
        {
            //フェードアウト（画面を閉じる）
            StartCoroutine(fade.PlayFadeOut(data.MaskSpeed(MaskData.MaskType.OUT), () =>
            {
                //画面が閉じきった後の処理
                loginPanel.SetActive(false);
                titlePanel.SetActive(true);

                //フェードイン（画面を開く）
                StartCoroutine(fade.PlayFadeIn(data.MaskSpeed(MaskData.MaskType.IN)));
            }));
        });
    }

    #endregion

    #region ステージ選択画面で表示ステージ変更ボタンが押された時
    /// <summary>
    /// ステージ選択画面で次のステージボタンが押された時の処理
    /// </summary>
    public void NextSelect()
    {
        button.PlayOneShot(buttonClip[DECISION]);
        //レベル選択のマックスだったら次のレベルへのボタンが押せないようにする
        if (level >= levelSelect.Length - 2) { nextButton.interactable = false; }

        //最初の選択画面で次へボタンが押された時、前へボタン表示
        if (level == 0) { beforeButton.SetActive(true); }

        //ボタン押したフラグtrue
        isButton = true;

        //ボタンが押された時現在の選択画面を非表示
        levelSelect[level].SetActive(false);

        //選択画面を1進める
        level += 1;
    }

    /// <summary>
    /// ステージ選択画面で前のステージボタンが押された時の処理
    /// </summary>
    public void BeforeSelect()
    {
        button.PlayOneShot(buttonClip[DECISION]);
        // level が配列範囲外なら return
        if (level < 0) { return; }

        //次のレベルへのボタンが押せるようにする
        nextButton.interactable = true;

        //最初の画面のひとつ前で前へボタンが押された時、前へボタン非表示
        if (level == 1) { beforeButton.SetActive(false); }

        //ボタン押したフラグtrue
        isButton = true;

        //ボタンが押された時現在の選択画面を非表示
        levelSelect[level].SetActive(false);
        if (level > 0)
        {
            level -= 1; //0（配列の一番小さい値）以上だったらマイナス
        }
    }

    #endregion

    #region タイトルでステージの何かが押された時
    /// <summary>
    /// ゲームスタート処理
    /// </summary>
    public void GameStart()
    {
        objctName = EventSystem.current.currentSelectedGameObject;
        string name = objctName.name;
        button.PlayOneShot(buttonClip[DECISION]);

        //ステージ番号に変換
        if (name.StartsWith("Stage"))
        {
            string numberPart = name.Replace("Stage", "");

            //TryParseで安全に整数に変換（失敗してもクラッシュしない）
            if (int.TryParse(numberPart, out int number))
            {
                StageIndex.Instance.SetIndex(number); //選択されたステージ番号を保存

                //バナーを消す
                if(GoogleAdMobBanner.Instance != null) { GoogleAdMobBanner.Instance.BannerHide(); }

                // 1. フェードアウト（画面を閉じる）を開始
                // 2. 第二引数のラムダ式は、アニメーション終了後に実行される
                StartCoroutine(fade.PlayFadeOut(data.MaskSpeed(MaskData.MaskType.OUT), () =>
                {
                    //画面が閉じきったタイミングでシーン遷移を開始
                    StartCoroutine(StageLoad());
                }));
            }
            else
            {
                //Debug.LogWarning("ステージ名に数値が含まれていません: " + name);

                StartCoroutine(TextCountDown());
            }
        }
    }

    IEnumerator StageLoad()
    {
        StageIndex.Instance.SetIsFirst(false);

        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("GameScene");
    }

    IEnumerator TextCountDown()
    {
        //TextRock.SetActive(true);

        yield return new WaitForSeconds(1.0f); //1秒待つ

        //TextRock.SetActive(false);
    }
    #endregion

    #region タイトルで設定が押された時
    /// <summary>
    /// 設定が押された時の処理
    /// </summary>
    public void SettingSelect()
    {
        button.PlayOneShot(buttonClip[DECISION]);
        isSetting = true;
        titlePanel.SetActive(false);
        settingPanel.SetActive(true);
    }

    #endregion

    #region バイブレーション On／Off ボタンが押された時

    #region バイブレーションUIの更新
    /// <summary>
    /// 現在のPlayerSettingの状態に合わせて、設定画面のボタン色を同期する
    /// </summary>
    private void RefreshVibrationUI()
    {
        button.PlayOneShot(buttonClip[DECISION]);

        if (PlayerSetting.Instance.GetVibration())
        {
            Color currentOnColor = vibration[ON].color;
            Color currentOffColor = vibration[OFF].color;
            vibration[ON].color = new Color(currentOnColor.r, currentOnColor.g, currentOnColor.b, 1f);       // ONをクッキリ
            vibration[OFF].color = new Color(currentOffColor.r, currentOffColor.g, currentOffColor.b, 0.5f); // OFFを半透明
        }
        else
        {
            Color currentOnColor = vibration[ON].color;
            Color currentOffColor = vibration[OFF].color;
            vibration[OFF].color = new Color(currentOffColor.r, currentOffColor.g, currentOffColor.b, 1f);   // OFFをクッキリ
            vibration[ON].color = new Color(currentOnColor.r, currentOnColor.g, currentOnColor.b, 0.5f);     // ONを半透明
        }
    }
    #endregion

    public void VibrationOnPush()
    {
        PlayerSetting.Instance.SetVibration(true);
        RefreshVibrationUI();
    }

    public void VibrationOffPush()
    {
        PlayerSetting.Instance.SetVibration(false);
        RefreshVibrationUI();
    }

    #endregion

    #region 言語設定の何かが押された時

    /// <summary>
    /// 言語設定降下時の処理
    /// </summary>
    public void PushLanguage()
    {
        button.PlayOneShot(buttonClip[DECISION]);
    }

    #endregion

    #region タイトルへ戻るが押された時

    public void Exit()
    {
        button.PlayOneShot(buttonClip[CANCEL]);
        //一旦全部非表示にしてから
        levelSelect[level].SetActive(false);
        beforeButton.SetActive(false);
        rankSelect.SetActive(false);
        selectPanel.SetActive(false);
        rankPanel.SetActive(false);
        settingPanel.SetActive(false);
        //レベル初期化して、最初のレベルボタンを表示
        level = 0;
        levelSelect[level].SetActive(true);
        //次のレベルへのボタンが押せるようにしておく
        nextButton.interactable = true;

        titlePanel.SetActive(true);
    }

    #endregion

    #region ログアウトボタンが押された時

    /// <summary>
    /// ログアウト処理
    /// </summary>
    public void Logout()
    {
        button.PlayOneShot(buttonClip[DECISION]);

        StartCoroutine(fade.PlayFadeOut(data.MaskSpeed(MaskData.MaskType.OUT), () =>
        {
            OnLineRanking.Instance.ResetId();          //IDの削除
            PlayerPrefs.DeleteKey("OnlineUserID");     //ユーザ情報を削除する
            PlayerPrefs.DeleteKey("Tutorial_Cleared"); //チュートリアルのクリアフラグを削除する

            PlayerPrefs.Save();                        //セーブする
            Debug.Log("ログアウトしました（PlayerPrefsを削除）");

            if (StageIndex.Instance != null)
            {
                StageIndex.Instance.SetFirst(true);
            }

            //画面が閉じきったタイミングでシーン遷移を開始
            StartCoroutine(TitleLoad());
        }));
    }

    IEnumerator TitleLoad()
    {
        yield return new WaitForSeconds(1.0f);
        SceneManager.LoadScene("TitleScene");
    }

    #endregion

    #endregion

}
