using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialManager;

public class InputPlayerManager : MonoBehaviour
{
    #region シングルトン（他のスクリプトからInstanceでアクセスできるようにする）
    public static InputPlayerManager Instance { get; private set; }
    #endregion

    #region 操作状態の列挙対
    public enum OperationType
    {
        UP,
        DOWN,
        LEFT,
        RIGHT,
        TAP,
        NONE
    }
    #endregion

    #region private変数

    private Vector3 tapStartPos; //スワイプ判定最初押した時
    private Vector3 tapEndPos;   //スワイプ判定最後離した時
    private bool isInput;        //入力が確定したか

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

    // Update is called once per frame
    void Update()
    {
        if(TutorialManager.Instance.GetState() == TutorialState.SHOW_SWIPE || TutorialManager.Instance.GetState() == TutorialState.SHOW_GOAL) { return; }

        //タップしてるか
        if (Input.GetMouseButtonDown(0))
        {
            tapStartPos = Input.mousePosition;
            isInput = false;
        }

        //指を離したか
        if (Input.GetMouseButtonUp(0))
        {
            tapEndPos = Input.mousePosition;
            isInput = true;
        }
    }
    #endregion

    #region Start呼び出し関数
    /// <summary>
    /// 初期化
    /// </summary>
    void Init()
    {
        tapStartPos = Vector3.zero;
        tapEndPos = Vector3.zero;
    }


    #endregion

    #region スワイプ方向取得
    /// <summary>
    /// スワイプ方向取得処理
    /// </summary>
    /// <returns>スワイプ方向</returns>
    public OperationType GetDirection()
    {
        if (!isInput) return OperationType.NONE;

        isInput = false; //1回だけ

        OperationType ret = OperationType.NONE;
        var directionX = tapEndPos.x - tapStartPos.x;
        var directionY = tapEndPos.y - tapStartPos.y;

        if (Player.Instance.GetMovable())
        {
            //横と縦どっちにスワイプしたか
            if (Mathf.Abs(directionY) < Mathf.Abs(directionX))
            {
                //横方向にスワイプした時
                if (directionX > 0)
                {
                    ret = OperationType.RIGHT;
                    Player.Instance.SetMovable(false);
                }
                else if (directionX < 0)
                {
                    ret = OperationType.LEFT;
                    Player.Instance.SetMovable(false);
                }
                else
                {
                    //タップ判定
                    ret = OperationType.TAP;
                }

            }
            else if (Mathf.Abs(directionY) > Mathf.Abs(directionX))
            {
                //縦方向にスワイプした時
                if (directionY > 0)
                {
                    ret = OperationType.UP;
                    Player.Instance.SetMovable(false);

                }
                else if (directionY < 0)
                {
                    ret = OperationType.DOWN;
                    Player.Instance.SetMovable(false);
                }
                else
                {
                    //タップ判定
                    ret = OperationType.TAP;
                }
            }
            else
            {
                //上記意外はタップ扱い
                ret = OperationType.TAP;
            }
        }
        return ret;
    }

    #endregion
}
