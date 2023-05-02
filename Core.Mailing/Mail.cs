using MimeKit;

namespace Core.Mailing;

public class Mail
{
    public string Subject { get; set; }
    public string TextBody { get; set; }
    public string HtmlBody { get; set; }
    public AttachmentCollection? Attachments { get; set; }
    public List<MailboxAddress> ToList { get; set; }
    public List<MailboxAddress>? CcList { get; set; }
    public List<MailboxAddress>? BccList { get; set; }
    public string? UnsubscribeLink { get; set; }

    public Mail()
    {
        Subject = string.Empty;
        TextBody = string.Empty;
        HtmlBody = string.Empty;
        ToList = new List<MailboxAddress>();
    }

    public Mail(string subject, string textBody, string htmlBody, List<MailboxAddress> toList)
    {
        Subject = subject;
        TextBody = textBody;
        HtmlBody = htmlBody;
        ToList = toList;
    }
}
