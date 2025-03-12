using COSXML.Auth;

public class CustomQCloudCredentialProvider : QCloudCredentialProvider
{
    private readonly string _secretId;
    private readonly string _secretKey;
    private readonly int _expiredTime;

    public CustomQCloudCredentialProvider(string secretId, string secretKey, int expiredTime)
    {
        if (string.IsNullOrEmpty(secretId))
            throw new ArgumentException("SecretId 不能为空。");
        if (string.IsNullOrEmpty(secretKey))
            throw new ArgumentException("SecretKey 不能为空。");
        if (expiredTime <= 0)
            throw new ArgumentException("ExpiredTime 必须大于 0。");

        _secretId = secretId;
        _secretKey = secretKey;
        _expiredTime = expiredTime;
    }

    public QCloudCredentials GetCredentials()
    {
        try
        {
            // 生成 keyTime
            long startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long endTimestamp = startTimestamp + _expiredTime;
            string keyTime = $"{startTimestamp};{endTimestamp}";

            // 输出调试信息
            Console.WriteLine($"生成的 keyTime: {keyTime}");

            // 返回 QCloudCredentials
            return new QCloudCredentials(_secretId, _secretKey, keyTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成 QCloudCredentials 失败: {ex.Message}");
            throw;
        }
    }

    public override void Refresh()
    {
        // 如果需要刷新认证信息，可以在这里实现
        Console.WriteLine("刷新 COS 认证信息...");
    }
}