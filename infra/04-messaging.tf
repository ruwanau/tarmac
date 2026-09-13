resource "aws_sns_topic" "bag_events" {
  name = "${local.name}-bag-events"
}

resource "aws_sqs_queue" "dlq" {
  name = "${local.name}-bag-events-dlq"
}

resource "aws_sqs_queue" "bag_events" {
  name                       = "${local.name}-bag-events"
  visibility_timeout_seconds = 60
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.dlq.arn
    maxReceiveCount     = 3
  })
}

resource "aws_sns_topic_subscription" "to_queue" {
  topic_arn            = aws_sns_topic.bag_events.arn
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.bag_events.arn
  raw_message_delivery = true
}

resource "aws_sqs_queue_policy" "allow_sns" {
  queue_url = aws_sqs_queue.bag_events.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "sns.amazonaws.com" }
      Action    = "sqs:SendMessage"
      Resource  = aws_sqs_queue.bag_events.arn
      Condition = {
        ArnEquals = { "aws:SourceArn" = aws_sns_topic.bag_events.arn }
      }
    }]
  })
}

resource "aws_iam_role_policy" "task_messaging" {
  role = aws_iam_role.task.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = "sns:Publish"
        Resource = aws_sns_topic.bag_events.arn
      },
      {
        Effect = "Allow"
        Action = [
          "sqs:ReceiveMessage",
          "sqs:DeleteMessage",
          "sqs:GetQueueAttributes"
        ]
        Resource = aws_sqs_queue.bag_events.arn
      }
    ]
  })
}

resource "aws_ecr_repository" "worker" {
  name         = "${local.name}-worker"
  force_delete = true
}

resource "aws_cloudwatch_log_group" "worker" {
  name              = "/ecs/${local.name}-worker"
  retention_in_days = 3
}

output "topic_arn"  { value = aws_sns_topic.bag_events.arn }
output "queue_url"  { value = aws_sqs_queue.bag_events.id }
output "dlq_url"    { value = aws_sqs_queue.dlq.id }
output "ecr_worker" { value = aws_ecr_repository.worker.repository_url }
