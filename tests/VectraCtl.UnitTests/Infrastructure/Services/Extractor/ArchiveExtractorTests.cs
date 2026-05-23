using FluentAssertions;
using System.IO.Compression;
using System.Text;
using VectraCtl.Infrastructure.Services.Extractor;

namespace VectraCtl.UnitTests.Infrastructure.Services.Extractor;

public class ArchiveExtractorTests : IDisposable
{
    private readonly ArchiveExtractor _sut = new();
    private readonly string _tempDir;

    public ArchiveExtractorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── ZIP ───────────────────────────────────────────────────────────────────

    [Fact]
    public void ExtractArchive_Zip_ExtractsFile()
    {
        var zipPath = Path.Combine(_tempDir, "test.zip");
        var extractDir = Path.Combine(_tempDir, "out_zip");

        CreateZip(zipPath, ("hello.txt", "Hello world"));

        _sut.ExtractArchive(zipPath, extractDir);

        File.Exists(Path.Combine(extractDir, "hello.txt")).Should().BeTrue();
        File.ReadAllText(Path.Combine(extractDir, "hello.txt")).Should().Be("Hello world");
    }

    // ── TAR.GZ ────────────────────────────────────────────────────────────────

    [Fact]
    public void ExtractArchive_TarGz_ExtractsFile()
    {
        var archivePath = Path.Combine(_tempDir, "test.tar.gz");
        var extractDir = Path.Combine(_tempDir, "out_tgz");

        CreateTarGz(archivePath, ("hello.txt", "Hello tarball"));

        _sut.ExtractArchive(archivePath, extractDir);

        File.Exists(Path.Combine(extractDir, "hello.txt")).Should().BeTrue();
    }

    // ── TGZ ───────────────────────────────────────────────────────────────────

    [Fact]
    public void ExtractArchive_TgzExtension_ExtractsFile()
    {
        var archivePath = Path.Combine(_tempDir, "test.tgz");
        var extractDir = Path.Combine(_tempDir, "out_tgz2");

        CreateTarGz(archivePath, ("hello.txt", "Hello tgz"));

        _sut.ExtractArchive(archivePath, extractDir);

        File.Exists(Path.Combine(extractDir, "hello.txt")).Should().BeTrue();
    }

    // ── Unsupported extension ─────────────────────────────────────────────────

    [Fact]
    public void ExtractArchive_UnsupportedExtension_ThrowsNotSupported()
    {
        var act = () => _sut.ExtractArchive("archive.rar", _tempDir);
        act.Should().Throw<NotSupportedException>().WithMessage("*Unsupported*");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void CreateZip(string zipPath, (string name, string content) entry)
    {
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var zipEntry = archive.CreateEntry(entry.name);
        using var stream = zipEntry.Open();
        using var writer = new StreamWriter(stream);
        writer.Write(entry.content);
    }

    private static void CreateTarGz(string archivePath, (string name, string content) entry)
    {
        // Build a minimal POSIX tar entry in memory, then GZip-compress it.
        var contentBytes = Encoding.UTF8.GetBytes(entry.content);
        using var ms = new MemoryStream();

        // Tar header block = 512 bytes
        var header = new byte[512];
        // name (offset 0, 100 bytes)
        var nameBytes = Encoding.ASCII.GetBytes(entry.name);
        Array.Copy(nameBytes, header, Math.Min(nameBytes.Length, 99));
        // file mode (offset 100, 8 bytes)
        Array.Copy(Encoding.ASCII.GetBytes("0000644\0"), 0, header, 100, 8);
        // uid/gid (108/116)
        Array.Copy(Encoding.ASCII.GetBytes("0000000\0"), 0, header, 108, 8);
        Array.Copy(Encoding.ASCII.GetBytes("0000000\0"), 0, header, 116, 8);
        // file size in octal (offset 124, 12 bytes)
        var sizeOctal = Convert.ToString(contentBytes.Length, 8).PadLeft(11, '0') + " ";
        Array.Copy(Encoding.ASCII.GetBytes(sizeOctal), 0, header, 124, 12);
        // mtime (offset 136, 12 bytes)
        var mtime = Convert.ToString(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 8).PadLeft(11, '0') + " ";
        Array.Copy(Encoding.ASCII.GetBytes(mtime), 0, header, 136, 12);
        // type flag: regular file (offset 156)
        header[156] = (byte)'0';
        // ustar magic (offset 257)
        Array.Copy(Encoding.ASCII.GetBytes("ustar  \0"), 0, header, 257, 8);
        // Compute checksum (offset 148, 8 bytes): treat checksum field as spaces during calc
        for (int i = 148; i < 156; i++) header[i] = 0x20;
        int checksum = header.Sum(b => b);
        Array.Copy(Encoding.ASCII.GetBytes(Convert.ToString(checksum, 8).PadLeft(6, '0') + "\0 "), 0, header, 148, 8);

        ms.Write(header, 0, 512);
        ms.Write(contentBytes, 0, contentBytes.Length);
        // Pad content to 512-byte boundary
        int pad = (512 - (contentBytes.Length % 512)) % 512;
        ms.Write(new byte[pad], 0, pad);
        // Two 512-byte zero blocks as end-of-archive
        ms.Write(new byte[1024], 0, 1024);

        var tarBytes = ms.ToArray();
        using var outFile = File.Create(archivePath);
        using var gzip = new GZipStream(outFile, CompressionMode.Compress);
        gzip.Write(tarBytes, 0, tarBytes.Length);
    }
}
