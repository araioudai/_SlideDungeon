using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TraceManager : MonoBehaviour
{
    #region private変数
    [Header("オブジェクト削除時間")]
    [SerializeField] private float destroyTime;

    private float count;

    #endregion

    #region Unityイベント関数
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        DestroyCount();
    }

    #endregion

    #region Update呼び出し関数
    void DestroyCount()
    {
        count += Time.deltaTime;

        if (count > destroyTime) { Destroy(gameObject); }
    }

    #endregion


}
