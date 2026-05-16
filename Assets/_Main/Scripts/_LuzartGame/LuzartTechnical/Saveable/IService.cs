namespace Luzart
{
    public interface IService
    {
        string Id { get; }
        IDomain MyDomain { get; }
        void Inject(IDomain domain);
        void Initialize();
        void StartService();
        void StopService();
        void Terminate();
    }
}