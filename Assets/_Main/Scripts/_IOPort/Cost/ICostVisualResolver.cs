namespace Luzart
{
    // Faithful port — replaces the marker stub in _FrameworkStubs.cs.
    public interface ICostVisualResolver
    {
        IView GetCostView(ICost data, object displayContext);
    }

    public enum EResourceCostView
    {
        SingleLine = 0,
    }
}
