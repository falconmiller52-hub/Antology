using UnityEngine;

/// <summary>
/// Ключ разведки. Связывает информацию из источника (газета, письмо, радио, интервью)
/// с репликой в редакторе сюжетов.
/// Создаётся через Assets → Create → Intel → Key.
///
/// Когда игрок маркирует текст в газете/письме или слушает реплику по радио/интервью,
/// ключ добавляется в IntelManager. Реплики в редакторе сюжетов с этим ключом
/// становятся доступными (вместо "???").
/// </summary>
[CreateAssetMenu(fileName = "NewIntelKey", menuName = "Intel/Key")]
public class IntelKey : ScriptableObject
{
    [Tooltip("Название для отладки")]
    public string keyName = "Новый ключ";

    [TextArea(1, 3)]
    [Tooltip("Описание — какая информация раскрывается")]
    public string description;
}
