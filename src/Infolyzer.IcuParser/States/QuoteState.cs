namespace Infolyzer.IcuParser.States
{
    internal class QuoteState : BaseState
    {
        public override BaseState Eval(string c)
        {
            if (c == "'")
            {
                return TransitionTo<TentativeQuoteTerminationState>();
            }
            Scope.CurrentToken.Append(c);
            return this;
        }
    }
}
