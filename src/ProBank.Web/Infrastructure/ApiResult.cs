using Microsoft.AspNetCore.Mvc;

namespace ProBank.Web.Infrastructure;

public static class ApiResult
{
    public static ContentResult Json((int Status, string Json) r) => new() { StatusCode = r.Status, Content = r.Json, ContentType = "application/json" };
}
