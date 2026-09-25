using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Checks GitHub Releases for a newer published version on launch, and downloads this platform's build of it.
/// Releases come from the ElectroGrid build window: tag v&lt;version&gt; with ElectroGrid-&lt;version&gt;.apk and
/// ElectroGrid-&lt;version&gt;-Windows.zip attached. Drafts and prereleases are ignored, so a release is only
/// offered once it has been published.
/// </summary>
[DisallowMultipleComponent]
public class GameUpdater : MonoBehaviour
{
    private const string Repository = "danielnoam/ElectroGrid";
    private const string LatestReleaseUrl = "https://api.github.com/repos/" + Repository + "/releases/latest";
    private const string UpdatesFolderName = "Updates";
    private const int RequestTimeoutSeconds = 20;

    public enum State
    {
        Idle,
        Checking,
        UpToDate,
        UpdateAvailable,
        Downloading,
        ReadyToInstall,
        Failed
    }

    [Serializable]
    public class Release
    {
        public string tag_name;
        public string name;
        public string body;
        public string html_url;
        public bool draft;
        public bool prerelease;
        public Asset[] assets;
    }

    [Serializable]
    public class Asset
    {
        public string name;
        public long size;
        public string browser_download_url;
        public string digest;
    }

    public static GameUpdater Instance { get; private set; }

    public State CurrentState { get; private set; }
    public Release LatestRelease { get; private set; }
    public Asset PlatformAsset { get; private set; }
    public string LatestVersion => TrimVersion(LatestRelease?.tag_name);
    public string DownloadedPath { get; private set; }
    public float DownloadProgress { get; private set; }
    public string ErrorMessage { get; private set; }

    /// <summary>Installing only works from a build on Android or Windows; elsewhere the release page is offered instead.</summary>
    public static bool CanInstallOnThisPlatform =>
        !Application.isEditor && (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.WindowsPlayer);

    public event Action StateChanged;

    private static string UpdatesFolder => Path.Combine(Application.persistentDataPath, UpdatesFolderName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance) return;

        var go = new GameObject(nameof(GameUpdater));
        go.AddComponent<GameUpdater>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ClearOldDownloads();
        StartCoroutine(CheckForUpdate());
    }

    public void StartDownload()
    {
        if (CurrentState != State.UpdateAvailable && CurrentState != State.Failed) return;
        if (PlatformAsset == null) return;

        StartCoroutine(Download());
    }

    public UpdateInstaller.Result Install()
    {
        if (CurrentState != State.ReadyToInstall) return UpdateInstaller.Result.Failed("Nothing downloaded to install.");

        var result = UpdateInstaller.Install(DownloadedPath);
        if (result.Outcome == UpdateInstaller.Outcome.Failed) Fail(result.Message);
        return result;
    }

    public void OpenReleasePage()
    {
        string url = LatestRelease?.html_url;
        Application.OpenURL(string.IsNullOrEmpty(url) ? $"https://github.com/{Repository}/releases/latest" : url);
    }

    private IEnumerator CheckForUpdate()
    {
        SetState(State.Checking);

        using var request = UnityWebRequest.Get(LatestReleaseUrl);
        request.timeout = RequestTimeoutSeconds;
        request.SetRequestHeader("Accept", "application/vnd.github+json");

        yield return request.SendWebRequest();

        // A failed check is not worth bothering the player about, the game works fine offline
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Update check failed: {request.error}");
            SetState(State.Idle);
            yield break;
        }

        Release release;
        try
        {
            release = JsonUtility.FromJson<Release>(request.downloadHandler.text);
        }
        catch (ArgumentException e)
        {
            Debug.LogWarning($"Update check returned unreadable data: {e.Message}");
            SetState(State.Idle);
            yield break;
        }

        if (release == null || release.draft || release.prerelease || !IsNewer(release.tag_name, Application.version))
        {
            SetState(State.UpToDate);
            yield break;
        }

        LatestRelease = release;
        PlatformAsset = FindPlatformAsset(release);
        SetState(State.UpdateAvailable);
    }

    private IEnumerator Download()
    {
        var asset = PlatformAsset;
        Directory.CreateDirectory(UpdatesFolder);
        string path = Path.Combine(UpdatesFolder, asset.name);
        if (File.Exists(path)) File.Delete(path);

        DownloadProgress = 0f;
        SetState(State.Downloading);

        using (var request = new UnityWebRequest(asset.browser_download_url, UnityWebRequest.kHttpVerbGET))
        {
            request.downloadHandler = new DownloadHandlerFile(path) { removeFileOnAbort = true };
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                DownloadProgress = request.downloadProgress;
                StateChanged?.Invoke();
                yield return null;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Fail($"Download failed: {request.error}");
                yield break;
            }
        }

        DownloadProgress = 1f;

        long size = File.Exists(path) ? new FileInfo(path).Length : 0;
        if (asset.size > 0 && size != asset.size)
        {
            Fail($"Download is incomplete ({size} of {asset.size} bytes).");
            yield break;
        }

        // Hashing a large build takes a moment, so it runs off the main thread
        var hashTask = Task.Run(() => Sha256(path));
        while (!hashTask.IsCompleted) yield return null;

        if (!DigestMatches(asset.digest, hashTask.Result))
        {
            File.Delete(path);
            Fail("Download failed its checksum. Try again.");
            yield break;
        }

        DownloadedPath = path;
        SetState(State.ReadyToInstall);
    }

    private static Asset FindPlatformAsset(Release release)
    {
        if (release.assets == null) return null;

        // The editor has nothing to install, but shows the Windows build so the window can be tried out
        string suffix = Application.platform == RuntimePlatform.Android ? ".apk" : "-Windows.zip";
        return release.assets.FirstOrDefault(asset => asset.name != null && asset.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsNewer(string tag, string current)
    {
        return Version.TryParse(TrimVersion(tag), out var latest)
               && Version.TryParse(TrimVersion(current), out var installed)
               && latest > installed;
    }

    private static string TrimVersion(string version) => (version ?? string.Empty).Trim().TrimStart('v', 'V');

    /// <summary>GitHub publishes each asset's hash as "sha256:&lt;hex&gt;". Older releases without one are checked by size only.</summary>
    private static bool DigestMatches(string digest, string sha256)
    {
        const string prefix = "sha256:";
        if (string.IsNullOrEmpty(digest) || !digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;

        return string.Equals(digest.Substring(prefix.Length), sha256, StringComparison.OrdinalIgnoreCase);
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(stream).Select(b => b.ToString("x2")));
    }

    /// <summary>A download is only needed until it is installed, and a relaunch means it either was or was abandoned.</summary>
    private static void ClearOldDownloads()
    {
        try
        {
            if (Directory.Exists(UpdatesFolder)) Directory.Delete(UpdatesFolder, true);
        }
        catch (IOException e)
        {
            Debug.LogWarning($"Could not clear old updates: {e.Message}");
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.LogWarning($"Could not clear old updates: {e.Message}");
        }
    }

    private void Fail(string message)
    {
        ErrorMessage = message;
        Debug.LogWarning($"Update: {message}");
        SetState(State.Failed);
    }

    private void SetState(State state)
    {
        CurrentState = state;
        StateChanged?.Invoke();
    }
}
