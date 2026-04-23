using UnityEngine;
using UnityEngine.EventSystems;

public class UIInputBlocker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>
    /// ポインター（マウスカーソルや指）がUI要素の範囲内に入った時に呼ばれる処理
    /// </summary>
    /// <param name="eventData">イベントの詳細データ</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        //プレイヤーが生成済み（シングルトンが有効）か確認
        if (Player.Instance != null)
        {
            //プレイヤー側の「UI操作中フラグ」を立てて入力を禁止
            Player.Instance.SetOverUIForbidden(true);
        }
    }

    /// <summary>
    /// ポインターがUI要素の範囲外に出た時、または指が離れた時に呼ばれる処理
    /// </summary>
    /// <param name="eventData">イベントの詳細データ</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        //プレイヤーが生成済みか確認
        if (Player.Instance != null)
        {
            //プレイヤー側の「UI操作中フラグ」を下ろして入力を許可する
            Player.Instance.SetOverUIForbidden(false);
        }
    }

    /// <summary>
    /// オブジェクトが非アクティブになった際の安全策
    /// </summary>
    private void OnDisable()
    {
        if (Player.Instance != null)
        {
            Player.Instance.SetOverUIForbidden(false);
        }
    }
}