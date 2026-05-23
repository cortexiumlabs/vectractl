using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Octokit;
using VectraCtl.Infrastructure.Services.Github;

namespace VectraCtl.UnitTests.Infrastructure.Services.Github;

public class GitHubReleaseManagerTests : IDisposable
{
    private readonly IGitHubClient _client;
    private readonly HttpClient _httpClient;
    private readonly GitHubReleaseManager _sut;
    private readonly string _tempDir;

    public GitHubReleaseManagerTests()
    {
        _client = Substitute.For<IGitHubClient>();
        _httpClient = new HttpClient();
        _sut = new GitHubReleaseManager(_client, _httpClient);

        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── GetLatestVersion ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetLatestVersion_ReturnsTagName()
    {
        var release = CreateRelease("v1.2.3");
        _client.Repository.Release.GetLatest("owner", "repo")
               .Returns(release);

        var version = await _sut.GetLatestVersion("owner", "repo");
        version.Should().Be("v1.2.3");
    }

    // ── GetAssetHashCode ─────────────────────────────────────────────────────

    [Fact]
    public void GetAssetHashCode_FileExists_ReturnsSha256Hex()
    {
        var file = Path.Combine(_tempDir, "content.bin");
        File.WriteAllBytes(file, new byte[] { 1, 2, 3 });

        var hash = _sut.GetAssetHashCode(file);

        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void GetAssetHashCode_EmptyFile_ReturnsSha256OfEmpty()
    {
        var file = Path.Combine(_tempDir, "empty.bin");
        File.WriteAllBytes(file, Array.Empty<byte>());

        var hash = _sut.GetAssetHashCode(file);

        hash.Should().HaveLength(64);
    }

    // ── ValidateDownloadedAsset ───────────────────────────────────────────────

    [Fact]
    public void ValidateDownloadedAsset_ValidHash_ReturnsTrue()
    {
        var content = new byte[] { 10, 20, 30 };
        var downloadedFile = Path.Combine(_tempDir, "download.bin");
        File.WriteAllBytes(downloadedFile, content);

        var hash = _sut.GetAssetHashCode(downloadedFile);

        var hashFile = Path.Combine(_tempDir, "hash.sha256");
        File.WriteAllText(hashFile, hash + "  filename");

        _sut.ValidateDownloadedAsset(downloadedFile, hashFile).Should().BeTrue();
    }

    [Fact]
    public void ValidateDownloadedAsset_InvalidHash_ReturnsFalse()
    {
        var downloadedFile = Path.Combine(_tempDir, "download2.bin");
        File.WriteAllBytes(downloadedFile, new byte[] { 1, 2, 3 });

        var hashFile = Path.Combine(_tempDir, "hash2.sha256");
        File.WriteAllText(hashFile, "0000000000000000000000000000000000000000000000000000000000000000  filename");

        _sut.ValidateDownloadedAsset(downloadedFile, hashFile).Should().BeFalse();
    }

    [Fact]
    public void ValidateDownloadedAsset_HashFileHasOnlyHash_ReturnsTrue()
    {
        var content = new byte[] { 5, 6, 7 };
        var downloadedFile = Path.Combine(_tempDir, "download3.bin");
        File.WriteAllBytes(downloadedFile, content);

        var hash = _sut.GetAssetHashCode(downloadedFile);

        var hashFile = Path.Combine(_tempDir, "hash3.sha256");
        File.WriteAllText(hashFile, hash);

        _sut.ValidateDownloadedAsset(downloadedFile, hashFile).Should().BeTrue();
    }

    // ── DownloadHashAsset ─────────────────────────────────────────────────────

    [Fact]
    public async Task DownloadHashAsset_DelegatesToDownloadAsset()
    {
        var release = CreateRelease("v1.0.0");
        var assetUrl = "https://example.com/hash.sha256";
        var asset = new ReleaseAsset(
            url: assetUrl,
            id: 1,
            nodeId: "nodeid",
            name: "hash.sha256",
            label: string.Empty,
            state: "uploaded",
            contentType: "text/plain",
            size: 10,
            downloadCount: 0,
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow,
            browserDownloadUrl: assetUrl,
            uploader: new Author());

        var releaseWithAsset = new Release(
            release.Url, release.HtmlUrl, release.AssetsUrl, release.UploadUrl,
            release.Id, release.NodeId, release.TagName, release.TargetCommitish,
            release.Name, release.Body, release.Draft, release.Prerelease,
            release.CreatedAt, release.PublishedAt, new Author(), release.TarballUrl,
            release.ZipballUrl, new List<ReleaseAsset> { asset });

        _client.Repository.Release.Get("owner", "repo", "v1.0.0").Returns(releaseWithAsset);

        using var handler = new FakeHttpMessageHandler(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[] { 1, 2, 3 })
        });
        using var httpClient = new HttpClient(handler);
        var sut = new GitHubReleaseManager(_client, httpClient);

        var dest = Path.Combine(_tempDir, "output.sha256");
        var result = await sut.DownloadHashAsset("owner", "repo", "hash.sha256", dest, "v1.0.0");

        result.Should().Be(dest);
        File.Exists(dest).Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public FakeHttpMessageHandler(HttpResponseMessage response) => _response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(_response);
    }

    private static Release CreateRelease(string tag)
    {
        // Use Octokit's factory or reflection to create a Release with a tag
        return new Release(
            url: "https://api.github.com/repos/owner/repo/releases/1",
            htmlUrl: "https://github.com/owner/repo/releases/tag/" + tag,
            assetsUrl: "https://api.github.com/repos/owner/repo/releases/1/assets",
            uploadUrl: string.Empty,
            id: 1,
            nodeId: "node1",
            tagName: tag,
            targetCommitish: "main",
            name: tag,
            body: string.Empty,
            draft: false,
            prerelease: false,
            createdAt: DateTimeOffset.UtcNow,
            publishedAt: DateTimeOffset.UtcNow,
            author: new Author(),
            tarballUrl: string.Empty,
            zipballUrl: string.Empty,
            assets: new List<ReleaseAsset>()
        );
    }
}
