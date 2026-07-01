namespace GHB.DP2.Domain.Common;

using LanguageExt;

public interface IHasSoftDelete
{
    bool IsDeleted { get; }

    Unit Delete();
}