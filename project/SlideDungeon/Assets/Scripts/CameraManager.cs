using DG.Tweening;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class CameraManager : MonoBehaviour
{
    #region シングルトン
    public static CameraManager Instance { get; private set; } //他のスクリプトからInstanceでアクセスできるようにする
    #endregion

    #region private変数

/*    [Header("スライダーをセット")]
    [SerializeField] private Slider slider;
    [SerializeField] private RectTransform sliderRange;*/
    [Header("デッドゾーンのX・Y範囲")]
    [SerializeField] private Vector2 deadZone = new Vector2(2f, 1.5f); //デッドゾーンのX・Y範囲（中心からの距離）
    [Header("追従速度をセット")]
    [SerializeField] private float followSpeed = 5f;                   //追従速度（Lerpの係数）
    [Header("スワイプ感度")]
    [SerializeField] private float swipeSensitivity = 0.5f;

    private float minX = 0f;
    private float maxX;
    private Camera mainCam;

    private Transform target;                                          //追従対象（プレイヤーのTransform）
    //private bool isSlider;
    private float totalSwipeDistanceX = 0f;                            //Y座標合計スワイプ量

    #endregion

    #region Set関数
    /// <summary>
    /// 追従対象を変更するメソッド
    /// </summary>
    /// <param name="newTarget">追従ターゲット</param>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// ターゲットをプレイヤーに戻すためのリセットメソッド
    /// </summary>
    public void ResetTarget()
    {
        GameObject player = GameObject.Find("Player(Clone)");
        if (player != null) target = player.transform;
    }
    #endregion

    #region Get関数

    //public bool GetSlide() { return isSlider; }

    #endregion

    #region カメラシェイク処理
    /// <summary>
    /// DOTweenを用いたカメラシェイク処理
    /// </summary>
    /// <param name="time">時間</param>
    /// <param name="strength">強度</param>
    public void CameraShake(float time, float strength)
    {
        transform.DOKill(true);                           //前の揺れをキャンセル

        //カメラを指定された 秒間、強度で揺らす
        transform.DOShakePosition(time, strength);
    }

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
        //onValueChangedイベントにメソッドを登録する
        //slider.onValueChanged.AddListener(OnSliderValueChanged);

        Init();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(isSlider);
        //TapPosCheck();
        //CameraPos();
        SwipeCalculation();
        SwipeMovement();
    }

    void LateUpdate()
    {
        if (target == null                                                    //追従対象がセットされていなければ処理終了
            || !GameManager.Instance.GetIsFollow()                            //追従する必要がないステージなら処理終了
            || GameManager.Instance.GetGameMode() == GameManager.Modes.CAMERA //ゲームモードがカメラなら処理終了
            /*|| isSlider*/) { return; } 
                                                                                                                                                             

        Vector3 pos = transform.position;
        Vector3 tpos = target.position;

        //X 軸の処理
        float dx = tpos.x - pos.x;
        if (Mathf.Abs(dx) > deadZone.x)
        {
            //デッドゾーン外に出ている場合のみLerpで滑らかに追従
            pos.x = Mathf.Lerp(pos.x,
                               tpos.x - Mathf.Sign(dx) * deadZone.x,
                               followSpeed * Time.deltaTime);
        }
        //カメラ位置を更新（Z軸は維持）
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }

    #endregion


    #region Start呼び出し関数

    void Init()
    {
        mainCam = Camera.main;

        //カメラの縦サイズ(orthographicSize)から横幅のワールド単位を計算
        float screenWidthInWorld = mainCam.orthographicSize * 2 * mainCam.aspect;

        //初期位置を0として、右側に画面1枚分いけるように設定
        minX = 0f;
        maxX = screenWidthInWorld;

        StartCoroutine(PlayerTransform());
    }

    IEnumerator PlayerTransform()
    {
        yield return new WaitForSeconds(0.5f);

        target = GameObject.Find("Player(Clone)").transform;
    }

    #endregion

    #region Update呼び出し関数

    #region カメラ追従
    void CameraPos()
    {
/*        if (isSlider)
        {
            //スライダーの値(0～1)を 0～5 のワールド座標に変換
            float x = Mathf.Lerp(0f, 5f, slider.value);

            //カメラの位置を更新
            transform.position = new Vector3(x, 0, -10);
        }*/
    }
    #endregion

    #endregion

    #region Slider値が変更されたときに呼び出されるメソッド

    //Sliderの値が変更されたときに呼び出されるメソッド
/*    public void OnSliderValueChanged(float value)
    {
        Debug.Log("Sliderの値が変更されました: " + value);

        isSlider = true;
    }*/

    #endregion

    #region タップ場所判定

    void TapPosCheck()
    {
/*        if (Input.GetMouseButtonDown(0))
        {
            //タップした位置
            Vector2 tapPos = Input.mousePosition;

            //スライダー範囲に含まれているか
            bool inSlider = RectTransformUtility.RectangleContainsScreenPoint(
                sliderRange, //対象となるUI要素
                tapPos,      //タップ/クリックされたスクリーン座標
                Camera.main  // UIを描画しているカメラ
            );

            if (!inSlider)
            {
                //スライダー以外の場所をタップ時にfalse
                isSlider = false;
            }
            else
            {
                //一応スライダー判定をこっちでもしておく
                isSlider = true;
            }
        }*/
    }

    #endregion

    #region スワイプカメラ移動処理
    /// <summary>
    /// スワイプによるカメラ移動処理
    /// </summary>
    void SwipeMovement()
    {
        if(GameManager.Instance.GetGameMode() != GameManager.Modes.CAMERA) { return; }

        if (Input.GetMouseButton(0))
        {
            //マウスの移動量を取得（感度を掛ける）
            float moveX = Input.GetAxis("Mouse X") * swipeSensitivity;

            //現在の座標から移動量を引く（ドラッグした方向に進む場合はマイナス、背景を掴むならプラス）
            float newX = transform.position.x - moveX;

            //計算した範囲内に制限する
            newX = Mathf.Clamp(newX, minX, maxX);

            //位置を更新
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        }
    }

    #endregion

    #region スワイプ量を取る

    void SwipeCalculation()
    {
        if(GameManager.Instance.GetGameMode() != GameManager.Modes.CAMERA) { return; }

        //マウスの左ボタン、または画面のタッチが押されている間
        if (Input.GetMouseButton(0))
        {
            //前フレームからの移動量を取得
            float deltaX = Input.GetAxis("Mouse X");

            totalSwipeDistanceX += deltaX;

            Debug.Log($"スライド中: Y={totalSwipeDistanceX}");
        }

        //指を離した（クリックを離した）
        if (Input.GetMouseButtonUp(0))
        {
            Debug.Log($"スライド終了: Y={totalSwipeDistanceX}");
            totalSwipeDistanceX = 0f;
        }
    }

    #endregion
}
