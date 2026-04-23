using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarpManager : MonoBehaviour
{
    #region private変数
    [Header("ポータルをセット")]
    [SerializeField] private Transform warpPointFirst;
    [SerializeField] private Transform warpPointSecond;
    [Header("プレイヤーをセット")]
    [SerializeField] private Transform playerPos;
    [Header("クールタイム秒数")]
    [SerializeField] private float warpCooldown = 1f;

    private bool isWarp;
    #endregion


    #region Unityイベント関数
    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        //ワープ中やクールタイム中は処理しない
        if (isWarp) return;

        //ポータルに当たったらコルーチン開始
        if (GameManager.Instance.GetHitCheckFirst())
        {
            StartCoroutine(PlayerWarp(warpPointSecond));
        }
        else if (GameManager.Instance.GetHitCheckSecond())
        {
            StartCoroutine(PlayerWarp(warpPointFirst));
        }
        //Debug.Log(GameManager.Instance.GetPlayerSwiped());
    }
    #endregion

    #region Start呼び出し関数

    void Init()
    {
        isWarp = false;
    }

    #endregion

    #region Update呼び出し関数

    #region ワープクールダウン（何度もワープしないように）
    private IEnumerator PlayerWarp(Transform targetPoint)
    {
        isWarp = true;

        //ワープ処理
        playerPos.position = targetPoint.position;
        //GameManager.Instance.SetPlayerMove(false);

        //ワープ直後は必ず「まだスワイプしてない状態」にリセット
        GameManager.Instance.SetPlayerSwiped(false);

        /* yield return new WaitUntil(() => 条件式);
       「条件式が true になるまで待つ」*/
        //プレイヤーが再び動き出すまで待つ
        yield return new WaitUntil(() => GameManager.Instance.GetPlayerSwiped());

        //クールタイム待機することですぐにワープを防ぐ
        yield return new WaitForSeconds(warpCooldown);

        //スワイプ済みフラグをリセット（消費）
        GameManager.Instance.SetPlayerSwiped(false);

        isWarp = false;
    }
    #endregion

    #endregion
}




/*ソースコード
 * 複数のワープをやる場合こんな感じで取得で行けるかも

            // 検索対象のレイヤー名を指定（例: "Player"）
            string targetLayerName = "Player";
int targetLayer = LayerMask.NameToLayer(targetLayerName);

// 全てのGameObjectを検索する (例: GameObject.FindObjectsOfType)
GameObject[] allObjects = FindObjectsOfType<GameObject>();

// 特定のレイヤーを持つGameObjectを格納するリストを作成
List<GameObject> objectsOnLayer = new List<GameObject>();
foreach (GameObject obj in allObjects)
{
    // オブジェクトのレイヤーと一致するか確認
    if (obj.layer == targetLayer)
    {
        objectsOnLayer.Add(obj);
    }
}
// リストを配列に変換
GameObject[] resultArray = objectsOnLayer.ToArray();*/
