namespace Flea.Logic.IrcConnectionUnit
{
    /// <summary>
    ///     A message to/from IRC
    /// </summary>
    public class IrcMessage
    {
        #region Internals

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is public.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is public; otherwise, <c>false</c>.
        /// </value>
        public bool IsPublic { get; set; }

        /// <summary>
        ///     Gets or sets the message text.
        /// </summary>
        /// <value>
        ///     The message text.
        /// </value>
        public string MessageText { get; set; }

        /// <summary>
        /// Gets or sets the source user.
        /// </summary>
        /// <value>
        /// The source user.
        /// </value>
        public string SourceUser { get; set; }

        /// <summary>
        /// Gets or sets the source channel.
        /// </summary>
        /// <value>
        /// The source channel.
        /// </value>
        public string SourceChannel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is private message.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is private message; otherwise, <c>false</c>.
        /// </value>
        public bool IsPrivateMessage { get; set; }

        /// <summary>
        /// Gets or sets the type of the source.
        /// </summary>
        /// <value>
        /// The type of the source.
        /// </value>
        public string SourceType { get; set; }

        /// <summary>
        /// Gets or sets the raw message.
        /// </summary>
        /// <value>
        /// The raw message.
        /// </value>
        public string RawMessage { get; set; }

        /// <summary>
        /// Gets or sets the exclamation command.
        /// </summary>
        /// <value>
        /// The exclamation command.
        /// </value>
        public string ExclamationCommand { get; set; }

        /// <summary>
        /// Gets or sets the user part.
        /// </summary>
        /// <value>
        /// The user part.
        /// </value>
        public string UserPart { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        public string Url { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="IrcMessage" /> class.
        /// </summary>
        /// <param name="messageText">The message text.</param>
        /// <param name="isPublic">if set to <c>true</c> [is public].</param>
        public IrcMessage(string messageText, bool isPublic = true)
        {
            IsPublic = isPublic;
            MessageText = messageText;
        }

        public IrcMessage()
        {
        }

        #endregion

        #region Members

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return MessageText;
        }

        #endregion
    }
}