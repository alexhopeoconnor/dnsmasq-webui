using Microsoft.JSInterop;

namespace DnsmasqWebUI.Extensions.Interop;

/// <summary>
/// Safe JS interop helpers that swallow disconnect/cancel/dispose exceptions.
/// Use for best-effort or teardown calls; use raw InvokeVoidAsync/InvokeAsync when failure must propagate.
/// Also swallows <see cref="JSException"/> (JS-side throw) so callers match module *Safe behavior.
/// </summary>
public static class JSRuntimeExtensions
{
    /// <summary>
    /// Invokes a void JS function without throwing on circuit disconnect, cancel, or dispose.
    /// Returns true if the call succeeded, false otherwise.
    /// </summary>
    public static async ValueTask<bool> InvokeVoidAsyncSafe(
        this IJSRuntime js,
        string identifier,
        CancellationToken cancellationToken = default,
        params object?[]? args)
    {
        try
        {
            var a = args ?? [];
            if (cancellationToken == default)
                await js.InvokeVoidAsync(identifier, a);
            else
                await js.InvokeVoidAsync(identifier, cancellationToken, a);
            return true;
        }
        catch (JSDisconnectedException) { return false; }
        catch (TaskCanceledException) { return false; }
        catch (ObjectDisposedException) { return false; }
        catch (JSException) { return false; }
    }

    /// <summary>
    /// Invokes a JS function that returns a value, without throwing on circuit disconnect, cancel, or dispose.
    /// Returns default when the call fails (e.g. circuit gone). For value types, failure is indistinguishable from JS returning default—use try/catch when that matters.
    /// </summary>
    public static async ValueTask<T?> InvokeAsyncSafe<T>(
        this IJSRuntime js,
        string identifier,
        CancellationToken cancellationToken = default,
        params object?[]? args)
    {
        try
        {
            var a = args ?? [];
            if (cancellationToken == default)
                return await js.InvokeAsync<T>(identifier, a);
            return await js.InvokeAsync<T>(identifier, cancellationToken, a);
        }
        catch (JSDisconnectedException) { return default; }
        catch (TaskCanceledException) { return default; }
        catch (ObjectDisposedException) { return default; }
        catch (JSException) { return default; }
    }
}
