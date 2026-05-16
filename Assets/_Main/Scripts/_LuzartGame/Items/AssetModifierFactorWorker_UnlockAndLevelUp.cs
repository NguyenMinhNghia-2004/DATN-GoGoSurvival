using System.Collections.Generic;
#if UNITY_EDITOR
#endif
using UnityEngine;
namespace Luzart
{
    // Cái này sử dụng cho item Artifact, khi unlock item thì sẽ mở khóa modifier factor, và khi level up thì sẽ thêm modifier factor không cần equipped
    public class AssetModifierFactorWorker_UnlockAndLevelUp : AbstractScriptableContent
    {
        [SerializeField] AssetUnlockable unlockable;
        [SerializeField] AssetLevel assetLevel;
        [SerializeField] List<SerializedModifierFactor[]> factorsByLevel;
        public IReadOnlyList<IModifierFactorGroup> ModifierFactorGroupsByLevel { get; }
    }
}