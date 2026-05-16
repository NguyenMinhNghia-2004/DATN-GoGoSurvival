// Luzart/DI/IContent.cs
namespace Luzart
{
    public interface IContent
    {
        string Id { get; }
        IDomain MyDomain { get; }
        void Inject(IDomain domain);
        void Initialize();
        void Start();
        void Stop();
        void Terminate();
    }
}
