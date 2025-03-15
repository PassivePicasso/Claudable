using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Claudable.ViewModels;

public class ConversationViewModel : INotifyPropertyChanged
{
    [JsonProperty("uuid")]
    public string Uuid { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("summary")]
    public string Summary { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonProperty("chat_messages")]
    public List<ConversationMessageViewModel> Messages { get; set; } = new List<ConversationMessageViewModel>();

    [JsonProperty("project_uuid")]
    public string ProjectUuid { get; set; }

    [JsonProperty("current_leaf_message_uuid")]
    public string CurrentLeafMessageUuid { get; set; }

    [JsonProperty("settings")]
    public ConversationSettings Settings { get; set; }

    [JsonProperty("is_starred")]
    public bool IsStarred { get; set; }

    [JsonProperty("project")]
    public ProjectInfo Project { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class ConversationMessageViewModel
{
    [JsonProperty("uuid")]
    public string Uuid { get; set; }

    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("content")]
    public List<ConversationContentViewModel> Content { get; set; } = new List<ConversationContentViewModel>();

    [JsonProperty("sender")]
    public string Sender { get; set; }

    [JsonProperty("index")]
    public int Index { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonProperty("truncated")]
    public bool Truncated { get; set; }

    [JsonProperty("parent_message_uuid")]
    public string ParentMessageUuid { get; set; }

    [JsonProperty("attachments")]
    public List<object> Attachments { get; set; } = new List<object>();

    [JsonProperty("files")]
    public List<object> Files { get; set; } = new List<object>();
}

public class ConversationContentViewModel
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("thinking")]
    public string Thinking { get; set; }

    [JsonProperty("summaries")]
    public List<ConversationSummaryViewModel> Summaries { get; set; } = new List<ConversationSummaryViewModel>();

    [JsonProperty("start_timestamp")]
    public DateTime StartTimestamp { get; set; }

    [JsonProperty("stop_timestamp")]
    public DateTime StopTimestamp { get; set; }
}

public class ConversationSummaryViewModel
{
    [JsonProperty("summary")]
    public string Summary { get; set; }
}

public class ConversationSettings
{
    [JsonProperty("preview_feature_uses_artifacts")]
    public bool PreviewFeatureUsesArtifacts { get; set; }

    [JsonProperty("preview_feature_uses_latex")]
    public bool PreviewFeatureUsesLatex { get; set; }

    [JsonProperty("enabled_artifacts_attachments")]
    public bool EnabledArtifactsAttachments { get; set; }

    [JsonProperty("paprika_mode")]
    public string PaprikaMode { get; set; }
}

public class ProjectInfo
{
    [JsonProperty("uuid")]
    public string Uuid { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}

public class CurrentLeafMessageUpdate
{
    [JsonProperty("current_leaf_message_uuid")]
    public string CurrentLeafMessageUuid { get; set; }
}