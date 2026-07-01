namespace GHB.DP2.Application.Extensions;

using Codehard.Common.DomainModel;
using GHB.DP2.Application.Common;
using LanguageExt;

public static class PaginatedListExtensions
{
    public static PaginatedQueryResult<T> ToResult<T>(this IPaginatedList<T> paginatedList)
        => new(paginatedList, paginatedList.TotalCount);

    public static PaginatedQueryResult<TR> ToResult<T, TR>(this IPaginatedList<T> paginatedList, Func<T, TR> mapper)
        => new(paginatedList.Map(mapper), paginatedList.TotalCount);

    public static PaginatedQueryResult<TR> ToResult<T, TR>(this IPaginatedList<T> paginatedList, Func<T, TR> mapper, int totalRecords)
        => new(paginatedList.Map(mapper), totalRecords);

    public static async Task<PaginatedQueryResult<T>> ToResultAsync<T>(this PaginatedQueryResult<Task<T>> paginatedListTask)
    {
        var dataAsync =
            await paginatedListTask.Data
                                   .SequenceSerial();

        return new PaginatedQueryResult<T>(dataAsync, paginatedListTask.TotalRecords);
    }

    public static async Task<PaginatedQueryResult<TR>> ToResultAsync<T, TR>(this IPaginatedList<T> paginatedList, Func<T, Task<TR>> mapper)
    {
        var mappedData = await paginatedList.Map(mapper).SequenceSerial();

        return new PaginatedQueryResult<TR>(mappedData, paginatedList.TotalCount);
    }
}