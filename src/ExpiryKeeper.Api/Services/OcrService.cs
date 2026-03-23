using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;
using System.Text.Json;

namespace MedicineExpiration.Api.Services;

public record OcrMedicineResult(string? Name, string? ExpireDate, string? Manufacturer, string? Category, string RawText);

public class OcrService
{
    private const string SystemPrompt = """
        你是一个商品保质期信息识别助手，专门处理药品、食品、日用品等各类商品包装。
        请从图片中提取以下信息，以 JSON 格式返回：
        {
          "name": "商品名称（优先使用包装正面最大字体的商品名，去掉规格/净含量/数量等修饰词，保留品牌名+品类名，例如：'999感冒灵颗粒'、'农夫山泉矿泉水'、'飘柔洗发水'）",
          "expireDate": "保质期/有效期截止日期，格式 YYYY-MM-DD；若只有年月则取该月最后一天；若包装同时标注了生产日期和保质期时长（如'保质期12个月'、'保质期365天'），请用生产日期加上保质期时长计算出截止日期；无法识别则 null",
          "manufacturer": "生产厂家/品牌商，无法识别则 null",
          "category": "商品分类，只能从以下选项中选一个：药品、食品、日用品、保健品、其他"
        }
        注意：
        - 名称不要包含 'ml'、'g'、'片'、'粒'、'瓶' 等规格单位
        - 有效期/保质期 与 生产日期 不同，不要混淆；但如果只有生产日期+保质期时长，需要计算出截止日期
        - 若只有年月（如 2025-06），取该月最后一天（2025-06-30）
        - 只返回 JSON，不要任何其他文字或 markdown 代码块
        """;

    private readonly ChatClient _chatClient;
    private readonly ILogger<OcrService> _logger;

    public OcrService(IConfiguration config, ILogger<OcrService> logger)
    {
        _logger = logger;
        var endpoint = config["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
        var deployment = config["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
        _chatClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            .GetChatClient(deployment);
    }

    public async Task<OcrMedicineResult> AnalyzeMultipleAsync(
        IReadOnlyList<(Stream Stream, string ContentType)> images,
        CancellationToken ct = default)
    {
        if (images.Count == 0)
            return new OcrMedicineResult(null, null, null, null, "");

        // Read all streams concurrently, then send all images in one API call so the
        // model can reason across them (e.g. name on image 1, expiry date on image 2).
        var readTasks = images.Select(async img =>
        {
            using var ms = new MemoryStream();
            await img.Stream.CopyToAsync(ms, ct);
            return $"data:{img.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
        });
        var dataUris = await Task.WhenAll(readTasks);

        var hint = dataUris.Length > 1
            ? $"以下 {dataUris.Length} 张图片是同一商品的不同面，请综合识别所有图片中的信息："
            : "请识别这张商品包装图片中的信息：";

        var contentParts = new List<ChatMessageContentPart> { ChatMessageContentPart.CreateTextPart(hint) };
        contentParts.AddRange(dataUris.Select(uri => ChatMessageContentPart.CreateImagePart(new Uri(uri))));

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage([.. contentParts])
        };

        var response = await _chatClient.CompleteChatAsync(messages, cancellationToken: ct);
        var rawText = response.Value.Content[0].Text;
        _logger.LogDebug("LLM OCR response: {Text}", rawText);
        return ParseResponse(rawText);
    }

    private static OcrMedicineResult ParseResponse(string rawText)
    {
        try
        {
            var json = rawText.Trim();
            if (json.StartsWith("```"))
                json = string.Join("\n", json.Split('\n').Skip(1).SkipLast(1));

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string? Get(string key) =>
                root.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String
                    ? el.GetString()
                    : null;

            return new OcrMedicineResult(Get("name"), Get("expireDate"), Get("manufacturer"), Get("category"), rawText);
        }
        catch
        {
            return new OcrMedicineResult(null, null, null, null, rawText);
        }
    }
}
