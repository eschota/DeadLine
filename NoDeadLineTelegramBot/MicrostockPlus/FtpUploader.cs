using System;
using System.IO;
using System.Net;

public class FtpUploader
{
    private string _ftpUrl;
    private string _ftpUsername;
    private string _ftpPassword;

    public FtpUploader(string ftpUrl, string ftpUsername, string ftpPassword)
    {
        _ftpUrl = ftpUrl;
        _ftpUsername = ftpUsername;
        _ftpPassword = ftpPassword;
    }

    public bool UploadFile(string localFilePath, string remoteFileName)
    {
        try
        {
            var request = (FtpWebRequest)WebRequest.Create($"{_ftpUrl}/{remoteFileName}");
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;

            using (var fileStream = new FileStream(localFilePath, FileMode.Open))
            using (var requestStream = request.GetRequestStream())
            {
                fileStream.CopyTo(requestStream);
            }

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                Console.WriteLine($"Upload File Complete, status {response.StatusDescription}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }
} 

