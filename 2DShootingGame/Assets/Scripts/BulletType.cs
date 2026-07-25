/// <summary>
/// ショットの種類。
/// Player が「今のタイプ」を持ち、BulletCreater に渡します。
/// </summary>
public enum BulletType
{
    /// <summary>直進1発（クールタイムが短い）</summary>
    Straight = 0,

    /// <summary>直進3方向（正面・斜め上・斜め下）。クールタイムが少し長い</summary>
    Triple = 1,

    /// <summary>貫通弾</summary>
    Pierce = 2,
}
