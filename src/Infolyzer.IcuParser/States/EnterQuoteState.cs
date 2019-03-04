namespace Infolyzer.IcuParser.States
{
    internal class EnterQuoteState : BaseState
    {
        public override BaseState Eval(string c)
        {
            if (c == "'")
            {
                // Just a simple douple apostrophe, like this: [don''t]
                // Append a single apostrophe the current token buffer and switch back to text mode
                Scope.CurrentToken.Append(c);
                return TransitionTo<TextAppenderState>();
            }
            // Flush preceeding text
            Scope.FlushCurrentToken(TokenType.Text);
            // Start a new token for the quote
            Scope.CurrentToken.Append(c);
            return TransitionTo<QuoteState>();
        }
    }
}
