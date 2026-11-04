using System;
using System.IO;
using DQ9_Cheat.DataManager;
using Xunit;

namespace DQ9.Core.Tests;

/// <summary>
/// Builds synthetic DQ9 save files in memory. No personal save data is
/// committed; every fixture is generated from seeded random payloads.
///
/// File layout (documented in docs/save-format.md):
///   [0..H)     optional lead header (H = signature offset, typically 0)
///   [H..H+32768)  block 1 — 32768 bytes whose first 15 bytes are the
///                 "DRAGON QUEST IX" signature (CheckFile returns H and
///                 ReadData reads both blocks starting at that offset)
///   [H+32768..H+65536) block 2, same 32768-byte structure
///   rest       footer, preserved verbatim on save
///
/// Inside every block (offsets are relative to the block start):
///   [  0.. 15) "DRAGON QUEST IX" signature
///   [ 15]      unknown/reserved byte, preserved
///   [ 16.. 20) CRC32 of bytes [20..36)   (little-endian)
///   [ 20]      IsIntermission flag (clamped to 0/1 on read)
///   [132..136) CRC32 of bytes [136..28644) (little-endian)
///   [136..28644) main data region
/// </summary>
internal static class SaveFileBuilder
{
    public const int BlockSize = 32768;
    public const int BlockCount = 2;
    public const string Signature = "DRAGON QUEST IX";

    /// <summary>CRC32 of the legacy implementation (polynomial 0xEDB88320).</summary>
    public static uint Crc32(byte[] data, int offset, int length)
    {
        var table = new uint[256];
        for (var i = 0; i < 256; ++i)
        {
            uint value = (uint)i;
            for (var bit = 0; bit < 8; ++bit)
                value = (value & 1) != 0 ? 0xEDB88320u ^ (value >> 1) : value >> 1;
            table[i] = value;
        }

        uint crc = uint.MaxValue;
        for (uint i = 0; i < length; ++i)
            crc = table[(int)((crc ^ data[offset + i]) & 0xFF)] ^ (crc >> 8);
        return crc ^ uint.MaxValue;
    }

    /// <summary>Builds one 32768-byte block with valid CRC fields.</summary>
    public static byte[] BuildBlock(ushort seed, byte intermission = 0, Action<byte[]> mutate = null)
    {
        var block = new byte[BlockSize];
        var signature = System.Text.Encoding.ASCII.GetBytes(Signature);
        Array.Copy(signature, block, signature.Length);
        var rest = new byte[BlockSize - signature.Length];
        FillDeterministic(rest, seed);
        Array.Copy(rest, 0, block, signature.Length, rest.Length);
        if (mutate != null)
            mutate(block);
        block[20] = intermission;
        WriteInt32Le(block, 16, (int)Crc32(block, 20, 16));
        WriteInt32Le(block, 132, (int)Crc32(block, 136, 28508));
        return block;
    }

    /// <summary>Builds a complete save file with valid CRCs.</summary>
    public static byte[] BuildSaveFile(
        int headerSize = 0,
        byte intermissionBlock1 = 0,
        byte intermissionBlock2 = 0,
        int footerSize = 0,
        ushort seed = 0x1234,
        Action<byte[]> mutateBlock1 = null)
    {
        using var ms = new MemoryStream();
        if (headerSize > 0)
        {
            var header = new byte[headerSize];
            FillDeterministic(header, (ushort)(seed ^ 0xBEEF));
            ms.Write(header, 0, header.Length);
        }

        var block1 = BuildBlock(seed, intermissionBlock1, mutateBlock1);
        var block2 = BuildBlock((ushort)(seed ^ 0x5555), intermissionBlock2);
        ms.Write(block1, 0, block1.Length);
        ms.Write(block2, 0, block2.Length);
        if (footerSize > 0)
        {
            var footer = new byte[footerSize];
            FillDeterministic(footer, (ushort)(seed ^ 0xF00D));
            ms.Write(footer, 0, footer.Length);
        }

        return ms.ToArray();
    }

    /// <summary>Opens a save file through the production path (CheckFile + seek + ReadData).</summary>
    public static SaveDataManager OpenSave(byte[] fileBytes, out int headerLength)
    {
        var manager = SaveDataManager.Instance;
        using var input = new MemoryStream(fileBytes);
        using var reader = new BinaryReader(input);
        headerLength = manager.CheckFile(reader);
        Assert.True(headerLength >= 0, "CheckFile rejected the fixture");
        input.Position = headerLength;
        manager.ReadData(reader);
        return manager;
    }

    public static SaveDataManager OpenSave(byte[] fileBytes)
    {
        return OpenSave(fileBytes, out _);
    }

    /// <summary>Serializes a manager through the production WriteData path.</summary>
    public static byte[] SaveToBytes(SaveDataManager manager)
    {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);
        manager.WriteData(writer);
        return output.ToArray();
    }

    /// <summary>Deterministic pseudo-random fill (xorshift), so fixtures never depend on Random.</summary>
    public static void FillDeterministic(byte[] buffer, ushort seed)
    {
        uint state = seed == 0 ? 0x9E3779B9u : seed;
        for (var i = 0; i < buffer.Length; ++i)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            buffer[i] = (byte)(state & 0xFF);
        }
    }

    public static void WriteInt32Le(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }

    public static int ReadInt32Le(byte[] buffer, int offset)
    {
        return buffer[offset]
               | (buffer[offset + 1] << 8)
               | (buffer[offset + 2] << 16)
               | (buffer[offset + 3] << 24);
    }
}
