/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenNEL_WinUI.Utils;

public static class StaTaskRunner
{
    public static Task<T> RunOnStaAsync<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        var thread = new Thread(() =>
        {
            try
            {
                var r = func();
                tcs.TrySetResult(r);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        thread.IsBackground = true;
        try { thread.SetApartmentState(ApartmentState.STA); } catch { }
        thread.Start();
        return tcs.Task;
    }
}
