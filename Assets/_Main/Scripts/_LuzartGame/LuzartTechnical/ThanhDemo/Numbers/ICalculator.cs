namespace Luzart
{
    public interface ICalculator
    {
        event System.Action<ICalculator> OnCalculatorChanged;
        double Calculate(double x);
    }
}
