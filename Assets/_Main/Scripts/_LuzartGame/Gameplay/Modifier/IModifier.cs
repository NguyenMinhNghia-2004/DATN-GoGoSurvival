using System.Collections.Generic;
namespace Luzart
{
    // Định nghĩa của một Modifier, ví dụ: Player_ATK_AddRatio hoặc là Player_ATK_Multiply
    public interface IModifierDefinition
    {
        IString DisplayName { get; }
        string FormatValue(double value);
    }
    // Một thành phần của Modifier, ví dụ: Player_ATK_AddRatio (+15%)
    public interface IModifierFactor
    {
        IModifierDefinition Definition { get; }
        INumber Value { get; }
    }
    // Modifier này là một tập hợp các ModifierFactor áp dụng lên thuộc tính của nhân vật,
    // Ví dụ: PreGame_Player_ATK Modifier có contribute đến PreGame_Player_ATK. Trong runtime sẽ add các ModifierFactor vào Modifier này để cập nhật contrubutton
    public interface IModifier
    {
        IModifierDefinition Definition { get; }
        IReadOnlyList<IModifierFactor> Factors { get; }
        void AddFactor(IModifierFactor factor);
        void RemoveFactor(IModifierFactor factor);
    }
    // Một cặp ModifierFactor và Modifier để áp dụng trong Config
    public interface IModifierPair
    {
        IModifierFactor Factor { get; }
        IModifier Modifier { get; }
    }
    // Tương tự IModifierFactorPairGroup nhưng chỉ chứa các ModifierFactor để Activate/Deactivate cùng lúc
    public interface IModifierFactorGroup
    {
        IReadOnlyList<IModifierFactor> Factors { get; }
        void Activate();
        void Deactivate();
    }
}