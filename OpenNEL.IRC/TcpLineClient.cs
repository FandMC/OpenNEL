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

public class TcpLineClient : IDisposable
{
    readonly string _host;
    readonly int _port;
    
    TcpClient? _tcp;
    StreamReader? _reader;
    StreamWriter? _writer;

    public bool Connected => _tcp?.Connected ?? false;

    public TcpLineClient(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public void Connect()
    {
        _tcp = new TcpClient();
        _tcp.Connect(_host, _port);
        var stream = _tcp.GetStream();
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
    }

    public void Send(string line) => _writer?.WriteLine(line);

    public string? Read() => _reader?.ReadLine();

    public void Close()
    {
        _writer?.Dispose();
        _reader?.Dispose();
        _tcp?.Close();
        _tcp?.Dispose();
        _writer = null;
        _reader = null;
        _tcp = null;
    }

    public void Dispose() => Close();
}
