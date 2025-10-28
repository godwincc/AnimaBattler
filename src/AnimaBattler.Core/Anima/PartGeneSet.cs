#nullable enable
namespace AnimaBattler.Core.Anima;

/// <summary>
/// Represents the 3 genes for a given part slot.
/// D = Dominant, R1 = Recessive 1, R2 = Recessive 2.
/// </summary>
public record PartGeneSet(string D, string R1, string R2)
{
    public IEnumerable<string> AllCodes
    {
        get
        {
            yield return D;
            yield return R1;
            yield return R2;
        }
    }
}
