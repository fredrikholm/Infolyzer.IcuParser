namespace Infolyzer.IcuParser.States
{
    internal class TentativeQuoteTerminationState : BaseState
    {
        public override BaseState Eval(string c)
        {
            if (c == "'")
            {
                // Just a double apostrophe within the quote:
                // Do as I '''say''' goddammit --> Do as I 'say' goddammit
                Scope.CurrentToken.Append(c);
                return TransitionTo<QuoteState>();
            }
            Scope.FlushCurrentToken(TokenType.Quote);
            if (c == "{")
            {
                return TransitionTo<ExpressionState>();
            }
            Scope.CurrentToken.Append(c);
            return TransitionTo<TextAppenderState>();
        }
    }
}
