using System.Collections.Immutable;
using System.Threading;
using Moq;
using NAPS2.Ocr;
using Xunit;

namespace NAPS2.Sdk.Tests.Ocr;

public class OcrRequestQueueTests : ContextualTexts
{
    private readonly OcrRequestQueue _ocrRequestQueue;
    private readonly Mock<IOcrEngine> _mockEngine;
    private readonly Mock<OperationProgress> _mockOperationProgress;

    public OcrRequestQueueTests()
    {
        _mockEngine = new Mock<IOcrEngine>(MockBehavior.Strict);
        _mockOperationProgress = new Mock<OperationProgress>();
        _ocrRequestQueue = new OcrRequestQueue(_mockOperationProgress.Object);
    }

    [Fact]
    public async Task Enqueue()
    {
        var image = CreateScannedImage();
        var tempPath = Path.Combine(FolderPath, "tempocr.jpg");
        var ocrParams = new OcrParams("eng", OcrMode.Fast, 10);
        var mockResult = new OcrResult((0, 0, 1, 1), Enumerable.Empty<OcrResultElement>(), false);
        File.WriteAllText(tempPath, @"blah");
        _mockEngine.Setup(x => x.ProcessImage(tempPath, ocrParams, It.IsAny<CancellationToken>())).Returns(mockResult);

        var ocrResult =
            await _ocrRequestQueue.Enqueue(
                _mockEngine.Object,
                image,
                tempPath,
                ocrParams,
                OcrPriority.Foreground,
                CancellationToken.None);

        Assert.Equal(mockResult, ocrResult);
    }
    
    // TODO: Tests to add:
    // - Unsupported language code
    // - Many parallel tasks (more than worker threads - also # of worker threads should be configurable by the test)
    // - Can I break things by overloading task parallelization? I honestly don't remember why this is supposed to work...
}