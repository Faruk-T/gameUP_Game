using GameUp.Contracts;
using NUnit.Framework;

namespace GameUp.Lead.Tests
{
    public sealed class DocumentQueueTests
    {
        [Test] public void SecondDocumentDoesNotOverwriteFirst()
        { var queue = new DocumentQueue(); queue.Enqueue(new DocumentRequest(DocumentKind.Handbook, "first", "")); queue.Enqueue(new DocumentRequest(DocumentKind.Analysis, "second", "")); Assert.That(queue.Current.Title, Is.EqualTo("first")); queue.Confirm(queue.Current.Token); Assert.That(queue.Current.Title, Is.EqualTo("second")); }
        [Test] public void MandatoryDocumentCannotBeDismissed()
        { var queue = new DocumentQueue(); queue.Enqueue(new DocumentRequest(DocumentKind.Emergency, "fax", "", true)); Assert.That(queue.Dismiss(queue.Current.Token), Is.False); Assert.That(queue.IsOpen, Is.True); }
        [Test] public void StaleTokenCannotConfirmNewDocument()
        { var queue = new DocumentQueue(); queue.Enqueue(new DocumentRequest(DocumentKind.Handbook, "old", "")); long token = queue.Current.Token; queue.Clear(); queue.Enqueue(new DocumentRequest(DocumentKind.Emergency, "new", "", true)); Assert.That(queue.Confirm(token), Is.False); Assert.That(queue.Current.Title, Is.EqualTo("new")); }
        [Test] public void ConfirmationRunsOnlyOnce()
        { var queue = new DocumentQueue(); int count = 0; queue.Enqueue(new DocumentRequest(DocumentKind.Newspaper, "", "", true, "start", () => count++)); long token = queue.Current.Token; queue.Confirm(token); queue.Confirm(token); Assert.That(count, Is.EqualTo(1)); }
        [Test] public void ClearingDoesNotAcknowledgeEmergency()
        { var queue = new DocumentQueue(); int count = 0; queue.Enqueue(new DocumentRequest(DocumentKind.Emergency, "", "", true, "ok", () => count++)); queue.Clear(); Assert.That(count, Is.Zero); }
        [Test] public void ConfirmationCallbackCanEnqueueNextDocument()
        { var queue = new DocumentQueue(); queue.Enqueue(new DocumentRequest(DocumentKind.Newspaper, "first", "", true, "start", () => queue.Enqueue(new DocumentRequest(DocumentKind.Emergency, "second", "", true)))); queue.Confirm(queue.Current.Token); Assert.That(queue.Current.Title, Is.EqualTo("second")); }
    }
}
