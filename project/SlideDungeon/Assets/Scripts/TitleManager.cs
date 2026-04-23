using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    #region シングルトン（他のスクリプトからInstanceでアクセスできるようにする）
    public static TitleManager Instance { get; private set; }
    #endregion

    #region SEの定数

    private const int DECISION = 0;
    private const int CANCEL = 1;

    #endregion

    #region VibrationButton用の定数

    private const int ON = 0;
    private const int OFF = 1;

    #endregion

    #region private変数

    #region タイトルパネルで使うもの
    [Header("タイトルパネルセット")]
    [SerializeField] private GameObject titlePanel;    //タイトルパネル表示・非表示用

    [Header("マスクデータ")]
    [SerializeField] private MaskData data;
    [Header("マスクを置くキャンバスをセット")]
    [SerializeField] private GameObject canvasMask;

    private UIMaskFader fade;
    #endregion

    #region ステージ選択パネルで使うもの
    [Header("ステージセレクトパネルセット")]
    [SerializeField] private GameObject selectPanel;   //ステージセレクトパネル表示・非表示用
    [Header("ステージセレクトの名前入力オブジェセット")]
    [SerializeField] private GameObject inputObject;   //ランキングに登録する名前入力
    [Header("ステージセレクトのオブジェ(ボタンなど)セット")]
    [SerializeField] private GameObject selectObject;  //ステージ選択ボタンなど
    [Header("ステージセレクト画面を前へボタンセット")]
    [SerializeField] private GameObject beforeButton;  //前へボタンを最初の1～9レベルでは描画しない用
    [Header("ステージセレクト画面を次へボタンセット")]
    [SerializeField] private Button nextButton;
    [Header("レベルセレクトを配列にセット")]
    [SerializeField] private GameObject[] levelSelect; //レベル選択のパネル（ゲームオブジェクト）どこを表示するか

    #endregion

    #region ランキング関連で使うもの
    [Header("ランキングセレクトパネルセット")]
    [SerializeField] private GameObject rankSelect;    //ランキングセレクトパネル表示・非表示用
    [Header("ランキングパネルセット")]
    [SerializeField] private GameObject rankPanel;     //ランキングパネル表示・非表示用
    [Header("現在のランキングを表示用テキストセット")]
    [SerializeField] private Text ranking;             //現在のランキング表示
    [Header("ランキング表示用テキストセット")]
    [SerializeField] private Text[] textRanking;       //ランキング表示用

    #endregion

    #region 設定関連で使うもの
    [Header("設定パネルセット")]
    [SerializeField] private GameObject settingPanel;  //設定パネル表示・非表示用
    [Header("0:VibrationOn／1:VibrationOffをセット")]
    [SerializeField] private Image[] vibration;

    #endregion

    #region サウンド関連
    [Header("SE用オーディオソース／本体をセット")]
    [SerializeField] private AudioSource button;       //ボタン鳴らす用
    [SerializeField] private AudioClip[] buttonClip;   //ボタンSE配列

    #endregion

    private GameObject objctName;                      //オブジェクト名
    private GameObject rankName;                       //オブジェクト名

    private int level;                                 //レベルのどこを表示するか
    private bool isButton;                             //次へ or 前へボタンが押されたフラグ
    private bool isSetting;                            //設定ボタンが押されている状態かどうかのフラグ

    #endregion

    #region Set関数

    private void SetRank(int rank) { ranking.text = "レベル" + rank; }

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
        //Debug.Log(level);
        //Debug.Log(levelSelect.Length);
        //Debug.Log(GetIndex());
        UpdateSelect();
        UpdateVibration();
        //Debug.Log(PlayerSetting.Instance.GetVibration());
    }

    #endregion

    #region Start呼び出し関数

    void Init()
    {
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

    #region マスク処理
    void MaskFade()
    {
        if (StageIndex.Instance.GetIsFirst()) { return; }

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

    #region バイブレーションボタンの更新
    void UpdateVibration()
    {
        if (!isSetting) { return; }
        if (PlayerSetting.Instance.GetVibration())
        {
            Color currentOnColor = vibration[ON].color;
            Color currentOffColor = vibration[OFF].color;
            vibration[ON].color = new Color(currentOnColor.r, currentOnColor.g, currentOnColor.b, 1f);       //Alpha値を1（100%の透明度）にする
            vibration[OFF].color = new Color(currentOffColor.r, currentOffColor.g, currentOffColor.b, 0.5f); //Alpha値を0.5（50%の透明度）にする
        }
        else
        {
            Color currentOnColor = vibration[ON].color;
            Color currentOffColor = vibration[OFF].color;
            vibration[OFF].color = new Color(currentOnColor.r, currentOnColor.g, currentOnColor.b, 1f);       //Alpha値を1（100%の透明度）にする
            vibration[ON].color = new Color(currentOffColor.r, currentOffColor.g, currentOffColor.b, 0.5f); //Alpha値を0.5（50%の透明度）にする
        }
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
            RankingManager.Instance.SetStage(StageIndex.Instance.GetIndex()); //初期はレベル1のランキングセット
            //Debug.Log($"RankingManager.Instance: {RankingManager.Instance}");
            RankingManager.Instance.SetRequest(true);
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

    public void RankingLevelSelect()
    {
        objctName = EventSystem.current.currentSelectedGameObject;
        string name = objctName.name;
        button.PlayOneShot(buttonClip[DECISION]);

        // ステージ番号に変換
        if (name.StartsWith("Stage"))
        {
            string numberPart = name.Replace("Stage", "");

            if (int.TryParse(numberPart, out int number))
            {
                StageIndex.Instance.SetIndex(number); //選択されたステージ番号を保存
            }
        }
        int stageIndex = StageIndex.Instance.GetIndex();

        //ランキングデータ取得
        var rankingList = OffLineRankingManager.Instance.GetRanking(stageIndex);

        //一旦全クリア
        for (int i = 0; i < textRanking.Length; i++)
        {
            textRanking[i].text = "";
        }

        //名前とスコアをセット（順位はつけない）
        for (int i = 0; i < rankingList.Count && i < textRanking.Length; i++)
        {
            if (i < rankingList.Count)
            {
                var entry = rankingList[i];
                textRanking[i].text = $"{entry.playerName}　{entry.clearTime:F2}秒"; //全角スペース
            }
            else
            {
                textRanking[i].text = ""; //まだデータがない場合は空欄
            }
        }

        SetRank(stageIndex);
        rankSelect.SetActive(false);
        rankPanel.SetActive(true);
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

    #region 名前が決定されたら
    /*
        public void InputName()
        {
            button.PlayOneShot(buttonClip[DECISION]);
            inputObject.SetActive(false);
            selectObject.SetActive(true);
        }
    */
    #endregion

    #region ステージ選択画面で次のステージボタンが押された時

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

    #endregion

    #region ステージ選択画面で前のステージボタンが押された時

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
                if (DebugMode.Instance.GetDebugMode())
                {
                    //Minutes Games Contest用コメントアウト（オンラインランキング）
                    RankingManager.Instance.SetStage(StageIndex.Instance.GetIndex()); //ここでステージ番号のランキングに切り替える
                }
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
    public void SettingSelect()
    {
        isSetting = true;
        titlePanel.SetActive(false);
        settingPanel.SetActive(true);
    }

    #endregion

    #region バイブレーション On／Off ボタンが押された時
    public void VibrationOnPush()
    {
        PlayerSetting.Instance.SetVibration(true);
    }

    public void VibrationOffPush()
    {
        PlayerSetting.Instance.SetVibration(false);
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

/*    #region ランキングパネルで前へボタンが押された時

    public void BeforeRanking()
    {
        button.PlayOneShot(buttonClip[DECISION]);
        StageIndex.Instance.SetBeforeIndex(1);
        SetRank(StageIndex.Instance.GetIndex());
        RankingManager.Instance.SetStage(StageIndex.Instance.GetIndex()); //ここでランキング名を切り替える
        RankingManager.Instance.SetRequest(true);
    }

    #endregion

    #region ランキングパネルで次へボタンが押された時

    public void NextRanking()
    {
        button.PlayOneShot(buttonClip[DECISION]);
        StageIndex.Instance.SetNextIndex(1);
        SetRank(StageIndex.Instance.GetIndex());
        RankingManager.Instance.SetStage(StageIndex.Instance.GetIndex()); //ここでランキング名を切り替える
        RankingManager.Instance.SetRequest(true);
    }

    #endregion*/

    #endregion

}
