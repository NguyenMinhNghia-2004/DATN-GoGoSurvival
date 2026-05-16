namespace Luzart
{
    public interface IStat
    {
        IStatDefinition Definition { get; }
        INumber Value { get; }
    }
    public class StatExtension
    {
        public static RuntimeStat ConvertToRuntimeStat(IStat stat)
        {
            return new RuntimeStat(stat.Definition, stat.Value);
        }
    }
}
