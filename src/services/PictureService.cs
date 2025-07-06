using System.Text.Json;
using ConsoleApp1.models;
using ConsoleApp1.utils;
using COSXML;
using COSXML.Auth;
using COSXML.Model.Bucket;
using COSXML.Model.Object;
using COSXML.Model.Service;
using COSXML.Transfer;
using COSXML.Utils;
using MyBot.Api;
using MyBot.Models.MessageModels;



public static class CloudObjectStorage
{
    private static readonly CosConfig? Config;
    private static readonly CosXml? CosServiceProvider;
    private static readonly TransferManager? TransferManager;
    private static readonly string CosConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "机器人",
        "CosConfig.json"
    );


    // 静态构造函数
    static CloudObjectStorage()
    {
        if (!File.Exists(CosConfigPath))
        {
            throw new FileNotFoundException($"配置文件未找到: {CosConfigPath}");
        }

        try
        {
            var json = File.ReadAllText(CosConfigPath);
            Config = JsonSerializer.Deserialize<CosConfig>(json);
            Console.WriteLine($"SecretId: {Config.SecretId}, SecretKey: {Config.SecretKey}, ExpiredTime: {Config.ExpiredTime}");
            if (Config == null)
            {
                throw new InvalidOperationException("配置文件解析失败，返回 null。");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"配置文件读取或解析失败: {ex.Message}", ex);
        }
        
        try
        {
            CosServiceProvider = new CosXmlServer( 
                new CosXmlConfig.Builder()
                .IsHttps(Config.IsHttps)
                .SetRegion(Config.Region)
                .SetDebugLog(Config.DebugLog)
                .Build(),new DefaultQCloudCredentialProvider(Config.SecretId, Config.SecretKey, Config.ExpiredTime)
                );
            var credentialProvider = new CustomQCloudCredentialProvider(Config.SecretId, Config.SecretKey, Config.ExpiredTime);
            Console.WriteLine("Credential:"+credentialProvider);

            if (CosServiceProvider == null)
            {
                throw new InvalidOperationException("COS服务初始化失败，返回 null。");
            }

            var transferConfig = new TransferConfig();
            TransferManager = new TransferManager(CosServiceProvider, transferConfig);

            if (TransferManager == null)
            {
                throw new InvalidOperationException("传输管理器初始化失败，返回 null。");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"COS服务或传输管理器初始化失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 上传文件到 COS
    /// </summary>
    public static async Task<UploadedResult> UploadFileAsync(string filePath)
    {
        if (CosServiceProvider is null || Config is null || TransferManager is null)
        {
            throw new InvalidOperationException("服务初始化失败。");
        }

        var fileExtension = Path.GetExtension(filePath);
        var newFilePath = $"{Guid.NewGuid()}{fileExtension}";

        var uploadTask = new COSXMLUploadTask(Config.Bucket, newFilePath);
        uploadTask.SetSrcPath(filePath);

        uploadTask.progressCallback = (completed, total) =>
        {
            Console.WriteLine($"上传进度: {completed * 100.0 / total:##.##}%");
        };

        try
        {
            var uploadResult = await TransferManager.UploadAsync(uploadTask);
            Console.WriteLine($"图片上传成功: {uploadResult.GetResultInfo()}");
            return new UploadedResult($"{Config.Link}/{newFilePath}", uploadResult.eTag);
        }
        catch (Exception e)
        {
            Console.WriteLine($"上传失败: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    public static async Task DownloadFileAsync(string cosPath, string localPath)
    {
        if (CosServiceProvider is null || Config is null || TransferManager is null)
        {
            throw new InvalidOperationException("服务初始化失败。");
        }

        var downloadTask = new COSXMLDownloadTask(Config.Bucket, cosPath, Path.GetDirectoryName(localPath), Path.GetFileName(localPath));

        downloadTask.progressCallback = (completed, total) =>
        {
            Console.WriteLine($"下载进度: {completed * 100.0 / total:##.##}%");
        };

        try
        {
            var result = await TransferManager.DownloadAsync(downloadTask);
            Console.WriteLine($"文件下载成功: {result.GetResultInfo()}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"下载失败: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// 查询存储桶列表
    /// </summary>
    public static void ListBuckets()
    {
        if (CosServiceProvider is null)
        {
            throw new InvalidOperationException("服务未初始化");
        }

        try
        {
            var request = new GetServiceRequest();
            var result = CosServiceProvider.GetService(request);
            foreach (var bucket in result.listAllMyBuckets.buckets)
            {
                Console.WriteLine($"Bucket: {bucket.name}, Location: {bucket.location}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"查询存储桶失败: {e.Message}");
        }
    }

    /// <summary>
    /// 获取存储桶内的对象列表
    /// </summary>
    public static void ListObjects()
    {
        if (CosServiceProvider is null || Config is null)
        {
            throw new InvalidOperationException("服务未初始化");
        }

        try
        {
            var request = new GetBucketRequest(Config.Bucket);
            var result = CosServiceProvider.GetBucket(request);
            foreach (var obj in result.listBucket.contentsList)
            {
                Console.WriteLine($"对象: {obj.key}, 大小: {obj.size} 字节");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"获取对象列表失败: {e.Message}");
        }
    }
    public static async Task<ChatMessageResp> SendGroupMessage(
        this ChatMessageApi @this,
        ChatMessage message,
        string imageUrl,
        int messageSequenceValue = 1
    )
    {
        var response = await @this.SendGroupMediaAsync(message.GroupOpenId, resourceUrl: imageUrl);
        return await @this.SendGroupMessageAsync(
            message.GroupOpenId,
            string.Empty,
            ChatMessageType.Media,
            media: new() { FileInfo = response.FileInfo },
            passiveMsgId: message.Id,
            msgSeq: messageSequenceValue);
    }
}

