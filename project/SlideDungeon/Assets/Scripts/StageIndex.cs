using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageIndex : MonoBehaviour
{
    #region シングルトン（他のスクリプトからInstanceでアクセスできるようにする）
    public static StageIndex Instance { get; private set; }
    #endregion

    #region private変数
    private int stageIndex;        //ステージ番号
    private bool firstTime = true; //最初のプレイだったらチュートリアル
    private bool isFirst = true;   //最初ならフェード処理しないフラグ
    #endregion

    #region Set関数
    /// <summary>
    /// ステージ番号セット
    /// </summary>
    /// <param name="index">現在のステージ番号</param>
    public void SetIndex(int index) { stageIndex = index; }

    /// <summary>
    /// ステージ番号を次へ（ランキングパネルや次のステージへなど）
    /// </summary>
    /// <param name="index">現在のステージ番号</param>
    public void SetNextIndex(int index) { stageIndex += index; if (stageIndex > 14) stageIndex = 1; }

    /// <summary>
    /// 最初のプレイかどうかセット用
    /// </summary>
    /// <param name="first"></param>
    public void SetFirst(bool first) { firstTime = first; }

    /// <summary>
    /// 最初（起動時）フェード処理しないセット用
    /// </summary>
    /// <param name="first">最初かどうか</param>
    /// <returns></returns>
    public void SetIsFirst(bool first) { isFirst = first; }
    #endregion

    #region Get関数
    /// <summary>
    /// ステージ番号入手用
    /// </summary>
    /// <returns>現在のステージ番号</returns>
    public int GetIndex() { return stageIndex; }

    /// <summary>
    /// 最初のプレイかどうか入手用
    /// </summary>
    /// <returns></returns>
    public bool GetFirst() {  return firstTime; }

    /// <summary>
    /// 最初（起動時）かどうか入手用
    /// </summary>
    /// <returns></returns>
    public bool GetIsFirst() { return isFirst; }
    #endregion

    #region Unityイベント関数
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }
    #endregion

    #region Start呼び出し関数
    void Init()
    {
        //オンラインモード（debugMode == true）のときだけ保存データをロード
        if (DebugMode.Instance != null && DebugMode.Instance.GetDebugMode())
        {
            LoadTutorialProgress();
        }
/*        else
        {
            //オフライン（展示会モード）なら、毎回強制的にチュートリアルを有効にする
            firstTime = true;
        }*/
    }

    /// <summary>
    /// オンライン用：PlayerPrefsからチュートリアル完了状態を読み込む
    /// </summary>
    private void LoadTutorialProgress()
    {
        //すでにクリアしていれば、firstTimeをfalse にしてチュートリアルをスキップ
        if (PlayerPrefs.GetInt("Tutorial_Cleared", 0) == 1)
        {
            firstTime = false;
        }
        else
        {
            firstTime = true;
        }
    }
    #endregion
}
