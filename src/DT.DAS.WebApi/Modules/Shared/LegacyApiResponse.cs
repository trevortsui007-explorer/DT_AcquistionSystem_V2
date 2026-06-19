using Microsoft.AspNetCore.Mvc;

namespace DT.DAS.WebApi.Modules.Shared;

public static class LegacyApiResponse
{
    public static IActionResult Success(ControllerBase controller, string info, object? data = null)
    {
        return controller.Ok(new { code = 1, info, data });
    }

    public static IActionResult Fail(ControllerBase controller, string info, object? data = null)
    {
        return controller.Ok(new { code = 0, info, data });
    }

    public static IActionResult Page(ControllerBase controller, string info, int total, object? data = null)
    {
        return controller.Ok(new { code = 1, info, count = total, data });
    }
}
