using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    #region シングルトン
    public static SoundManager Instance { get; private set; }
    #endregion

    #region private変数

    #region BGM
    [Header("BGM用オーディオソースセット")]
    [SerializeField] private AudioSource bgmSource;
    [Header("BGM用オーディオ（本体）セット")]
    [SerializeField] private AudioClip[] bgmClip;
    #endregion

    #region SE
    [Header("SE用オーディオソースセット")]
    [SerializeField] private AudioSource seSource;
    [Header("SE用オーディオ（本体）セット")]
    [SerializeField] private AudioClip[] seClip;
    #endregion

    private int number;

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
        //number = StageIndex.Instance.GetIndex();
        //minutes用データ節約
        number = 1;
        BgmPlay(number);
    }
    #endregion

    void BgmPlay(int number)
    {
        bgmSource.PlayOneShot(bgmClip[number-1]);
/*        if (GameManager.Instance.GetGameClear())
        {
            bgmSource.Stop();
        }*/
    }

    public void BgmStop()
    {
        bgmSource.Stop();
    }

    public void SePlay(int number)
    {
        seSource.PlayOneShot(seClip[number]);
    }
}
