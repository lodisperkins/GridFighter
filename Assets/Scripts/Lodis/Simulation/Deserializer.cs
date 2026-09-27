using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Wraps a <see cref="BinaryReader"/> used by rollback state loading. When deep
/// logs are enabled, every read records its type, byte range, and returned value.
/// </summary>
public sealed class Deserializer : IDisposable
{
    private const string DeepLogDirectory = "deserialize_logs";

    private readonly BinaryReader _reader;
    private readonly StreamWriter _logWriter;
    private readonly string _scope;
    private readonly bool _ownsLogWriter;

    /// <summary>
    /// Gets the underlying stream so callers can perform existing bounds checks.
    /// </summary>
    public Stream BaseStream => _reader.BaseStream;

    /// <summary>
    /// Creates the root deserializer for one rollback state load.
    /// </summary>
    public Deserializer(Stream stream)
        : this(new BinaryReader(stream), null, "GridGame", true)
    {
    }

    private Deserializer(BinaryReader reader, StreamWriter logWriter, string scope, bool createLogWriter)
    {
        _reader = reader;
        _scope = scope;

        if (createLogWriter && GridGameManager.DeepLogsEnabled)
        {
            string directory = Path.Combine(Application.dataPath, "..", DeepLogDirectory);
            Directory.CreateDirectory(directory);

            string filename = Path.Combine(
                directory,
                $"gridgame-deserialize-{GridGameManager.FrameNumber:D6}.log");
            _logWriter = new StreamWriter(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read));
            _logWriter.WriteLine($"Deserialize Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            _logWriter.WriteLine($"Frame Before Deserialize: {GridGameManager.FrameNumber}");
            _logWriter.WriteLine();
            _logWriter.Flush();
            _ownsLogWriter = true;
        }
        else
        {
            _logWriter = logWriter;
        }
    }

    /// <summary>
    /// Creates a bounded child reader for a nested item payload while preserving
    /// the same deep-log file as the root game-state deserialization.
    /// </summary>
    public Deserializer CreateChild(Stream stream, string scope)
    {
        string childScope = string.IsNullOrEmpty(_scope) ? scope : $"{_scope} > {scope}";
        return new Deserializer(new BinaryReader(stream), _logWriter, childScope, false);
    }

    public bool ReadBoolean()
    {
        long start = BaseStream.Position;
        bool value = _reader.ReadBoolean();
        LogRead("ReadBoolean", start, value);
        return value;
    }

    public byte ReadByte()
    {
        long start = BaseStream.Position;
        byte value = _reader.ReadByte();
        LogRead("ReadByte", start, value);
        return value;
    }

    public byte[] ReadBytes(int count)
    {
        long start = BaseStream.Position;
        byte[] value = _reader.ReadBytes(count);
        LogRead("ReadBytes", start, BitConverter.ToString(value));
        return value;
    }

    public int ReadInt32()
    {
        long start = BaseStream.Position;
        int value = _reader.ReadInt32();
        LogRead("ReadInt32", start, value);
        return value;
    }

    public long ReadInt64()
    {
        long start = BaseStream.Position;
        long value = _reader.ReadInt64();
        LogRead("ReadInt64", start, value);
        return value;
    }

    public string ReadString()
    {
        long start = BaseStream.Position;
        string value = _reader.ReadString();
        LogRead("ReadString", start, $"\"{value}\"");
        return value;
    }

    /// <summary>
    /// Writes a high-level marker into the active deep log without consuming bytes.
    /// </summary>
    public void LogMarker(string message)
    {
        if (_logWriter == null)
            return;

        _logWriter.WriteLine($"--- {message} ---");
        _logWriter.Flush();
    }

    private void LogRead(string readMethod, long start, object value)
    {
        if (_logWriter == null)
            return;

        _logWriter.WriteLine($"{_scope} | Bytes={start}-{BaseStream.Position} | {readMethod} | Value={value}");
        _logWriter.Flush();
    }

    public void Dispose()
    {
        _reader.Dispose();

        if (_ownsLogWriter)
        {
            _logWriter?.Dispose();
        }
    }
}
