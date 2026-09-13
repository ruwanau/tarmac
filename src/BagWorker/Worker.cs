using Amazon.SQS;
using Amazon.SQS.Model;

public class Worker(ILogger<Worker> log, IConfiguration cfg) : BackgroundService
{
    private readonly IAmazonSQS _sqs = new AmazonSQSClient();

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var url = cfg["QUEUE_URL"] ?? throw new Exception("QUEUE_URL not set");
        log.LogInformation("Polling {Url}", url);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var res = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl            = url,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds     = 20
                }, ct);

                foreach (var m in res.Messages ?? new List<Message>())
                {
                    try
                    {
                        log.LogInformation("Bag event: {Body}", m.Body);

                        await _sqs.DeleteMessageAsync(new DeleteMessageRequest
                        {
                            QueueUrl      = url,
                            ReceiptHandle = m.ReceiptHandle
                        }, ct);
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Failed, leaving for retry");
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Poll failed, backing off");
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }
}
