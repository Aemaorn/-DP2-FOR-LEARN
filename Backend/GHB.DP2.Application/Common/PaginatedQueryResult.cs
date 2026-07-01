namespace GHB.DP2.Application.Common;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public sealed class PaginatedQueryResult<T>(IEnumerable<T> data, int totalRecords) : IActionResult
{
    public static readonly PaginatedQueryResult<T> Empty = new([], 0);

    public IEnumerable<T> Data { get; } = data;

    public int TotalRecords { get; } = totalRecords;

    /// <summary>
    /// Executes the result operation of the action method asynchronously. This method is called by MVC to process
    /// the result of an action method.
    /// </summary>
    /// <param name="context">The context in which the result is executed. The context information includes
    /// information about the action that was executed and request information.</param>
    /// <returns>A task that represents the asynchronous execute operation.</returns>
    public Task ExecuteResultAsync(ActionContext context)
    {
        return context.HttpContext.Response.WriteAsJsonAsync(this);
    }
}