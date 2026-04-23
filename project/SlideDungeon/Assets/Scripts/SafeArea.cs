using UnityEngine;

//このスクリプトが付いているオブジェクトに
//RectTransform が必ず必要になるようにする
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    #region private変数
    //このUIの RectTransform
    private RectTransform rectTransform;

    //前回適用した SafeArea（画面回転や解像度変更の検知用）
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);

    #endregion

    #region Unityイベント関数
    /// <summary>
    /// 最初に1回だけ呼ばれる
    /// </summary>
    void Awake()
    {
        //RectTransform を取得
        rectTransform = GetComponent<RectTransform>();

        //セーフエリアを適用
        ApplySafeArea();
    }

    /// <summary>
    /// 毎フレーム呼ばれる
    /// </summary>
    void Update()
    {
        //画面回転・解像度変更などで SafeArea が変わったら再適用
        if (lastSafeArea != Screen.safeArea)
        {
            ApplySafeArea();
        }
    }
    #endregion

    #region セーフエリアをRectTransformに反映する処理
    /// <summary>
    /// セーフエリアをRectTransformに反映する処理
    /// </summary>
    void ApplySafeArea()
    {
        //現在のセーフエリア（ピクセル単位）を取得
        Rect safeArea = Screen.safeArea;

        //次回比較用に保存
        lastSafeArea = safeArea;

        //セーフエリアの左下座標
        Vector2 anchorMin = safeArea.position;

        //セーフエリアの右上座標
        Vector2 anchorMax = safeArea.position + safeArea.size;

        //ピクセル → 0〜1 の正規化座標に変換
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        //RectTransform のアンカーに反映
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        //アンカーを変更したあとに、余白（Left, Right, Top, Bottom）を0に強制リセットする
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
    #endregion
}