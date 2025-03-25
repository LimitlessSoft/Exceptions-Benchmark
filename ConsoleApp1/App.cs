using System.Net;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace ConsoleApp1;

[MemoryDiagnoser]
[ExceptionDiagnoser]
public class App
{
    private const int DEPTH = 100;
    private const int MOCK_METHOD_EXECUTION_TIME_MILLIS = 100;
    
    #region Pure
    [Benchmark]
    public void ResponsePure() => HandlePure(new DefaultHttpContext(), EndpointPure);
    private ResponseWrapper EndpointPure(HttpContext obj) => MockMethodPure(DEPTH);
    private ResponseWrapper MockMethodPure(int callThisMoreTime)
    {
        if (callThisMoreTime == 0)
        {
            Thread.Sleep(MOCK_METHOD_EXECUTION_TIME_MILLIS);
            return new ResponseWrapper() { StatusCode = HttpStatusCode.BadRequest };
        }

        MockMethodPure(callThisMoreTime - 1);
        return new ResponseWrapper() { StatusCode = HttpStatusCode.OK };
    }
    private HttpContext HandlePure(HttpContext context, Func<HttpContext, ResponseWrapper> next)
    {
        var resp = next(context);
        switch (resp.StatusCode)
        {
            case HttpStatusCode.OK:
                context.Response.StatusCode = 200;
                return context;
            case HttpStatusCode.BadRequest:
                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/json";
                context.Response.WriteAsync(resp.ErrorMessage);
                return context;
            default:
                context.Response.StatusCode = 500;
                return context;
        }
    }
    #endregion
    
    #region Exception
    [Benchmark]
    public void ResponseWithException() => HandleException(new DefaultHttpContext(), EndpointWithTryCatch);
    [Benchmark]
    public void ResponseWithExceptionMultiLayer() => HandleException(new DefaultHttpContext(), EndpointWithTryCatchMultiLayer);
    private void EndpointWithTryCatch(HttpContext obj) => MockMethodWithTryCatchBlock(DEPTH);
    private void EndpointWithTryCatchMultiLayer(HttpContext obj) => MockMethodWithTryCatchBlockMultiLayer(DEPTH);
    private void MockMethodWithTryCatchBlock(int callThisMoreTime)
    {
        if (callThisMoreTime == 0)
        {
            Thread.Sleep(MOCK_METHOD_EXECUTION_TIME_MILLIS);
            throw new BadRequestException("Here is bad request message");
        }

        MockMethodWithTryCatchBlock(callThisMoreTime - 1);
    }
    private void MockMethodWithTryCatchBlockMultiLayer(int callThisMoreTime)
    {
        try
        {
            if (callThisMoreTime == 0)
            {
                Thread.Sleep(MOCK_METHOD_EXECUTION_TIME_MILLIS);
                throw new BadRequestException("Here is bad request message");
            }
            MockMethodWithTryCatchBlock(callThisMoreTime - 1);
        }
        catch (Exception e)
        {
            throw;
        }
    }
    private HttpContext HandleException(HttpContext context, Action<HttpContext> next)
    {
        try
        {
            next(context);
            return context;
        }
        catch (BadRequestException e)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            context.Response.WriteAsync(e.Message);
            return context;
        }
        catch (Exception e)
        {
            Console.WriteLine("Unhandled exception: " + e.Message);
            context.Response.StatusCode = 500;
            return context;
        }
    }
    #endregion
}

public class ResponseWrapper
{
    public HttpStatusCode StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}
public class BadRequestException (string message) : Exception(message);