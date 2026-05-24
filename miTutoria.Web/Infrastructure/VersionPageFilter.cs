using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace miTutoria.Web.Infrastructure;

public class VersionPageFilter(string gitHash) : IPageFilter
{
    public void OnPageHandlerSelected(PageHandlerSelectedContext context) { }
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (context.HandlerInstance is PageModel page)
            page.ViewData["GitHash"] = gitHash;
    }
    public void OnPageHandlerExecuted(PageHandlerExecutedContext context) { }
}