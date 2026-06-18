using System.Net;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Infrastructure.FileAccess.Providers;

public sealed class FtpFileProvider : IFileProvider, ICredentialSupported
{
    private string? _username;
    private string? _password;

    public bool CanHandle(string path)
    {
        return path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);
    }

    public void SetCredentials(string? username, string? password)
    {
        _username = username;
        _password = password;
    }

    public bool Exists(string filePath)
    {
        try
        {
            using var response = (FtpWebResponse)CreateRequest(filePath, WebRequestMethods.Ftp.GetFileSize).GetResponse();
            return response.StatusCode == FtpStatusCode.FileStatus;
        }
        catch (WebException ex) when ((ex.Response as FtpWebResponse)?.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
        {
            return false;
        }
    }

    public async Task<Stream> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(filePath, WebRequestMethods.Ftp.DownloadFile);
        using var _ = cancellationToken.Register(request.Abort);
        var response = (FtpWebResponse)await request.GetResponseAsync().ConfigureAwait(false);
        return response.GetResponseStream() ?? Stream.Null;
    }

    public async Task SaveFileAsync(string filePath, Stream content, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        if (!overwrite && Exists(filePath))
        {
            throw new IOException($"FTP file already exists: {filePath}");
        }

        var request = CreateRequest(filePath, WebRequestMethods.Ftp.UploadFile);
        using var _ = cancellationToken.Register(request.Abort);
        await using var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false);
        await content.CopyToAsync(requestStream, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<string>> GetFileNamesAsync(string directoryPath, string searchPattern = "*.*", bool recursive = false, CancellationToken cancellationToken = default)
    {
        var normalizedPath = directoryPath.EndsWith("/", StringComparison.Ordinal) ? directoryPath : $"{directoryPath}/";
        var request = CreateRequest(normalizedPath, WebRequestMethods.Ftp.ListDirectory);
        var result = new List<string>();

        using var _ = cancellationToken.Register(request.Abort);
        using var response = (FtpWebResponse)await request.GetResponseAsync().ConfigureAwait(false);
        using var stream = response.GetResponseStream();
        if (stream == null)
        {
            return result;
        }

        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            var fileName = Path.GetFileName(line);
            if (IsMatch(fileName, searchPattern))
            {
                result.Add($"{normalizedPath}{fileName}");
            }
        }

        return result;
    }

    public async Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        if (overwrite && Exists(destinationPath))
        {
            await DeleteFileAsync(destinationPath, cancellationToken).ConfigureAwait(false);
        }

        var request = CreateRequest(sourcePath, WebRequestMethods.Ftp.Rename);
        request.RenameTo = destinationPath;
        using var _ = cancellationToken.Register(request.Abort);
        using var response = await request.GetResponseAsync().ConfigureAwait(false);
    }

    public async Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(filePath, WebRequestMethods.Ftp.DeleteFile);
        using var _ = cancellationToken.Register(request.Abort);
        using var response = await request.GetResponseAsync().ConfigureAwait(false);
    }

    private FtpWebRequest CreateRequest(string path, string method)
    {
        var request = (FtpWebRequest)WebRequest.Create(path.Replace("\\", "/", StringComparison.Ordinal));
        request.Method = method;
        request.UseBinary = true;
        request.KeepAlive = false;
        if (!string.IsNullOrWhiteSpace(_username))
        {
            request.Credentials = new NetworkCredential(_username, _password);
        }

        return request;
    }

    private static bool IsMatch(string fileName, string searchPattern)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(searchPattern) || searchPattern is "*" or "*.*")
        {
            return true;
        }

        return fileName.EndsWith(searchPattern.Replace("*", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase);
    }
}

