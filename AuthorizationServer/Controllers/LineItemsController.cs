using System.Threading.Tasks;
using LtiAdvantage.AspNetCore.AssignmentGradeServices;
using LtiAdvantage.AssignmentGradeServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AuthorizationServer.Controllers;

public class LineItemsController : LineItemsControllerBase
{
    public LineItemsController(IWebHostEnvironment env, ILogger<LineItemsControllerBase> logger) : base(env, logger)
    {
    }

    protected override Task<ActionResult<LineItem>> OnAddLineItemAsync(AddLineItemRequest request)
    {
        throw new System.NotImplementedException();
    }

    protected override Task<ActionResult> OnDeleteLineItemAsync(DeleteLineItemRequest request)
    {
        throw new System.NotImplementedException();
    }

    protected override Task<ActionResult<LineItem>> OnGetLineItemAsync(GetLineItemRequest request)
    {
        throw new System.NotImplementedException();
    }

    protected override Task<ActionResult<LineItemContainer>> OnGetLineItemsAsync(GetLineItemsRequest request)
    {
        throw new System.NotImplementedException();
    }

    protected override Task<ActionResult> OnUpdateLineItemAsync(UpdateLineItemRequest request)
    {
        throw new System.NotImplementedException();
    }
}