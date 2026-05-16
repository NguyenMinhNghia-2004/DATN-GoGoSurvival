namespace Luzart
{
    public interface INumberWithContribution : INumber
    {
        void Contribute(INumber subNumber);
        void Uncontribute(INumber subNumber);
    }
}
