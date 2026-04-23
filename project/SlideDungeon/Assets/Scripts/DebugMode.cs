using UnityEngine;

public class DebugMode : MonoBehaviour
{
    #region シングルトン
    public static DebugMode Instance { get; private set; }
    #endregion

    #region private変数

    [Header("オンラインランキングで使うものセット")]
    [SerializeField] private GameObject[] onlineRank;
    [Header("オフラインランキングで使うものセット")]
    [SerializeField] private GameObject[] offlineRank;
    [Header("ture: オンラインランキング／false: オフラインランキング")]
    [SerializeField] private bool debugMode;

    #endregion

    #region Get関数

    public bool GetDebugMode() {  return debugMode; }

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

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(debugMode);
    }

    #endregion

    #region Start呼び出し関数

    void Init()
    {
        if (debugMode)
        {
            //オフラインランキングで使うものを非表示
            for (int i = 0; i < offlineRank.Length; i++)
            {
                offlineRank[i].SetActive(false);
            }
            for(int i = 0; i < onlineRank.Length; i++)
            {
                onlineRank[i].SetActive(true);
            }
        }
        else
        {
            //オンラインランキングで使うものを非表示
            for (int i = 0; i < onlineRank.Length; i++)
            {
                onlineRank[i].SetActive(false);
            }
            for (int i = 0; i < offlineRank.Length; i++)
            {
                offlineRank[i].SetActive(true);
            }
        }
    }

    #endregion
}
