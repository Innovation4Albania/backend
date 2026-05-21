using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Tests;

public sealed class InnovationDashboardStoreAiChatTests
{
    [Fact]
    public async Task GetAiChatReply_RejectsOversizedMessageBeforeGemini()
    {
        var store = StoreTestHelpers.CreateStore();
        var request = new AiChatRequest(new string('a', AiChatLimits.MaxMessageLength + 1));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            store.GetAiChatReply(StoreTestHelpers.DirectorContext(), request, "fake-api-key"));

        Assert.Contains(AiChatLimits.MaxMessageLength.ToString(), exception.Message);
    }

    [Fact]
    public async Task GetAiChatReply_RejectsOversizedHistoryMessageBeforeGemini()
    {
        var store = StoreTestHelpers.CreateStore();
        var request = new AiChatRequest(
            "Analizo projektet",
            [new AiChatMessageRequest("user", new string('h', AiChatLimits.MaxHistoryMessageLength + 1))]);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            store.GetAiChatReply(StoreTestHelpers.DirectorContext(), request, "fake-api-key"));

        Assert.Contains(AiChatLimits.MaxHistoryMessageLength.ToString(), exception.Message);
    }
}
