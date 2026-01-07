/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System.Net.Sockets;
using System.Text;

namespace OpenNEL.IRC;

public class TcpConnection : IDisposable
{
    readonly string _host;
    readonly int _port;
    readonly object _lock = new();
    
    TcpClient? _tcp;
    StreamReader? _reader;
    StreamWriter? _writer;

    public bool Connected { get; private set; }

    public TcpConnection(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public void Connect()
    {
        lock (_lock)
        {
            if (Connected) return;
            _tcp = new TcpClient();
            _tcp.Connect(_host, _port);
            var stream = _tcp.GetStream();
            _reader = new StreamReader(stream, Encoding.UTF8);
            _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            Connected = true;
        }
    }

    public void Close()
    {
        lock (_lock)
        {
            Connected = false;
            _writer?.Dispose();
            _reader?.Dispose();
            _tcp?.Dispose();
            _writer = null;
            _reader = null;
            _tcp = null;
        }
    }

    public void WriteLine(string line)
    {
        lock (_lock)
        {
            if (!Connected || _writer == null) return;
            _writer.WriteLine(line);
        }
    }

    public string? ReadLine()
    {
        if (!Connected || _reader == null) return null;
        try
        {
            return _reader.ReadLine();
        }
        catch
        {
            Connected = false;
            return null;
        }
    }

    public void Dispose() => Close();
}
