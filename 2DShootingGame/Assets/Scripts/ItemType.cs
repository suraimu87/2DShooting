/// <summary>
/// アイテムの種類。
/// Item が持ち、Player.ApplyItem の switch で効果を分ける。
/// </summary>
public enum ItemType
{
    /// <summary>移動速度アップ</summary>
    MoveSpeedUp = 0,

    /// <summary>連射力アップ（クールタイム短縮）</summary>
    FireRateUp = 1,

    /// <summary>武器チェンジ（弾の種類を変更）</summary>
    WeaponChange = 2,
}
