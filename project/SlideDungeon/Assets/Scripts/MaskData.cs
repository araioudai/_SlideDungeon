using UnityEngine;

/// <summary>
/// マスク用のデータ
/// </summary>
[CreateAssetMenu(
    fileName = "MaskData",
    menuName = "ScriptableObjects/MaskData"
)]
public class MaskData : ScriptableObject {
    public enum MaskType
    {
        OUT,
        IN
    }

    [Header("Mask用パネルプレファブをセット")]
    public GameObject panelMask;

    [Header("Maskスピード調整用")]
    [SerializeField, EnumIndex(typeof(MaskType))] private float[] maskSpeed = new float[2];

    /// <summary>
    /// マスクスピード入手用
    /// </summary>
    /// <param name="type">マスクタイプ</param>
    /// <returns></returns>
    public float MaskSpeed(MaskType type)
    {
        return maskSpeed[(int)type];
    } 
}
