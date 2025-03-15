using System.Text;
using Claudable.ViewModels;

namespace Claudable.Services;

public class ConversationExporter
{
    private readonly ConversationViewModel _conversation;

    public ConversationExporter(ConversationViewModel conversation)
    {
        _conversation = conversation ?? throw new ArgumentNullException(nameof(conversation));
    }

    /// <summary>
    /// Gets only the messages in the current conversation branch (lineage)
    /// by starting from the current leaf message and walking up the tree.
    /// </summary>
    private List<ConversationMessageViewModel> GetCurrentBranchMessages()
    {
        var result = new List<ConversationMessageViewModel>();
        var messageMap = _conversation.Messages.ToDictionary(m => m.Uuid);

        // Start with the current leaf message
        string currentMessageId = _conversation.CurrentLeafMessageUuid;

        // If current leaf message UUID is not set, try to find the last message by index
        if (string.IsNullOrEmpty(currentMessageId) && _conversation.Messages.Count > 0)
        {
            var lastMessage = _conversation.Messages.OrderByDescending(m => m.Index).FirstOrDefault();
            if (lastMessage != null)
            {
                currentMessageId = lastMessage.Uuid;
            }
        }

        // Walk up the tree
        while (!string.IsNullOrEmpty(currentMessageId))
        {
            if (messageMap.TryGetValue(currentMessageId, out var message))
            {
                result.Add(message);
                currentMessageId = message.ParentMessageUuid;
            }
            else
            {
                // Message not found, break the loop
                break;
            }
        }

        // Reverse the list to get chronological order (oldest first)
        result.Reverse();
        return result;
    }

    public string Export()
    {
        StringBuilder sb = new StringBuilder();

        // Add a header with conversation info
        sb.AppendLine($"<!-- Conversation: {_conversation.Name} -->");
        sb.AppendLine($"<!-- UUID: {_conversation.Uuid} -->");
        sb.AppendLine($"<!-- Exported on: {DateTime.Now} -->");
        sb.AppendLine();

        // Get only the messages in the current branch (lineage)
        var currentBranchMessages = GetCurrentBranchMessages();

        // Handle case where the current leaf message wasn't found
        if (currentBranchMessages.Count == 0)
        {
            sb.AppendLine("<!-- No messages found in the current conversation branch -->");
            sb.AppendLine("<!-- Falling back to all messages in chronological order -->");
            sb.AppendLine();
            currentBranchMessages = _conversation.Messages;
        }

        // Sort messages by index to ensure correct order
        var sortedMessages = currentBranchMessages.OrderBy(m => m.Index).ToList();

        foreach (var message in sortedMessages)
        {
            if (message.Sender == "human")
            {
                sb.AppendLine("<user>");

                // Get all text content from this message
                var textContents = message.Content.Where(c => c.Type == "text").ToList();
                foreach (var content in textContents)
                {
                    if (!string.IsNullOrEmpty(content.Text))
                    {
                        sb.AppendLine(content.Text.Trim());
                    }
                }

                sb.AppendLine("</user>");
            }
            else if (message.Sender == "assistant")
            {
                sb.AppendLine("<assistant>");

                // Get thinking content (if any)
                var thinkingContents = message.Content.Where(c => c.Type == "thinking").ToList();
                foreach (var thinking in thinkingContents)
                {
                    // Add thinking summaries
                    if (thinking.Summaries != null && thinking.Summaries.Count > 0)
                    {
                        sb.AppendLine("<thinking_summaries>");
                        foreach (var summary in thinking.Summaries)
                        {
                            sb.AppendLine($"- {summary.Summary}");
                        }
                        sb.AppendLine("</thinking_summaries>");
                    }

                    // Add thinking content
                    if (!string.IsNullOrEmpty(thinking.Thinking))
                    {
                        sb.AppendLine("<thinking>");
                        sb.AppendLine(thinking.Thinking.Trim());
                        sb.AppendLine("</thinking>");
                    }
                }

                // Get text response content
                var textContents = message.Content.Where(c => c.Type == "text").ToList();
                foreach (var content in textContents)
                {
                    if (!string.IsNullOrEmpty(content.Text))
                    {
                        sb.AppendLine("<response>");
                        sb.AppendLine(content.Text.Trim());
                        sb.AppendLine("</response>");
                    }
                }

                sb.AppendLine("</assistant>");
            }

            sb.AppendLine(); // Add an extra line between messages
        }

        return sb.ToString();
    }
}