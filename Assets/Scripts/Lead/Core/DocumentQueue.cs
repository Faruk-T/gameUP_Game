using System;
using System.Collections.Generic;
using GameUp.Contracts;

namespace GameUp.Lead
{
    public sealed class DocumentRequest
    {
        public long Token { get; internal set; }
        public DocumentKind Kind { get; }
        public string Title { get; }
        public string Body { get; }
        public bool RequiresAcknowledgement { get; }
        public string ButtonText { get; }
        internal Action Acknowledged { get; }
        public DocumentRequest(DocumentKind kind, string title, string body, bool requiresAcknowledgement = false,
            string buttonText = "Kapat", Action acknowledged = null)
        { Kind = kind; Title = title; Body = body; RequiresAcknowledgement = requiresAcknowledgement; ButtonText = buttonText; Acknowledged = acknowledged; }
    }

    /// <summary>Session tokens prevent delayed UI callbacks from closing a different document.</summary>
    public sealed class DocumentQueue
    {
        private readonly Queue<DocumentRequest> pending = new Queue<DocumentRequest>();
        private long nextToken;
        public DocumentRequest Current { get; private set; }
        public bool IsOpen => Current != null;
        public event Action Changed;

        public void Enqueue(DocumentRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Token != 0) throw new InvalidOperationException("Each document presentation needs a new request.");
            request.Token = ++nextToken;
            pending.Enqueue(request);
            ShowNext();
        }

        private void ShowNext()
        {
            if (Current != null || pending.Count == 0) return;
            Current = pending.Dequeue(); Changed?.Invoke();
        }

        public bool Confirm(long token)
        {
            if (Current == null || Current.Token != token) return false;
            var request = Current;
            Current = null;
            // Promote an already queued document before callbacks can enqueue another one.
            if (pending.Count > 0) Current = pending.Dequeue();
            Changed?.Invoke();
            request.Acknowledged?.Invoke();
            ShowNext();
            return true;
        }

        public bool Dismiss(long token)
        {
            if (Current == null || Current.Token != token || Current.RequiresAcknowledgement) return false;
            return Confirm(token);
        }

        public void Clear()
        {
            pending.Clear(); Current = null;
            // Tokens never reset; an old window's callback remains invalid after a new run.
            Changed?.Invoke();
        }
    }
}
