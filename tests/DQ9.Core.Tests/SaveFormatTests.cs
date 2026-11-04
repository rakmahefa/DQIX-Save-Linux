using System;
using System.Linq;
using DQ9_Cheat.DataManager;
using DQ9_Cheat.GameData;
using Xunit;

namespace DQ9.Core.Tests;

/// <summary>
/// P0 reference tests for the binary save format: opening, validation,
/// byte-for-byte round-trips, CRC rules, both blocks and the intermission
/// cases, plus truncated/invalid inputs.
/// </summary>
public class SaveFormatTests
{
    // ---------------------------------------------------------------- open

    [Fact]
    public void CheckFile_AcceptsSignatureAtOffsetZero()
    {
        var bytes = SaveFileBuilder.BuildSaveFile();
        using var input = new MemoryStream(bytes);
        using var reader = new BinaryReader(input);
        Assert.Equal(0, SaveDataManager.Instance.CheckFile(reader));
    }

    [Fact]
    public void CheckFile_AcceptsSignatureAfterLeadHeader()
    {
        var bytes = SaveFileBuilder.BuildSaveFile(headerSize: 0x50);
        using var input = new MemoryStream(bytes);
        using var reader = new BinaryReader(input);
        Assert.Equal(0x50, SaveDataManager.Instance.CheckFile(reader));
    }

    [Fact]
    public void CheckFile_RejectsFilesShorterThanTwoBlocks()
    {
        var bytes = new byte[65535]; // minimum is 65536
        using var input = new MemoryStream(bytes);
        using var reader = new BinaryReader(input);
        Assert.Equal(-1, SaveDataManager.Instance.CheckFile(reader));
    }

    [Fact]
    public void CheckFile_RejectsMissingSignature()
    {
        var bytes = SaveFileBuilder.BuildSaveFile();
        var index = bytes.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes(SaveFileBuilder.Signature));
        for (var i = 0; i < SaveFileBuilder.Signature.Length; ++i)
            bytes[index + i] = 0x20;
        using var input = new MemoryStream(bytes);
        using var reader = new BinaryReader(input);
        Assert.Equal(-1, SaveDataManager.Instance.CheckFile(reader));
    }

    // ---------------------------------------------------------- round-trip

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(0x50, 0, 16, 1)]
    [InlineData(0, 1, 32, 1)]
    public void OpenThenSave_WithoutModification_IsByteForByteIdentical(
        int headerSize, byte intermissionBlock2, int footerSize, int _)
    {
        var original = SaveFileBuilder.BuildSaveFile(
            headerSize: headerSize,
            intermissionBlock2: intermissionBlock2,
            footerSize: footerSize);
        SaveFileBuilder.OpenSave(original);
        var saved = SaveFileBuilder.SaveToBytes(SaveDataManager.Instance);
        Assert.Equal(original, saved);
    }

    [Fact]
    public void OpenThenSave_RandomPayload_IsByteForByteIdentical()
    {
        // A fully randomised main region proves no hidden normalisation
        // happens on untouched data (CharacterManager.OnSaving and
        // AlchemyData.OnSaving are no-ops until the UI edits something).
        var original = SaveFileBuilder.BuildSaveFile(
            headerSize: 8,
            footerSize: 8,
            seed: 0xFEED,
            mutateBlock1: block =>
            {
                var random = new byte[28508];
                SaveFileBuilder.FillDeterministic(random, 0xCAFE);
                Array.Copy(random, 0, block, 136, random.Length);
            });
        SaveFileBuilder.OpenSave(original);
        var saved = SaveFileBuilder.SaveToBytes(SaveDataManager.Instance);
        Assert.Equal(original, saved);
    }

    // ----------------------------------------------------------------- CRC

    [Fact]
    public void SavedBlocks_ContainValidCrcFields()
    {
        var bytes = SaveFileBuilder.BuildSaveFile(footerSize: 4);
        SaveFileBuilder.OpenSave(bytes);
        var saved = SaveFileBuilder.SaveToBytes(SaveDataManager.Instance);
        var offset = 0; // no lead header in this fixture
        for (var blockIndex = 0; blockIndex < 2; ++blockIndex)
        {
            var block = offset + blockIndex * SaveFileBuilder.BlockSize;
            var crc1 = SaveFileBuilder.ReadInt32Le(saved, block + 16);
            var crc2 = SaveFileBuilder.ReadInt32Le(saved, block + 132);
            Assert.Equal((int)SaveFileBuilder.Crc32(saved, block + 20, 16), crc1);
            Assert.Equal((int)SaveFileBuilder.Crc32(saved, block + 136, 28508), crc2);
        }
    }

    [Fact]
    public void Save_RepairsCorruptedCrcFields()
    {
        var bytes = SaveFileBuilder.BuildSaveFile();
        bytes[16] ^= 0xFF; // corrupt block 1 CRC1
        bytes[132] ^= 0xFF; // corrupt block 1 CRC2
        SaveFileBuilder.OpenSave(bytes); // loader accepts corrupted CRCs
        var saved = SaveFileBuilder.SaveToBytes(SaveDataManager.Instance);
        Assert.Equal((int)SaveFileBuilder.Crc32(saved, 20, 16), SaveFileBuilder.ReadInt32Le(saved, 16));
        Assert.Equal((int)SaveFileBuilder.Crc32(saved, 136, 28508), SaveFileBuilder.ReadInt32Le(saved, 132));
    }

    // -------------------------------------------------------------- blocks

    [Fact]
    public void WithoutIntermissionData_IndexIsForcedBackToBlock1()
    {
        var bytes = SaveFileBuilder.BuildSaveFile(intermissionBlock2: 0);
        SaveFileBuilder.OpenSave(bytes);
        Assert.False(SaveDataManager.Instance.IsThereIntermissionData);
        SaveDataManager.Instance.SelectedDataIndex = 1;
        Assert.Equal(0, SaveDataManager.Instance.SelectedDataIndex);
    }

    [Fact]
    public void WithIntermissionData_Block2IsSelectable()
    {
        var bytes = SaveFileBuilder.BuildSaveFile(intermissionBlock2: 1);
        SaveFileBuilder.OpenSave(bytes);
        Assert.True(SaveDataManager.Instance.IsThereIntermissionData);
        SaveDataManager.Instance.SelectedDataIndex = 1;
        Assert.Equal(1, SaveDataManager.Instance.SelectedDataIndex);
        Assert.Same(SaveDataManager.Instance.GetSaveData(1), SaveDataManager.Instance.SaveData);
    }

    [Fact]
    public void BothBlocks_AreReadFromTheFile()
    {
        // Distinct seeds make the two block payloads differ.
        var bytes = SaveFileBuilder.BuildSaveFile(intermissionBlock2: 1);
        SaveFileBuilder.OpenSave(bytes);
        var data0 = SaveDataManager.Instance.GetSaveData(0).GetData();
        var data1 = SaveDataManager.Instance.GetSaveData(1).GetData();
        Assert.NotEqual(data0, data1);
        // Each block keeps its own intermission flag (offset 16 in GetData,
        // i.e. raw offset 20).
        Assert.Equal(0, data0[20 - 16]);
        Assert.Equal(1, data1[20 - 16]);
    }

    [Fact]
    public void CopyIntermissionData_CopiesBlock1OverBlock2()
    {
        var bytes = SaveFileBuilder.BuildSaveFile();
        SaveFileBuilder.OpenSave(bytes);
        // Random fixture data rarely holds a valid save-place index; assign
        // one from the reference table first (real saves always do). Copy
        // reads the SOURCE block (0), whatever the current selection is.
        var place = SavePlaceList.GetList(SavePlaceType.All, null)[0];
        SaveDataManager.Instance.GetSaveData(0).BasisData.SavePlace = place;
        var data0 = SaveDataManager.Instance.GetSaveData(0).GetData();
        SaveDataManager.Instance.CopyIntermissionData(0);
        var data1 = SaveDataManager.Instance.GetSaveData(1).GetData();
        Assert.Equal(1, SaveDataManager.Instance.GetSaveData(1).IsIntermission.Value);
        Assert.Equal(place.Index,
            SaveDataManager.Instance.GetSaveData(1).BasisData.IntermissionPlace.Index);
        // The flag (20), the intermission place (27104..27105) and the
        // intermission coordinates (27108..27119) live inside the copied
        // region but are re-derived by the copy: block 2 must match block 1
        // everywhere else.
        Assert.Equal(1, data1[20 - 16]);
        Assert.Equal(0, data0[20 - 16]);
        for (var i = 0; i < data0.Length; ++i)
        {
            var raw = i + 16;
            if (raw == 20 || (raw >= 27104 && raw < 27106) || (raw >= 27108 && raw < 27120))
                continue;
            Assert.Equal(data0[i], data1[i]);
        }
    }

    [Fact]
    public void CopyIntermissionData_FromBlock2WithoutFlag_IsIgnored()
    {
        var bytes = SaveFileBuilder.BuildSaveFile();
        SaveFileBuilder.OpenSave(bytes);
        var data1Before = SaveDataManager.Instance.GetSaveData(1).GetData();
        SaveDataManager.Instance.CopyIntermissionData(1);
        Assert.Equal(data1Before, SaveDataManager.Instance.GetSaveData(1).GetData());
    }

    [Fact]
    public void RemoveIntermissionData_ClearsTheFlag()
    {
        var bytes = SaveFileBuilder.BuildSaveFile(intermissionBlock2: 1);
        SaveFileBuilder.OpenSave(bytes);
        SaveDataManager.Instance.RemoveIntermissionData();
        Assert.Equal(0, SaveDataManager.Instance.GetSaveData(1).IsIntermission.Value);
        Assert.False(SaveDataManager.Instance.IsThereIntermissionData);
    }

    // -------------------------------------------------------------- footer

    [Fact]
    public void Footer_IsPreservedVerbatimOnSave()
    {
        var bytes = SaveFileBuilder.BuildSaveFile(footerSize: 24);
        SaveFileBuilder.OpenSave(bytes);
        var saved = SaveFileBuilder.SaveToBytes(SaveDataManager.Instance);
        var expectedFooter = bytes.Skip(bytes.Length - 24);
        var actualFooter = saved.Skip(saved.Length - 24);
        Assert.Equal(expectedFooter, actualFooter);
        Assert.Equal(bytes.Length, saved.Length);
    }

    // ------------------------------------------------------------ truncate

    [Theory]
    [InlineData(0)] // empty
    [InlineData(15)] // signature cut short
    [InlineData(32768)] // one block only
    [InlineData(65535)] // one byte short of two blocks
    public void TruncatedFiles_AreRejected(int size)
    {
        var full = SaveFileBuilder.BuildSaveFile();
        var bytes = full.Take(size).ToArray();
        using var input = new MemoryStream(bytes);
        using var reader = new BinaryReader(input);
        Assert.Equal(-1, SaveDataManager.Instance.CheckFile(reader));
    }
}
