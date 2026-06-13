namespace Luzart
{
    // Faithful port of IO_Training Luzart MVVM View contract.
    public interface IView
    {
        object DataObj { get; }

        void Setup(object data);
        void Teardown();
    }

    public interface IView<T> : IView
    {
        T DataT { get; }

        void SetupT(T data);
    }
}
