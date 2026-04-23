using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerSetting : MonoBehaviour
{
    #region シングルトン
    public static PlayerSetting Instance { get; private set; }
    #endregion

    #region private変数
    [Header("バイブレーション true:ON／false:OFF")]
    [SerializeField] private bool vibration; //バイブレーションON／OFF設定用

    #endregion

    #region Set関数
    public void SetVibration(bool vib) { vibration = vib; }

    #endregion

    #region Get関数
    public bool GetVibration() { return vibration; }

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
    #endregion

}
