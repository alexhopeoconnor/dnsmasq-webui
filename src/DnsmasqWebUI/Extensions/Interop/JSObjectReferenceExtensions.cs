using Microsoft.JSInterop;

namespace DnsmasqWebUI.Extensions.Interop;

/// <summary>
/// Safe JS module interop helpers that swallow disconnect/cancel/dispose exceptions.
/// Use for best-effort or teardown calls (dispose, cleanup, hub callbacks, error toasts).
/// Swallows <see cref="JSException"/> when the invoked JS throws.
/// </summary>
public static class JSObjectReferenceExtensions
{
    /// <summary>
    /// Invokes a void method on the module without throwing on circuit disconnect, cancel, or dispose.
    /// Returns true if the call succeeded, false otherwise.
    /// </summary>
    public static async ValueTask<bool> InvokeVoidAsyncSafe(
        this IJSObjectReference module,
        string identifier,
        params object?[]? args)
    {
        try
        {
            await module.InvokeVoidAsync(identifier, args ?? []);
            return true;
        }
        catch (JSDisconnectedException) { return false; }
        catch (TaskCanceledException) { return false; }
        catch (ObjectDisposedException) { return false; }
        catch (JSException) { return false; }
    }

    /// <summary>
    /// Invokes a method on the module that returns a value, without throwing on circuit disconnect, cancel, or dispose.
    /// Returns default when the call fails. For value types (e.g. <see cref="bool"/>), failure and a legitimate default
    /// from JS are indistinguishable—use raw <see cref="IJSObjectReference.InvokeAsync{TValue}"/> with try/catch when that matters.
    /// </summary>
    public static async ValueTask<T?> InvokeAsyncSafe<T>(
        this IJSObjectReference module,
        string identifier,
        params object?[]? args)
    {
        try
        {
            return await module.InvokeAsync<T>(identifier, args ?? []);
        }
        catch (JSDisconnectedException) { return default; }
        catch (TaskCanceledException) { return default; }
        catch (ObjectDisposedException) { return default; }
        catch (JSException) { return default; }
    }

    /// <summary>
    /// Disposes the module reference without throwing. Use in DisposeAsync when the circuit may already be gone.
    /// </summary>
    public static async ValueTask DisposeAsyncSafe(this IJSObjectReference? module)
    {
        if (module == null) return;
        try
        {
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
        catch (JSException) { }
    }
}
