namespace Luzart
{
    public interface INumber
    {
        double Value { get; }
        event System.Action<INumber> Changed;
    }
}
