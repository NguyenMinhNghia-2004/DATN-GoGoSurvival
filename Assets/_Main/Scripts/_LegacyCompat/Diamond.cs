using UnityEngine;

/// <summary>
/// GUID anchor cho 4 prefab DiamondGreen/Blue/Red/VIP (script guid
/// <c>34cd61dbc7186ac4583e66814713b55b</c>) đang Missing Script sau W5/W6 nuke.
///
/// <para>Class hoàn toàn rỗng — toàn bộ pickup logic ở <see cref="PickupBase"/> và
/// data nằm trong <see cref="Luzart.DropConfig"/> SO được assign vào prefab. Class tồn
/// tại chỉ vì Unity bind script theo GUID; xoá file = Missing Script trở lại.</para>
///
/// <para>Migrate xong các prefab sang dùng PickupBase trực tiếp (đổi <c>m_Script</c> guid
/// trong prefab YAML) thì có thể xoá class này.</para>
/// </summary>
public class Diamond : PickupBase
{
}
